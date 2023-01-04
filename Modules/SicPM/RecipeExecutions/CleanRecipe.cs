using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicPM.Routines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicPM.RecipeExecutions
{
    public class CleanRecipe : PMBaseRoutine
    {
        public enum RoutineStep
        {
            SetServoUp,
            SetChamberPressure,
            EnableRotation,
            OpenH2Valve,
            EnableHeater,
            PcCalibartion,
            RunStepOne,
            Wait,
            CheckCondition,
            WaitTempReach,
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

        private string _recipeName;

        private object _recipeLocker = new object();
        private bool _isPSUHeaterJumpMode;
        private bool _isSCRHeaterJumpMode;
        private bool _isMFCJumpMode;

        private int _currentStepNumber;

        private DeviceTimer _stepTimer = new DeviceTimer();
        private DeviceTimer _recipeTimer = new DeviceTimer();
        private RecipeRunningState _state = RecipeRunningState.ExecStep;

        private PMServoToPressure _pmServoToPressure;



        public CleanRecipe(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "CleanRecipe";
            _pmServoToPressure = new PMServoToPressure(module, pm);

        }

        public void Init(string recipeName)
        {
            if (!recipeName.Contains("Sic\\Clean\\") && recipeName!="")
            {
                recipeName = "Sic\\Clean\\" + recipeName;
            }
            _recipeName = recipeName;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (!RecipeParser.Parse(_recipeName, Module, out var recipeHead, out var recipeSteps, out string reason))
            {
                Stop($"Load recipe {_recipeName} failed, {reason}");
                return Result.FAIL;
            }

            if (WaferManager.Instance.CheckHasWafer(PMDevice.Module, 0))
            {
                EV.PostWarningLog(Module, $"can not run clean recipe, Has wafer at {Module}");
                return Result.FAIL;
            }
            if (!PMDevice.Pump.IsOn || PMDevice.Pump.IsError)
            {
                EV.PostWarningLog(Module, $"can not run process, pump state is not correct");
                return Result.FAIL;
            }
            if (!PMDevice._ioThrottleValve.TVValveEnable)
            {
                EV.PostWarningLog(Module, $"can not run process, TV is not enable");
                return Result.FAIL;
            }

            if (PMDevice._ioThrottleValve.ControlModeFeedback != PressureCtrlMode.TVPressureCtrl)
            {
                EV.PostWarningLog(Module, $"can not run process, TV is not Pressure Ctrol Mode");
                return Result.FAIL;
            }

            if (!PMDevice.CheckIOValueByGroup(IoGroupName.I, true))
            {
                EV.PostAlarmLog(Module, $"can not run process,I valves is not open!");
                return Result.FAIL;
            }
            if (!PMDevice.CheckIOValueByGroup(IoGroupName.E, false))
            {
                EV.PostAlarmLog(Module, $"can not run process,E valves is not closed!");
                return Result.FAIL;
            }
            if (!PMDevice.CheckIOValueByGroup(IoGroupName.K, false))
            {
                EV.PostAlarmLog(Module, $"can not run process,K valves is not closed!");
                return Result.FAIL;
            }
            if (!PMDevice.CheckIOValueByGroup(IoGroupName.VentPump, true))
            {
                EV.PostAlarmLog(Module, $"can not run process,V72 is not open!");
                return Result.FAIL;
            }
            if (!PMDevice.PSU1.AllHeatEnable)
            {
                EV.PostAlarmLog(Module, $"can not run process,Heater is disable!");
                return Result.FAIL;
            }
            _currentStepNumber = 0;
            PMDevice.RecipeRunningInfo.RecipeName = _recipeName;
            PMDevice.RecipeRunningInfo.Head = recipeHead;
            PMDevice.RecipeRunningInfo.RecipeStepList = recipeSteps;
            PMDevice.RecipeRunningInfo.TotalTime = CalcRecipeTime();
            _state = RecipeRunningState.ExecStep;

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                SetConfinementRingUpAndWait((int)RoutineStep.SetServoUp, 30);

                ExecuteRoutine((int)RoutineStep.SetChamberPressure, _pmServoToPressure);

                EnableRotation((int)RoutineStep.EnableRotation, 5);

                OpenH2Valve((int)RoutineStep.OpenH2Valve, PMDevice, 2);

                EnableHeater((int)RoutineStep.EnableHeater, PMDevice, 5);

                MonitorRecipeRunInfo();

                lock (_recipeLocker)
                {
                    try
                    {
                        switch (_state)
                        {
                            case RecipeRunningState.ExecStep:

                                if (_currentStepNumber == 0)
                                {
                                    _recipeTimer.Start(int.MaxValue);
                                }

                                _stepTimer.Start(PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime * 1000);

                                Notify($"Running step {_currentStepNumber + 1} : {PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName}");


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

                                    if (!OP.CanDoOperation($"{Module}.{recipeCmd}", out string reason, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd]))
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

                                        OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].RecipeCommands[recipeCmd]);                                        
                                    }
                                }

                                if (PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].EndBy == EnumEndByCondition.ByTime)
                                    _state = RecipeRunningState.TimeWait;
                                else
                                    _state = RecipeRunningState.ConditionWait;
                                break;

                            case RecipeRunningState.TimeWait:
                                if (_stepTimer.IsTimeout())
                                {
                                    _state = RecipeRunningState.StepCompleted;
                                }
                                break;
                            case RecipeRunningState.ConditionWait:
                                {
                                    if (_stepTimer.IsTimeout())
                                    {
                                        _state = RecipeRunningState.StepCompleted;
                                    }
                                }
                                break;
                            case RecipeRunningState.StepCompleted:
                                _currentStepNumber++;
                                if (_currentStepNumber >= PMDevice.RecipeRunningInfo.RecipeStepList.Count)
                                {
                                    _currentStepNumber = PMDevice.RecipeRunningInfo.RecipeStepList.Count - 1;
                                    _state = RecipeRunningState.RecipeCompleted;
                                }
                                else
                                {
                                    _state = RecipeRunningState.ExecStep;
                                }
                                break;
                            case RecipeRunningState.RecipeCompleted:
                                {
                                    _recipeTimer.Stop();
                                    Notify("Finished");
                                    return Result.DONE;
                                }
                        }

                        return Result.RUN;
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        return Result.FAIL;
                    }
                }
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }
            catch (Exception ex)
            {
                LOG.Write(ex, String.Format("CleanRecipe has exception"));
                throw (ex);
            }

            Notify("Finished");

            return Result.DONE;
        }

        protected void EnableHeater(int id, PMModuleBase pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {pm.Name} Heater Enable");

                if (!pm.EnableHeater(true, out string reason))
                {
                    Stop($"Set {pm.Name} Heater Enable failed, {reason}");
                    return false;
                }

                return true;
            }, () =>
            {
                return pm.CheckHeaterEnable();
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Set {pm.Name} Heater Enable timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
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

        private void MonitorRecipeRunInfo()
        {
            try
            {
                PMDevice.RecipeRunningInfo.StepNumber = _currentStepNumber + 1;
                PMDevice.RecipeRunningInfo.StepName = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepName;
                PMDevice.RecipeRunningInfo.StepTime = PMDevice.RecipeRunningInfo.RecipeStepList[_currentStepNumber].StepTime;                
                PMDevice.RecipeRunningInfo.TotalElapseTime = _recipeTimer.GetElapseTime() / 1000;

            }
            catch (Exception ex)
            {

            }
        }

    }
}
