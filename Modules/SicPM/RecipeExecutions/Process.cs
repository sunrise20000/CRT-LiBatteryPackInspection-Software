using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.Common.SubstrateTrackings;
using SicPM.Devices;
using SicPM.Routines;
using System;
using System.Collections.Generic;
using System.Xml;
using static SicPM.PmDevices.DicMode;

namespace SicPM.RecipeExecutions
{
    public enum RecipeContinueMode
    {
        None,
        WaferReturnAndJobStop,
        RecipeCompleted,
        StepContinue,
        StepRestart,
        RecipeRestart,
        NextStep,
    }

    public partial class Process : PMBaseRoutine
    {
        enum RoutineStep
        {
            WaitProcess,
        }

        enum RecipeRunningState
        {
            Error,
            RecipeCompleted,
            ExecStep,
            TimeWait,
            ConditionWait,
            StepCompleted,
            Paused,
        }

        private object _recipeLocker = new object();
        private bool _hasRecordRunTime = false;

        private RecipeRunningState _state = RecipeRunningState.ExecStep;
        private RecipeRunningState _pausedState = RecipeRunningState.ExecStep;
        private DeviceTimer _estimatedTimeCalcTimer = new DeviceTimer();//用于定时计算工艺程序估计的结束时间

        private double _curStepElpasedTimeBeforePaused;

        private double _curStepElpasedTimeBeforePaused2;
        private List<int> _lstSkipSteps = new List<int>();

        public RecipeContinueMode ContinueAction { get; set; }

        public DateTime _recipeStartTime
        {
            get;
            private set;
        }

        public string CurrentRecipeContent { get; private set; }


        private int _currentStepNumber;

        private int _dummyStepCount;

        public int CurStepTotalLoopCount
        {
            get;
            private set;
        }

        public double CurStepTotalTime
        {
            get
            {
                if (PMDevice.RecipeRunningInfo.RecipeStepList == null || PMDevice.RecipeRunningInfo.RecipeStepList.Count == 0 || _state == RecipeRunningState.RecipeCompleted || _state == RecipeRunningState.Error)
                    return 0;
                return PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000;
            }
        }


        public int CurrentLoopCount
        {
            get;
            private set;
        }

        private DeviceTimer _stepTimer = new DeviceTimer();
        private DeviceTimer _stepTimer2 = new DeviceTimer();
        private DeviceTimer _recipeTimer = new DeviceTimer();

        public bool IsPaused
        {
            private set;
            get;
        }


        private double CurStepLeftTime
        {
            get
            {
                return _stepTimer.GetTotalTime() - _stepTimer.GetElapseTime();
            }
        }

        public double EstimatedTotalLeftTime
        {
            get;
            private set;
        }

        private RecipeDBCallback _dbCallback;
        private Fdc _fdc;

        private bool _isDryRun;
        private int _delayTimeDryRun;

        private double _tempOffset;

        private int _currentStepIndex = 99;
        private IoInterLock _pmInterLock;

        #region Parse

        private bool _isPSUHeaterJumpMode;
        private bool _isSCRHeaterJumpMode;
        private bool _isMFCJumpMode;

        #endregion

        #region Check

        private PeriodicJob _thread;

        private DeviceTimer _rampCalcTimer = new DeviceTimer();//用于定时获取PC的Ramp

        protected ToleranceChecker[] _mfcGapChecker = new ToleranceChecker[32];
        protected R_TRIG[] _mfcTrig = new R_TRIG[32];

        protected ToleranceChecker[] _pcGapChecker = new ToleranceChecker[7];
        protected R_TRIG[] _pcTrig = new R_TRIG[7];
        protected R_TRIG[] _pcTrig2 = new R_TRIG[7];

        protected double[] _pressurePrevious = new double[7];

        #endregion

        public Process(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "Process";

            _dbCallback = new RecipeDBCallback();
            _fdc = new Fdc(Module); 
            _pmInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");

            for (int i = 0; i < _mfcGapChecker.Length; i++)
            {
                _mfcGapChecker[i] = new ToleranceChecker();
                _mfcTrig[i] = new R_TRIG();
            }

            for (int i = 0; i < _pcGapChecker.Length; i++)
            {
                _pcGapChecker[i] = new ToleranceChecker();
                _pcTrig[i] = new R_TRIG();
                _pcTrig2[i] = new R_TRIG();
            }

            Initialize();

            Calculte();

            _thread = new PeriodicJob(10 * 1000, Calculte, "Calculte Standard Deviation", false);
        }

