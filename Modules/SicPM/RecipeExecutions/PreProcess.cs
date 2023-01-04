using System;
using System.Collections.Generic;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicPM.Devices;
using SicPM.Routines;

namespace SicPM.RecipeExecutions
{
    public class PreProcess : PMBaseRoutine
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
            SetRotation,
            TimeDelay1,

            //DelayForTest,

            //CheckDoor,
            //CheckDryPump,

            //CloseSlitValve,

            //CloseAllVavle,

            //OpenPumpingValve,

            //OpenGateVlave,

            //CheckVAC,

            //SetRfMatchMode,

            //SetElectrodeTemp,

            //NoteInProcess,

            //NoteProcessFailed,
            //SetThrottlePressureControlMode,

            //Pump,

            //End,
            //NoteProcessComplete,
        }

        private double _pumpBasePressure;
        private int _pumpTimeout;
        private string _recipeName;
        private bool _isCleanRecipe;
        private bool _withWafer;

        private bool _isSlitDoorOpened;
        private ThrottleValveBase _throttleValve;
        private bool _isThrottlePressureControlMode;
        private Guid _guidProcess;

        private bool _isDryRun;
        private bool _isPSUHeaterJumpMode;
        private bool _isSCRHeaterJumpMode;
        private bool _isMFCJumpMode;
        private int _delayTimeRamp;

        private double _targetPressure;

        private double _preTemp;

        private int _rotationSpeed;

        private IoInterLock _pmInterLock;

        private PMServoToPressure _pmServoToPressure;
        private PMPcCalibrationRoutine _pmPcCalibrationRoutine;

        public PreProcess(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "PreProcess";

            _pmInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
            _pmServoToPressure = new PMServoToPressure(module, pm);
            _pmPcCalibrationRoutine = new PMPcCalibrationRoutine(module, pm);
        }

        public void Init(string recipeName, bool isClean, bool withWafer)
        {
            _recipeName = recipeName;
            _isCleanRecipe = isClean;
            _withWafer = withWafer;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (!RecipeParser.Parse(_recipeName, Module, out var recipeHead, out var recipeSteps, out string reason))
            {
                Stop($"Load recipe {_recipeName} failed, {reason}");

                return Result.FAIL;
            }


            if (!RecipeParser.CheckDataAvalible(_recipeName, Module, out string reason1))
            {
                Stop($"Recipe data is error! " + reason1);
                return Result.FAIL;
            }


            if (!_withWafer && WaferManager.Instance.CheckHasWafer(PMDevice.Module, 0))
            {
                EV.PostWarningLog(Module, $"can not run waferless process, Has wafer at {Module}");
                return Result.FAIL;
            }

            if (_withWafer && !WaferManager.Instance.CheckHasWafer(PMDevice.Module, 0))
            {
                EV.PostWarningLog(Module, $"can not run process, no wafer at {Module}");
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

            if (!_pmInterLock.SetPMPreProcessRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not run preProcess, {reason}");
                return Result.FAIL;
            }

            _rotationSpeed = SC.GetValue<int>($"PM.{Module}.ProcessIdle.RotationSpeed");

            _isDryRun = SC.GetValue<bool>($"PM.{Module}.DryRun.IsDryRun");
            _delayTimeRamp = SC.GetValue<int>($"PM.{Module}.PreProcess.RampDelayTime");

            _targetPressure = SC.GetValue<double>($"PM.{Module}.PreProcess.ChamberPressure");
            _preTemp = SC.GetValue<double>($"PM.{Module}.PreProcess.ProcessBeginTempCondition"); //等待温度到达这个温度才开始工艺
            _pmServoToPressure.Init(_targetPressure);

            _guidProcess = Guid.NewGuid();

            PMDevice.RecipeRunningInfo.RecipeName = _recipeName;
            PMDevice.RecipeRunningInfo.Head = recipeHead;
            PMDevice.RecipeRunningInfo.RecipeStepList = recipeSteps;

            Notify($"Start");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                EnableRotation((int)RoutineStep.EnableRotation, 5);
                SetRotationValveAndNoWait((int)RoutineStep.SetRotation, _rotationSpeed);

                TimeDelay((int)RoutineStep.TimeDelay1, 5);

                SetConfinementRingUpAndWait((int)RoutineStep.SetServoUp, 30);

                ExecuteRoutine((int)RoutineStep.SetChamberPressure, _pmServoToPressure);

                OpenH2Valve((int)RoutineStep.OpenH2Valve, PMDevice, 2);

                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                {
                    return Result.DONE;
                }

                EnableHeater((int)RoutineStep.EnableHeater, PMDevice, 5);

                //ExecuteRoutine((int)RoutineStep.PcCalibartion, _pmPcCalibrationRoutine);

                if (!_isDryRun) // 空跑工艺
                {
                    RunStepOne((int)RoutineStep.RunStepOne, PMDevice, _delayTimeRamp);

                    Wait((int)RoutineStep.Wait, $"Chamber:{Name}:Wait", _delayTimeRamp);

                    CheckCondition((int)RoutineStep.CheckCondition, PMDevice, PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands);

                    WaitTempratureToBeginProcess((int)RoutineStep.WaitTempReach, _preTemp, 10000);
                }
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                //NoteProcessFailed((int)Routine.NoteProcessFailed);
                return Result.FAIL;
            }
            catch (Exception ex)
            {
                LOG.Write(ex, String.Format("Preprocess Monitor has exception"));
                throw (ex);
            }

            _pmInterLock.SetPMPreProcessRunning(false, out string reason);
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

        protected void RunStepOne(int id, PMModuleBase pm, int delayTime)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Run {pm.Name} recipe step one");

                string reason = string.Empty;

                //执行工艺程序命令
                foreach (var recipeCmd in PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands.Keys)
                {
                    if (recipeCmd == "SusHeaterSetMode")
                    {
                        if (PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd] == "Jump")
                            _isPSUHeaterJumpMode = true;
                        else
                            _isPSUHeaterJumpMode = false;

                        continue;
                    }
                    if (recipeCmd == "WWHeaterSetMode")
                    {
                        if (PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd] == "Jump")
                            _isSCRHeaterJumpMode = true;
                        else
                            _isSCRHeaterJumpMode = false;

                        continue;
                    }
                    if (recipeCmd == "FlowSetMode")
                    {
                        if (PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd] == "Jump")
                            _isMFCJumpMode = true;
                        else
                            _isMFCJumpMode = false;

                        continue;
                    }

                    if (IsCmdSkip(recipeCmd))
                        continue;

                    if (!OP.CanDoOperation($"{Module}.{recipeCmd}", out reason, PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd]))
                    {
                        Stop($"Can not execute {recipeCmd}, {reason}");
                        return false;
                    }
                    else
                    {
                        int time = delayTime * 1000;
                        if (recipeCmd.StartsWith("TC1") && _isPSUHeaterJumpMode)
                        {
                            time = 1;
                        }
                        if (recipeCmd.StartsWith("TC2") && _isSCRHeaterJumpMode)
                        {
                            time = 1;
                        }
                        if (recipeCmd.StartsWith("Mfc") && recipeCmd.EndsWith(".Ramp") && _isMFCJumpMode)
                        {
                            time = 1;
                        }

                        //if (recipeCmd == "Pressure5.Ramp")
                        //{
                        //    double pc5Pressure = Convert.ToDouble(PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd]) + _pc5Offset;
                        //    OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, pc5Pressure.ToString());
                        //}
                        //else if (recipeCmd == "Pressure6.Ramp")
                        //{
                        //    double pc6Pressure = Convert.ToDouble(PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd]) + _pc6Offset;
                        //    OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, pc6Pressure.ToString());
                        //}
                        //else if (recipeCmd == "Pressure7.Ramp")
                        //{
                        //    double pc7Pressure = Convert.ToDouble(PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd]) + _pc7Offset;
                        //    OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, pc7Pressure.ToString());
                        //}
                        //else
                        {
                            OP.DoOperation($"{Module}.{recipeCmd}", out string reason1, time, PMDevice.RecipeRunningInfo.RecipeStepList[0].RecipeCommands[recipeCmd]);
                        }
                    }
                }

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        public void Wait(int id, string stepName, int delayTime)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                Notify($"Wait for {delayTime} seconds");

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
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void CheckCondition(int id, PMModuleBase pm, Dictionary<string, string> recipeCommands)
        {
            //Tuple<bool, Result> ret = Execute(id, () =>
            //{
            //    Notify($"Run {pm.Name} check condition");

            //    if (!pm.CheckPreProcessCondition(recipeCommands, out string reason))
            //    {
            //        Stop(reason);
            //        return false;
            //    }

            //    return true;
            //});

            //if (ret.Item1)
            //{
            //    if (ret.Item2 == Result.FAIL)
            //    {
            //        throw (new RoutineFaildException());
            //    }
            //    else
            //    {
            //        throw (new RoutineBreakException());
            //    }
            //}
            string reason = String.Empty;
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                if (!pm.CheckPreProcessCondition(recipeCommands, out reason))
                {
                    return false;
                }

                return true;
            }, 60 * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop(reason);
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop(reason);
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        /// <summary>
        /// 等待温度到达工艺温度
        /// </summary>
        protected void WaitTempratureToBeginProcess(int id,double targetTemp,int timeout)
        {
            string reason = String.Empty;
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                if (PMDevice.TC1.L2PVFeedBack >= targetTemp)
                {
                    return true;
                }

                return false;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop(reason);
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop(reason);
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void NoteInProcess(int id, int slot)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update process information, process in {ModuleName.PM1} start");

                ModuleName mod = ModuleName.PM1;

                WaferManager.Instance.UpdateWaferProcessStatus(mod, slot, EnumWaferProcessStatus.InProcess);

                //_pm.OnProcessStart(_guidProcess.ToString(), _recipeName, false);

                return true;
            });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }


        protected void NoteProcessComplete(int id, int slot)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update process information, process in {ModuleName.PM1} completed");

                ModuleName mod = ModuleName.PM1;

                WaferManager.Instance.UpdateWaferProcessStatus(mod, slot, EnumWaferProcessStatus.Completed);

                return true;
            });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }

        public void Exit()
        {

        }

        public override void Abort()
        {
            _pmInterLock.SetPMPreProcessRunning(false, out string reason);
            PMDevice.AbortRunProcess(out reason);

            //_pm.StopAllGasFlow(out string reason);
            //_pm.SetPowerOnOff(false, out reason);
            //_pm.SetPower(0, false);
            //_pm.CloseValvesForPumping(out reason);

            //_executor.RecipeRunningInfo.Step = 0;
            //_executor.RecipeRunningInfo.ElapsedTime = 0;
        }

    }
}
