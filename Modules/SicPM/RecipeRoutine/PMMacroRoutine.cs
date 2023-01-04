using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicPM.Devices;
using SicPM.RecipeExecutions;
using SicPM.Routines;

namespace SicPM.RecipeRoutine
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

    public enum RoutineType
    {
        DefaultRoutine,
        AtmRoutine,
        VacRoutine,
        ProcessIdleRoutine,
        PumpRoutine,
        VentRoutine,
        PurgeRoutine,
        CleanRoutine,
        ExchangeMORoutine,
        IsolationRoutine,
        LeakCheckRoutine,
        AbortRoutine
    }

    public partial class PMMacroRoutine : PMBaseRoutine
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

        private RecipeRunningState _state = RecipeRunningState.ExecStep;
        private RecipeRunningState _pausedState = RecipeRunningState.ExecStep;
        private DeviceTimer _estimatedTimeCalcTimer = new DeviceTimer();//用于定时计算工艺程序估计的结束时间

        private double _curStepElpasedTimeBeforePaused;

        private double _curStepElpasedTimeBeforePaused2;

        //前序所有步总的时间
        private double _preStepTotalTime;

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

        public int CurStartLoopStep;

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


        private int _currentStepIndex = 99;
        private IoInterLock _pmInterLock;

        private string _recipeName;
        private RoutineType _routineType;

        #region Parse

        private bool _isPSUHeaterJumpMode;
        private bool _isSCRHeaterJumpMode;
        private bool _isMFCJumpMode;

        #endregion

        public PMMacroRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "Routine";

            _dbCallback = new RecipeDBCallback();
            _fdc = new Fdc(Module);
            _pmInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }

        public void Init(string recipeName, RoutineType routineType = RoutineType.DefaultRoutine)
        {
            _recipeName = recipeName;
            _routineType = routineType;
        }

        public bool SetRoutineRunning(bool eValue,out string reason)
        {
            reason = string.Empty;

            if(_routineType == RoutineType.ProcessIdleRoutine)
            {
                if (!_pmInterLock.SetPMProcessIdleRunning(eValue, out  reason))
                {
                    return false;
                }
            }
            else if(_routineType == RoutineType.AtmRoutine)
            {
                if (!_pmInterLock.SetPMAtmIdleRountingRunning(eValue, out  reason))
                {
                    return false;
                }
            }
            else if(_routineType == RoutineType.VacRoutine)
            {
                if (!_pmInterLock.SetPMVacIdleRountingRunning(eValue, out  reason))
                {
                    return false;
                }
            }
            else if (_routineType == RoutineType.CleanRoutine)
            {
                if (!_pmInterLock.SetPMCleanRoutineRunning(eValue, out  reason))
                {
                    return false;
                }
            }
            else if (_routineType == RoutineType.PurgeRoutine)
            {
                if (!_pmInterLock.SetPMPurgeRoutineRunning(eValue, out reason))
                {
                    return false;
                }
            }
            else if (_routineType == RoutineType.PumpRoutine)
            {
                if (!_pmInterLock.SetPMPumpRoutineRunning(eValue, out reason))
                {
                    return false;
                }
            }
            else if (_routineType == RoutineType.VentRoutine)
            {
                if (!_pmInterLock.SetPMVentRoutineRunning(eValue, out reason))
                {
                    return false;
                }
            }
            else if (_routineType == RoutineType.ExchangeMORoutine)
            {
                if (!_pmInterLock.SetPMExchangeMoRoutineRunning(eValue, out  reason))
                {
                    return false;
                }
            }
            else if (_routineType == RoutineType.VacRoutine)
            {

            }
            else if (_routineType == RoutineType.AtmRoutine)
            {

            }
            else
            {
                if (!_pmInterLock.SetPMProcessRunning(eValue, out reason))
                {
                    return false;
                }
            }

            return true;
        }

        public override Result Start(params object[] param)
        {
            Reset();

            //解析Recipe
            if (!RecipeParser.Parse(_recipeName, Module, out var recipeHead, out var recipeSteps, out string reason))
            {
                Stop($"Load recipe {_recipeName} failed, {reason}");

                return Result.FAIL;
            }

            if(!SetRoutineRunning(true,out reason))
            {
                EV.PostAlarmLog(Module, $"can not run Routine, {reason}");
                return Result.FAIL;
            }

            if (_routineType == RoutineType.PurgeRoutine)
            {
                _pmInterLock.DoLidCloseRoutineSucceed = false;
            }
            else if (_routineType == RoutineType.CleanRoutine)
            {
                _pmInterLock.DoLidOpenRoutineSucceed = false;
            }

            //先打开加热
            PMDevice.SetHeatEnable(true);

            _currentStepIndex = 99;

            //设置当前Step/当前循环步/当前总循环步为0
            _currentStepNumber = 0;
            CurStartLoopStep = 0;
            CurrentLoopCount = CurrentLoopCount = 0;

            _preStepTotalTime = 0;

            _dummyStepCount = 0;

            _estimatedTimeCalcTimer.Start(1000);

            PMDevice.RecipeRunningInfo.RecipeName = _recipeName;
            PMDevice.RecipeRunningInfo.Head = recipeHead;
            PMDevice.RecipeRunningInfo.RecipeStepList = recipeSteps;

            PMDevice.RecipeRunningInfo.InnerId = Guid.NewGuid();
            PMDevice.RecipeRunningInfo.BeginTime = DateTime.Now;
            PMDevice.RecipeRunningInfo.TotalTime = CalcRecipeTime();

            _state = RecipeRunningState.ExecStep;

            _recipeTimer.Start(int.MaxValue);

            _dbCallback.RecipeStart(PMDevice.Module, 0, PMDevice.RecipeRunningInfo.InnerId.ToString(), PMDevice.RecipeRunningInfo.RecipeName);

            _dbCallback.RecipeUpdateStatus(PMDevice.RecipeRunningInfo.InnerId.ToString(), "InProcess");

            WaferManager.Instance.UpdateWaferProcessStatus(ModuleHelper.Converter(Module), 0, EnumWaferProcessStatus.InProcess);

            _fdc.Reset();

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

                                if (ContinueAction != RecipeContinueMode.StepContinue)
                                {
                                    _curStepElpasedTimeBeforePaused = 0;
                                    _curStepElpasedTimeBeforePaused2 = 0;
                                }

                                ContinueAction = RecipeContinueMode.None;

                                if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsLoopStartStep)
                                {
                                    CurStartLoopStep = _currentStepNumber;
                                    CurStepTotalLoopCount = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].LoopCount;
                                }

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
                                        int time = (int)PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000;

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
                                                        time = 1;
                                                    }
                                                }
                                            }
                                        }

                                        if (recipeCmd.EndsWith(AITValveOperation.GVTurnValve)) // 阀门
                                        {
                                            OP.DoOperation($"{Module}.{recipeCmd}", PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd]);
                                        }
                                        else
                                        {
                                            OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd]);
                                        }
                                    }
                                }

                                if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].EndBy == EnumEndByCondition.ByTime)
                                    _state = RecipeRunningState.TimeWait;
                                else
                                    _state = RecipeRunningState.ConditionWait;

                                _dbCallback.RecipeStepStart(PMDevice.RecipeRunningInfo.InnerId.ToString(), _currentStepNumber, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName, (float)PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime);
                                _fdc.Start(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands);
                            }
                            break;

                        case RecipeRunningState.TimeWait:
                            {
                                if (IsPaused)
                                {
                                    _state = RecipeRunningState.Paused;
                                }

                                if (_stepTimer.IsTimeout())
                                {
                                    _state = RecipeRunningState.StepCompleted;
                                }
                            }
                            break;

                        case RecipeRunningState.ConditionWait:
                            {
                                //如果暂停，则进入暂停状态
                                if (IsPaused)
                                {
                                    _state = RecipeRunningState.Paused;
                                }

                                //设置的压力
                                double.TryParse(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands["TV.SetPressure"], out double setPressure);

                                //实时反馈的压力
                                double feedBackPressure =  DEVICE.GetDevice<IoPressure>($"{Module}.PT1").FeedBack;

                                double _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");

                                if(Math.Abs(setPressure - feedBackPressure) <= _pmPressureMaxDiff)
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
                                _preStepTotalTime += CurStepTotalTime/1000;

                                //放在前面，stepnumber后面会被更新
                                _stepTimer2.Stop();

                                _dbCallback.RecipeStepEnd(PMDevice.RecipeRunningInfo.InnerId.ToString(), _currentStepNumber, _fdc.DataList);
                                _fdc.Stop();

                                if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].IsLoopEndStep)
                                {
                                    CurrentLoopCount++;
                                    if (CurrentLoopCount >= CurStepTotalLoopCount)
                                    {
                                        CurrentLoopCount = CurStepTotalLoopCount = 0;
                                        CurStartLoopStep = 0;
                                        _currentStepNumber++;
                                    }
                                    else
                                    {
                                        _currentStepNumber = CurStartLoopStep;
                                    }
                                }
                                else
                                {
                                    _currentStepNumber++;
                                }


                                //判断最后一步是否执行完
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
                                _recipeTimer.Stop();

                                if (!SetRoutineRunning(false, out reason))
                                {
                                    EV.PostAlarmLog(Module, $"can not run Process, {reason}");
                                    return Result.FAIL;
                                }

                                //设置Succeed状态
                                if(_routineType == RoutineType.PurgeRoutine)
                                {
                                    _pmInterLock.SetLidOpenRoutineSucceed(true, out reason);
                                }
                                else if (_routineType == RoutineType.CleanRoutine)
                                {
                                    _pmInterLock.SetLidClosedRoutineSucceed(true, out  reason);
                                }

                                //设置当前Step/当前循环步/当前总循环步为0
                                _currentStepNumber = 0;
                                CurStartLoopStep = 0;
                                CurrentLoopCount = CurrentLoopCount = 0;


                                Notify("Finished");
                                return Result.DONE;
                            }

                        case RecipeRunningState.Error:
                            {
                                if (!SetRoutineRunning(false, out reason))
                                {
                                    EV.PostAlarmLog(Module, $"can not run Process, {reason}");
                                    return Result.FAIL;
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

                    if (!SetRoutineRunning(false, out reason))
                    {
                        EV.PostAlarmLog(Module, $"can not run Process, {reason}");
                        return Result.FAIL;
                    }

                    LOG.Write(ex);
                    return Result.FAIL;
                }
            }

            return Result.RUN;
        }


        private void MonitorRecipeRunInfo()
        {
            PMDevice.RecipeRunningInfo.StepNumber = _currentStepNumber + 1 - _dummyStepCount; //CurStepNum start from 0, ignore dummy step
            PMDevice.RecipeRunningInfo.StepName = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName;
            PMDevice.RecipeRunningInfo.StepTime = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime;
            PMDevice.RecipeRunningInfo.StepElapseTime = _stepTimer.IsIdle() ? _curStepElpasedTimeBeforePaused / 1000 : (_stepTimer.GetElapseTime() + _curStepElpasedTimeBeforePaused) / 1000;
            PMDevice.RecipeRunningInfo.TotalElapseTime = CalcElapseRecipeTime();

            PMDevice.RecipeRunningInfo.StepElapseTime2 = _stepTimer2.IsIdle() ? _curStepElpasedTimeBeforePaused2 / 1000 : (_stepTimer2.GetElapseTime() + _curStepElpasedTimeBeforePaused2) / 1000;
            PMDevice.RecipeRunningInfo.TotalElapseTime2 = _recipeTimer.GetElapseTime() / 1000;

            if (_currentStepIndex != PMDevice.RecipeRunningInfo.StepNumber)
            {
                Notify($"Start Recipe: Step:{PMDevice.RecipeRunningInfo.StepNumber}  Name:{PMDevice.RecipeRunningInfo.StepName} ");
                _currentStepIndex = PMDevice.RecipeRunningInfo.StepNumber;
            }
        }

        public override void Abort()
        {
            _state = RecipeRunningState.RecipeCompleted;

            _dbCallback.RecipeFailed(PMDevice.RecipeRunningInfo.InnerId.ToString());
            _fdc.Stop();

            PMDevice.RecipeRunningInfo.StepName = string.Empty;
            PMDevice.RecipeRunningInfo.StepNumber = 0;
            PMDevice.RecipeRunningInfo.StepTime = 0;
            PMDevice.RecipeRunningInfo.StepElapseTime = 0;
            PMDevice.RecipeRunningInfo.TotalTime = 0;
            PMDevice.RecipeRunningInfo.TotalElapseTime = 0;

            if (!SetRoutineRunning(false, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not run Process, {reason}");
            }
            //根据Routine类型执行Abort
            SetRoutineAbort();

            base.Abort();
        }

        private List<string> _lstGroup1 = new List<string>() { "V46", "V46s", "V73" };
        private List<string> _lstGroup2 = new List<string>() { "V46", "V46s" };
        private List<string> _lstGroup3 = new List<string>() { "V43", "V43s", "V45" };

        //默认为TMA换源参数
        private int mfc7Or10 = 7;
        private int mfc8Or12 = 8;
        private int mfc11 = 11;
        private int pc2Or3 = 2;

        public void SetRoutineAbort()
        {
            if (_routineType == RoutineType.ProcessIdleRoutine)
            {
                PMDevice._ioThrottleValve.StopRamp();
                PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
                PMDevice.SetHeaterStopRamp();
                PMDevice.SetRotationStopRamp();
                PMDevice.SetRotationServo(0, 0);
            }
            else if (_routineType == RoutineType.CleanRoutine)
            {
                PMDevice._ioThrottleValve.StopRamp();
                PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
                PMDevice.SetRotationServo(0, 0);
            }
            else if (_routineType == RoutineType.PurgeRoutine)
            {
                PMDevice._ioThrottleValve.StopRamp();
                PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
                PMDevice.SetRotationServo(0, 0);
            }
            else if (_routineType == RoutineType.PumpRoutine)
            {
                PMDevice.SetRotationServo(0, 0);
            }
            else if (_routineType == RoutineType.VentRoutine)
            {
                PMDevice._ioThrottleValve.StopRamp();
                PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
                PMDevice.SetRotationServo(0, 0);
            }
            else if (_routineType == RoutineType.ExchangeMORoutine)
            {
                PMDevice.SetIoValue(_lstGroup1, false);
                PMDevice.SetIoValue(_lstGroup2, false);
                PMDevice.SetMfcValueToDefault(new List<int> { mfc7Or10 });
                PMDevice.SetMfcValueToDefault(new List<int> { mfc8Or12 });
                PMDevice.SetMfcValueToDefault(new List<int> { mfc11 });
                PMDevice.SetPCValueToDefault(new List<int> { pc2Or3 });
                PMDevice.SetRotationServo(0, 0);
            }
            else if (_routineType == RoutineType.VacRoutine)
            {
                PMDevice.SetRotationServo(0, 0);
            }
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
                    int loopTimes = PMDevice.RecipeRunningInfo.RecipeStepList[i].LoopCount == 0 ? 1 : PMDevice.RecipeRunningInfo.RecipeStepList[i].LoopCount;
                    total += PMDevice.RecipeRunningInfo.RecipeStepList[i].StepTime * loopTimes;
                }
            }

            return (int)total;
        }

        protected int CalcElapseRecipeTime()
        {
            double total = 0;
            //for (int i = 0; i < _currentStepNumber; i++)
            //{
            //    if (!PMDevice.RecipeRunningInfo.RecipeStepList[i].IsDummyStep) // 不统计虚拟Step的时间
            //    {
            //        total += PMDevice.RecipeRunningInfo.RecipeStepList[i].StepTime;
            //    }
            //}
            total = _preStepTotalTime;
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
    }
}