        public override Result Start(params object[] param)
        {
            Reset();
            _hasRecordRunTime = false;
            if (!_pmInterLock.SetPMProcessRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not run Process, {reason}");
                return Result.FAIL;
            }
            

            _lstSkipSteps = new List<int>();

            _currentStepIndex = 99;
            _currentStepNumber = CurStepTotalLoopCount = 0;
            _dummyStepCount = 0;

            _estimatedTimeCalcTimer.Start(1000);
            _rampCalcTimer.Start(1000);

            PMDevice.RecipeRunningInfo.InnerId = Guid.NewGuid();
            PMDevice.RecipeRunningInfo.BeginTime = DateTime.Now;
            PMDevice.RecipeRunningInfo.TotalTime = CalcRecipeTime(); 
            PMDevice.RecipeRunningInfo.IsRoutineAbort = false;

            _state = RecipeRunningState.ExecStep;

            _recipeTimer.Start(int.MaxValue);

            _dbCallback.RecipeStart(PMDevice.Module, 0, PMDevice.RecipeRunningInfo.InnerId.ToString(), PMDevice.RecipeRunningInfo.RecipeName);
            _dbCallback.RecipeUpdateStatus(PMDevice.RecipeRunningInfo.InnerId.ToString(), "InProcess");

            WaferManager.Instance.UpdateWaferProcessStatus(ModuleHelper.Converter(Module), 0, EnumWaferProcessStatus.InProcess);
            WaferManager.Instance.GetWafer(ModuleHelper.Converter(Module), 0).TrayProcessCount--;

            _fdc.Reset();

            _isDryRun = SC.GetValue<bool>($"PM.{Module}.DryRun.IsDryRun");
            _delayTimeDryRun = SC.GetValue<int>($"PM.{Module}.DryRun.DryRunDelayTime");
            _tempOffset = SC.GetValue<double>($"PM.{Module}.Process.TempOffset");

            _thread.Start();

            Notify($"Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            if (!PMDevice.CheckEnableRunProcess(out string reason))
            {
                EV.PostAlarmLog(Module, reason);
                return Result.FAIL;
            }

            if (_isDryRun) // 空跑工艺
            {
                try
                {
                    DryRunProcess((int)RoutineStep.WaitProcess, $"Chamber:{Name}:WaitProcess", _delayTimeDryRun);
                }
                catch (RoutineBreakException)
                {
                    return Result.RUN;
                }
                catch (RoutineFaildException)
                {
                    return Result.FAIL;
                }

                Notify("End");

                return Result.DONE;
            }
            else
            {
                MonitorRecipeEndTime();

                MonitorRecipeRunInfo();

                lock (_recipeLocker)
                {
                    try
                    {
                        switch (_state)
                        {
                            case RecipeRunningState.ExecStep:
                                {
                                    PMDevice.ResetToleranceChecker();

                                    //int stepTime = (int)PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000;
                                    if (ContinueAction != RecipeContinueMode.StepContinue)
                                    {
                                        _curStepElpasedTimeBeforePaused = 0;
                                        _curStepElpasedTimeBeforePaused2 = 0;
                                    }

                                    ContinueAction = RecipeContinueMode.None;

                                    if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsLoopStartStep)
                                    {
                                        CurStepTotalLoopCount = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].LoopCount;
                                        if (CurStepTotalLoopCount == 0)
                                        {
                                            CurrentLoopCount = 0;
                                        }
                                        else
                                        {
                                            CurrentLoopCount++;
                                        }
                                    }

                                    //stepTime = (int)(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime - _curStepElpasedTimeBeforePaused / 1000);
                                    _stepTimer.Start(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000 - _curStepElpasedTimeBeforePaused);

                                    if (!_stepTimer2.IsIdle())
                                    {
                                        _curStepElpasedTimeBeforePaused2 += _stepTimer2.GetElapseTime();
                                        _stepTimer2.Stop();
                                    }
                                    _stepTimer2.Start(int.MaxValue);

                                    Notify($"Running step {_currentStepNumber + 1} : {PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName}");

                                    if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsDummyStep)
                                    {
                                        _dummyStepCount++;
                                    }

                                    //执行工艺程序命令
                                    foreach (var recipeCmd in PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands.Keys)
                                    {
                                        if (recipeCmd == "SusHeaterSetMode")
                                        {
                                            if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd] == "Jump")
                                                _isPSUHeaterJumpMode = true;
                                            else
                                                _isPSUHeaterJumpMode = false;

                                            continue;
                                        }
                                        if (recipeCmd == "WWHeaterSetMode")
                                        {
                                            if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd] == "Jump")
                                                _isSCRHeaterJumpMode = true;
                                            else
                                                _isSCRHeaterJumpMode = false;

                                            continue;
                                        }
                                        if (recipeCmd == "FlowSetMode")
                                        {
                                            if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd] == "Jump")
                                                _isMFCJumpMode = true;
                                            else
                                                _isMFCJumpMode = false;

                                            continue;
                                        }

                                        if (IsCmdSkip(recipeCmd)) // 不是注册的方法，需要跳过
                                            continue;

                                        if (!OP.CanDoOperation($"{Module}.{recipeCmd}", out reason, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd]))
                                        {
                                            EV.PostAlarmLog(Module, $"Can not execute {recipeCmd}, {reason}");
                                            return Result.FAIL;
                                        }
                                        else
                                        {
                                            int time = (int)(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000 - _curStepElpasedTimeBeforePaused);// (int)PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000;

                                            if (recipeCmd.StartsWith("TC1") && _isPSUHeaterJumpMode)
                                            {
                                                time = 1;
                                            }

                                            if (recipeCmd.StartsWith("TC2") && _isSCRHeaterJumpMode)
                                            {
                                                time = 1;
                                            }

                                            if (recipeCmd.StartsWith("Mfc") && recipeCmd.EndsWith(".Ramp") && !PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsDummyStep)
                                            {
                                                if (_isMFCJumpMode)
                                                {
                                                    time = 1;
                                                }
                                            }

                                            if ((recipeCmd == "TV.SetPressure"
                                                || recipeCmd == "PMServo.SetActualSpeed"
                                                || recipeCmd.StartsWith("Pressure") && recipeCmd.EndsWith(".Ramp")
                                                || recipeCmd.StartsWith("Mfc") && recipeCmd.EndsWith(".Ramp"))
                                                && !PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsDummyStep)
                                            {
                                                if (_currentStepNumber >= 1)
                                                {
                                                    int previousStepNumber = _currentStepNumber - 1;

                                                    if (PMDevice.RecipeRunningInfo.RecipeStepList[previousStepNumber].IsDummyStep)
                                                        previousStepNumber = _currentStepNumber - 2;

                                                    if (PMDevice.RecipeRunningInfo.RecipeStepList[previousStepNumber].RecipeCommands.ContainsKey(recipeCmd))
                                                    {
                                                        string previousValue = PMDevice.RecipeRunningInfo.RecipeStepList[previousStepNumber].RecipeCommands[recipeCmd];
                                                        string currentValue = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd];
                                                        if (previousValue == currentValue)
                                                        {
                                                            if (_lstSkipSteps.Count>0 && _lstSkipSteps.Contains(previousStepNumber)) //上一步是跳步过来的，这一步和上一步的值是相同的不用设置
                                                            {
                                                                continue;
                                                            }

                                                            time = 1;
                                                        }
                                                    }
                                                }
                                            }

                                            OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd]);
                                        }
                                    }

                                    if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].EndBy == EnumEndByCondition.ByTime)
                                        _state = RecipeRunningState.TimeWait;
                                    else
                                        _state = RecipeRunningState.ConditionWait;

                                    //ResetChecker();

                                    _dbCallback.RecipeStepStart(PMDevice.RecipeRunningInfo.InnerId.ToString(), _currentStepNumber, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName, (float)PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime);
                                    _fdc.Start(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands);
                                }
                                break;

                            case RecipeRunningState.TimeWait:

                                if (IsPaused)
                                {
                                    _state = RecipeRunningState.Paused;
                                }

                                if (_stepTimer.IsTimeout())
                                {
                                    _state = RecipeRunningState.StepCompleted;
                                }

                                if (PMDevice.RecipeRunningInfo.NeedReloadRecipe)
                                {
                                    if (string.IsNullOrEmpty(PMDevice.RecipeRunningInfo.XmlRecipeToReload))
                                    {
                                        EV.PostWarningLog(Module, "Recipe is required to be reloaded but no new recipe is received");
                                        PMDevice.RecipeRunningInfo.NeedReloadRecipe = false;
                                    }
                                    else
                                    {   //重新加载后面的Recipe内容
                                        if (RecipeParser.ParseXmlString(PMDevice.RecipeRunningInfo.XmlRecipeToReload, Module, out var recipeHead, out var recipeSteps, out var reason1))
                                        {
                                            PMDevice.RecipeRunningInfo.RecipeStepList = recipeSteps;
                                            PMDevice.RecipeRunningInfo.TotalTime = CalcRecipeTime();
                                            PMDevice.RecipeRunningInfo.NeedReloadRecipe = false;
                                        }
                                        else
                                        {
                                            EV.PostWarningLog(Module, $"Reloading Recipe failed, {reason1}");
                                        }
                                    }
                                 
                                }

                                //ToleranceChecker();

                                SkipStepForHeat();

                                break;

                            case RecipeRunningState.ConditionWait:
                                {
                                    if (_stepTimer.IsTimeout())
                                    {
                                        _state = RecipeRunningState.StepCompleted;
                                    }
                                }
                                break;

                            case RecipeRunningState.Paused:
                                PMDevice.PauseRecipe(out reason);
                                if (!_stepTimer.IsIdle())
                                {
                                    _curStepElpasedTimeBeforePaused += _stepTimer.GetElapseTime();
                                    _stepTimer.Stop();
                                }
                                switch (ContinueAction)
                                {
                                    case RecipeContinueMode.None:
                                        break;
                                    case RecipeContinueMode.WaferReturnAndJobStop:
                                        //Singleton<RouteManager>.Instance.CheckToPostMessage((int)RouteManager.MSG.StopJob);
                                        _state = RecipeRunningState.Error;
                                        break;
                                    case RecipeContinueMode.RecipeCompleted:
                                        _state = RecipeRunningState.RecipeCompleted;
                                        break;
                                    case RecipeContinueMode.StepContinue:
                                        _state = RecipeRunningState.ExecStep;
                                        break;
                                    case RecipeContinueMode.StepRestart:
                                        _state = RecipeRunningState.ExecStep;
                                        break;
                                    case RecipeContinueMode.RecipeRestart:
                                        _currentStepNumber = 0;
                                        _state = RecipeRunningState.ExecStep;
                                        break;
                                    case RecipeContinueMode.NextStep:
                                        _state = RecipeRunningState.StepCompleted;
                                        break;
                                }
                                break;

                            case RecipeRunningState.StepCompleted:
                                {
                                    //放在前面，stepnumber后面会被更新

                                    _stepTimer2.Stop();

                                    _dbCallback.RecipeStepEnd(PMDevice.RecipeRunningInfo.InnerId.ToString(), _currentStepNumber, _fdc.DataList);
                                    _fdc.Stop();

                                    if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsLoopEndStep)
                                    {
                                        //重新读取循环的设定次数
                                        for (int nn = _currentStepNumber; nn >= 0; nn--)
                                        {
                                            if (PMDevice.RecipeRunningInfo.RecipeStepList[nn].IsLoopStartStep)
                                            {
                                                CurStepTotalLoopCount = PMDevice.RecipeRunningInfo.RecipeStepList[nn].LoopCount;
                                                break;
                                            }
                                        }
                                        if (CurrentLoopCount >= CurStepTotalLoopCount)
                                        {
                                            CurrentLoopCount = CurStepTotalLoopCount = 0;
                                            _currentStepNumber++;
                                        }
                                        else
                                        {
                                            int n = _currentStepNumber - 1;
                                            int next = -1;
                                            while (n >= 0)
                                            {
                                                if (PMDevice.RecipeRunningInfo.RecipeStepList[n].IsLoopStartStep)
                                                {
                                                    next = n;
                                                    break;
                                                }
                                                n--;
                                            }
                                            if (next == -1)
                                                throw new Exception("Loop End control error");
                                            _currentStepNumber = next;
                                        }
                                    }
                                    else
                                    {
                                        _currentStepNumber++;
                                    }

                                    if (_currentStepNumber >= PMDevice.RecipeRunningInfo.RecipeStepList.Count)
                                    {
                                        _currentStepNumber = PMDevice.RecipeRunningInfo.RecipeStepList.Count - 1;
                                        _state = RecipeRunningState.RecipeCompleted;
                                    }
                                    else
                                    {
                                        _state = RecipeRunningState.ExecStep;
                                    }
                                }


                                break;

                            case RecipeRunningState.RecipeCompleted:
                                {

                                    //更新PM的Runtime
                                    if (!_hasRecordRunTime)
                                    {
                                        _hasRecordRunTime = true;
                                        RuntimeDataRecorder.UpdateElapseTimePM(Module + " Process", (int)(_recipeTimer.GetElapseTime() / 60000));
                                    }

                                    _recipeTimer.Stop();
                                    Notify("Finished");
                                    return Result.DONE;
                                }

                            case RecipeRunningState.Error:
                                {
                                    //更新PM的Runtime
                                    if (!_hasRecordRunTime)
                                    {
                                        _hasRecordRunTime = true;
                                        RuntimeDataRecorder.UpdateElapseTimePM(Module+" Process", (int)(_recipeTimer.GetElapseTime() / 60000));
                                    }
                                    return Result.DONE;
                                }
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        PMDevice.SetHeaterStopRamp();
                        PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
                        PMDevice.SetRotationStopRamp();

                        LOG.Write(ex);
                        return Result.FAIL;
                    }
                }

                return Result.RUN;
            }
        }


        private void MonitorRecipeRunInfo()
        {
            try
            {
                PMDevice.RecipeRunningInfo.StepNumber = _currentStepNumber + 1 - _dummyStepCount; //CurStepNum start from 0, ignore dummy step
                PMDevice.RecipeRunningInfo.StepName = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName;
                PMDevice.RecipeRunningInfo.StepTime = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime;
                PMDevice.RecipeRunningInfo.StepElapseTime = _stepTimer.IsIdle() ? _curStepElpasedTimeBeforePaused / 1000 : (_stepTimer.GetElapseTime() + _curStepElpasedTimeBeforePaused) / 1000;
                PMDevice.RecipeRunningInfo.TotalElapseTime = CalcElapseRecipeTime();

                PMDevice.RecipeRunningInfo.StepElapseTime2 = _stepTimer2.IsIdle() ? _curStepElpasedTimeBeforePaused2 / 1000 : (_stepTimer2.GetElapseTime() + _curStepElpasedTimeBeforePaused2) / 1000;
                PMDevice.RecipeRunningInfo.TotalElapseTime2 = _recipeTimer.GetElapseTime() / 1000;

                if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands.ContainsKey("SHArH2Switch.SetValve"))
                {
                    PMDevice.RecipeRunningInfo.ArH2Switch = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands["SHArH2Switch.SetValve"];
                }
                if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands.ContainsKey("N2Dilution.SetValve"))
                {
                    PMDevice.RecipeRunningInfo.N2FlowMode = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands["N2Dilution.SetValve"];
                }

                if (_currentStepIndex != PMDevice.RecipeRunningInfo.StepNumber)
                {
                    Notify($"Start Recipe: Step:{PMDevice.RecipeRunningInfo.StepNumber}  Name:{PMDevice.RecipeRunningInfo.StepName} ");
                    _currentStepIndex = PMDevice.RecipeRunningInfo.StepNumber;
                }
            }
            catch (Exception ex)
            { 
                
            }
        }

        public override void Abort()
        {
            //更新PM的Runtime
            PMDevice.RecipeRunningInfo.IsRoutineAbort = true;
            if (!_hasRecordRunTime)
            {
                _hasRecordRunTime = true;
                RuntimeDataRecorder.UpdateElapseTimePM(Module + " Process", (int)(_recipeTimer.GetElapseTime() / 60000));
            }

            _state = RecipeRunningState.RecipeCompleted;

            _dbCallback.RecipeFailed(PMDevice.RecipeRunningInfo.InnerId.ToString());
            _fdc.Stop();

            PMDevice.AbortRunProcess(out string reason);

            PMDevice.RecipeRunningInfo.StepName = string.Empty;
            PMDevice.RecipeRunningInfo.StepNumber = 0;
            PMDevice.RecipeRunningInfo.StepTime = 0;
            PMDevice.RecipeRunningInfo.StepElapseTime = 0;
            PMDevice.RecipeRunningInfo.TotalTime = 0;
            PMDevice.RecipeRunningInfo.TotalElapseTime = 0;

            //PMDevice.Rf.SetPowerOnOff(false, out _);
            //PMDevice.Microwave.SetPowerOnOff(false, out _);

            //PMDevice.GasLine1.SetFlow(out _, 0, 0);
            //PMDevice.GasLine2.SetFlow(out _, 0, 0);
            //PMDevice.GasLine3.SetFlow(out _, 0, 0);
        }

        public void ExitProcess()
        {
            if (_state == RecipeRunningState.RecipeCompleted)
            {
                _dbCallback.RecipeComplete(PMDevice.RecipeRunningInfo.InnerId.ToString());
                _fdc.Stop();
            }
            else
            {
                _dbCallback.RecipeFailed(PMDevice.RecipeRunningInfo.InnerId.ToString());
                _fdc.Stop();
            }

            _thread.Stop();
        }


        public void PauseRecipe()
        {
            if (_state != RecipeRunningState.TimeWait && _state != RecipeRunningState.ConditionWait)
                return;

            if (!IsPaused)
            {
                IsPaused = true;
                _pausedState = _state;
                _state = RecipeRunningState.Paused;
            }
        }


        public void SkipCurrentRecipeStep()
        {
            if (_state == RecipeRunningState.ConditionWait || _state == RecipeRunningState.TimeWait)
            {
                _lstSkipSteps.Add(_currentStepNumber);
                _state = RecipeRunningState.StepCompleted;
            }
        }


        protected int CalcRecipeTime()
        {
            double total = 0;
            for (int i = 0; i < PMDevice.RecipeRunningInfo.RecipeStepList.Count; i++)
            {
                if (!PMDevice.RecipeRunningInfo.RecipeStepList[i].IsDummyStep) // 不统计虚拟Step的时间
                {
                    total += PMDevice.RecipeRunningInfo.RecipeStepList[i].StepTime;
                }
            }

            return (int)total;
        }

        protected int CalcElapseRecipeTime()
        {
            double total = 0;
            for (int i = 0; i < _currentStepNumber; i++)
            {
                if (!PMDevice.RecipeRunningInfo.RecipeStepList[i].IsDummyStep) // 不统计虚拟Step的时间
                {
                    total += PMDevice.RecipeRunningInfo.RecipeStepList[i].StepTime;
                }
            }
            total += PMDevice.RecipeRunningInfo.StepElapseTime;

            return (int)total;
        }

        protected void MonitorRecipeEndTime()
        {
            try
            {
                if (!_estimatedTimeCalcTimer.IsTimeout())
                    return;
                _estimatedTimeCalcTimer.Start(1000);

                EstimatedTotalLeftTime = 0;

                if (_state == RecipeRunningState.RecipeCompleted)
                    return;

                if (!(_currentStepNumber >= 0 && _currentStepNumber <= PMDevice.RecipeRunningInfo.RecipeStepList.Count - 1))
                    return;

                if (CurStepLeftTime > 0)
                {
                    EstimatedTotalLeftTime = CurStepLeftTime;
                }

                int nextBegin = _currentStepNumber;
                bool IsInLoop = false;
                int iNum1 = 0;
                int iNum2 = 0;
                //int j=i;
                for (int j = _currentStepNumber; j < PMDevice.RecipeRunningInfo.RecipeStepList.Count; j++)
                {
                    if (PMDevice.RecipeRunningInfo.RecipeStepList[j].IsLoopEndStep)
                    {
                        iNum2 = j;
                        IsInLoop = true;
                        break;
                    }
                    else if (j > _currentStepNumber && PMDevice.RecipeRunningInfo.RecipeStepList[j].IsLoopStartStep)
                    {
                        IsInLoop = false;
                        break;
                    }
                }

                if (IsInLoop)
                {
                    iNum1 = _currentStepNumber;
                    for (int j = _currentStepNumber; j >= 0; j--)
                    {
                        if (PMDevice.RecipeRunningInfo.RecipeStepList[j].IsLoopStartStep)
                        {
                            iNum1 = j;
                            break;
                        }
                    }

                    for (int j = _currentStepNumber + 1; j <= iNum2; j++)
                    {
                        EstimatedTotalLeftTime += PMDevice.RecipeRunningInfo.RecipeStepList[j].StepTime * 1000 * (PMDevice.RecipeRunningInfo.RecipeStepList[iNum1].LoopCount - CurrentLoopCount + 1);
                    }

                    for (int j = iNum1; j <= _currentStepNumber; j++)
                    {
                        EstimatedTotalLeftTime += PMDevice.RecipeRunningInfo.RecipeStepList[j].StepTime * 1000 * (PMDevice.RecipeRunningInfo.RecipeStepList[iNum1].LoopCount - CurrentLoopCount);
                    }

                    nextBegin = iNum2 + 1;
                }
                else
                {
                    nextBegin++;
                }

                for (int j = nextBegin; j < PMDevice.RecipeRunningInfo.RecipeStepList.Count; j++)
                {
                    if (PMDevice.RecipeRunningInfo.RecipeStepList[j].IsLoopStartStep)
                    {
                        //j=i;
                        iNum1 = j;
                        iNum2 = j + 1;
                        double lr1 = 0;
                        for (int m = j; m < PMDevice.RecipeRunningInfo.RecipeStepList.Count; m++)
                        {
                            lr1 += PMDevice.RecipeRunningInfo.RecipeStepList[m].StepTime * 1000;
                            if (PMDevice.RecipeRunningInfo.RecipeStepList[m].IsLoopEndStep)
                            {
                                iNum2 = m;
                                break;
                            }
                        }
                        EstimatedTotalLeftTime += lr1 * PMDevice.RecipeRunningInfo.RecipeStepList[iNum1].LoopCount;
                        j = iNum2;
                    }
                    else
                    {
                        EstimatedTotalLeftTime += PMDevice.RecipeRunningInfo.RecipeStepList[j].StepTime * 1000;
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void SetContinue(string continueMode)
        {
            switch (continueMode)
            {
                case "Step continue":
                    ContinueAction = RecipeContinueMode.StepContinue;
                    break;
                case "Step restart":
                    ContinueAction = RecipeContinueMode.StepRestart;
                    break;
                case "Next step":
                    ContinueAction = RecipeContinueMode.NextStep;
                    break;
                case "Recipe restart":
                    ContinueAction = RecipeContinueMode.RecipeRestart;
                    break;
                case "Recipe complete":
                    ContinueAction = RecipeContinueMode.RecipeCompleted;
                    break;
                case "Wafer return and job stop":
                    ContinueAction = RecipeContinueMode.WaferReturnAndJobStop;
                    break;
            }

            IsPaused = false;

            PMDevice.ResetToleranceChecker();
        }

        public void DryRunProcess(int id, string stepName, int delayTime)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                Notify($"Dry run process {delayTime} seconds");

                _stepSpan = new TimeSpan(0, 0, 0, (int)delayTime);
                _stepStartTime = DateTime.Now;
                _stepName = stepName;

                return true;
            }, delayTime * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }

                throw new RoutineBreakException();
            }
        }

        public void ToleranceChecker()
        {
            #region MFC

            for (int i = 0; i < _mfcGapChecker.Length; i++)
            {
                _mfcGapChecker[i].Monitor(PMDevice._mfcList[i].FeedBack, PMDevice._mfcList[i].SetPoint * (1 - 2 / 100), PMDevice._mfcList[i].SetPoint * (1 + 2 / 100), 5 * 60);

                if (_mfcGapChecker[i].Trig)
                {
                    EV.PostWarningLog(Module, $"{PMDevice._mfcList[i].Name} flow gap > 2%, over 5 minites");
                }
            }

            for (int i = 0; i < _mfcTrig.Length; i++)
            {
                double gap = Convert.ToDouble(dbMfcGapResults[i].StdDev);
                double setPoint = Convert.ToDouble(dbMfcSetPointResults[i].Mean);

                _mfcTrig[i].CLK = gap / setPoint > 1 / 100;

                if (_mfcTrig[i].Q)
                {
                    EV.PostWarningLog(Module, $"{PMDevice._mfcList[i].Name} flow standard deviation > 1%, for 5 minites");
                }
            }

            #endregion

            #region PC

            for (int i = 0; i < _pcGapChecker.Length; i++)
            {
                _pcGapChecker[i].Monitor(PMDevice._pcList[i].FeedBack, PMDevice._pcList[i].SetPoint - 60, PMDevice._pcList[i].SetPoint + 60, 5 * 60);

                if (_pcGapChecker[i].Trig)
                {
                    EV.PostWarningLog(Module, $"{PMDevice._pcList[i].Name} flow gap > 60 mbar over 5 minites");
                }
            }

            for (int i = 0; i < _pcTrig.Length; i++)
            {
                double gap = Convert.ToDouble(dbPcGapResults[i].StdDev);
                //double setPoint = Convert.ToDouble(dbPcSetPointResults[i].StdDev);

                _pcTrig[i].CLK = gap > 10;

                if (_pcTrig[i].Q)
                {
                    EV.PostWarningLog(Module, $"{PMDevice._mfcList[i].Name} flow standard deviation > 1%, for 5 minites");
                }
            }

            if (_rampCalcTimer.IsTimeout())
            {
                for (int i = 0; i < _pcTrig2.Length; i++)
                {
                    _pcTrig2[i].CLK = Math.Abs(PMDevice._pcList[i].FeedBack - _pressurePrevious[i]) > 90;

                    if (_pcTrig2[i].Q)
                    {
                        EV.PostWarningLog(Module, $"{PMDevice._mfcList[i].Name} pressure ramp > 90 mbar");
                    }

                    _pressurePrevious[i] = PMDevice._pcList[i].FeedBack;
                }

                _rampCalcTimer.Start(1000);
            }

            #endregion

        }

        public void ResetChecker()
        {
            #region MFC

            for (int i = 0; i < _mfcGapChecker.Length; i++)
            {
                _mfcGapChecker[i].Reset(5 * 60);
            }

            for (int i = 0; i < _mfcTrig.Length; i++)
            {
                _mfcTrig[i].RST = true;
            }

            #endregion

            #region PC

            for (int i = 0; i < _pcGapChecker.Length; i++)
            {
                _pcGapChecker[i].Reset(5 * 60);
            }

            for (int i = 0; i < _pcTrig.Length; i++)
            {
                _pcTrig[i].RST = true;
            }

            #endregion
        }

        public void SkipStepForHeat()
        {
            int stepCount = PMDevice.RecipeRunningInfo.RecipeStepList.Count;

            if (_currentStepNumber + 1 < stepCount)
            {
                var currentStep = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber];
                var nextStep = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber + 1];

                if (currentStep.IsDummyStep)
                    return;

                if (nextStep.IsDummyStep)
                    nextStep = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber + 2];

                if (!currentStep.RecipeCommands.ContainsKey("TC1.SetHeaterMode") ||
                    !nextStep.RecipeCommands.ContainsKey("TC1.SetHeaterMode"))
                    return;


                HeaterControlMode currentPSUMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode),
                    currentStep.RecipeCommands["TC1.SetHeaterMode"]);
                HeaterControlMode nextPSUMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode),
                    nextStep.RecipeCommands["TC1.SetHeaterMode"]);

                
                
                if (currentPSUMode == HeaterControlMode.Power)
                {
                    var PsuL2TempPv =
                        (float)DATA.Poll(
                            $"{Module}.TC1.L2InputTempSetPoint"); // (float)DATA.Poll($"{Module}.TC1.L1PVFeedBack"); 测试用

                    // 根据系统配置决定Power模式加热时是否强制跳步。
                    var forceSkip = SC.GetValue<bool>("PM.PM1.Heater.Recipe.ForceSkipPowerModeStep");
                    if (forceSkip)
                    {
                        //  如果温差小于阈值，则跳步
                        var PsuL2TempSp = Convert.ToSingle(currentStep.RecipeCommands["TC1.SetL2TargetSP"]);
                        if (Math.Abs(PsuL2TempPv - PsuL2TempSp) < Math.Abs(_tempOffset))
                        {
                            SkipCurrentRecipeStep();

                            EV.PostInfoLog(Module,
                                $"Current PSU middle temperature is {PsuL2TempPv}℃ and Power mode, and it's target is {PsuL2TempSp}℃，" +
                                $" TempOffset is {_tempOffset}℃. Need jump step!");
                        }
                    }
                    else
                    {
                        // 仅下一步不是Power模式时才判断是否跳步
                        if (nextPSUMode != HeaterControlMode.Power)
                        {
                            var nextL2Temp = Convert.ToSingle(nextStep.RecipeCommands["TC1.SetL2TargetSP"]);

                            // 如果当前实时温度已经逼近到下一步的设置温度，则跳步
                            if (PsuL2TempPv < nextL2Temp  && nextL2Temp - PsuL2TempPv < Math.Abs(_tempOffset))
                            {
                                SkipCurrentRecipeStep();

                                EV.PostInfoLog(Module,
                                    $"Current PSU middle temperature is {PsuL2TempPv}℃ and Power mode, next step PSU middle temperature setpoint is {nextL2Temp}℃ and {nextPSUMode} mode." +
                                    $" TempOffset is {_tempOffset}℃. Need jump step!");

                            }
                        }
                    }
                }
            }
        }
    }
}
