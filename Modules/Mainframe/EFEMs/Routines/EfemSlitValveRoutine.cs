using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.LLs;
using Mainframe.LLs.Routines;
using Mainframe.TMs;
using Mainframe.UnLoads;
using Mainframe.UnLoads.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Utilities;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad;

namespace Mainframe.EFEMs.Routines
{
    public class EfemSlitValveRoutine : EfemBaseRoutine
    {
        enum RoutineStep
        {
            ServoRoutine,
            LiftRoutine,
            CheckPressureCondition,
            CheckPressure,
            SetVentValve,
            TimeDelay0,
            SetSlitValve,
            TimeDelay1,
            CheckPressureCondition2,
            TimeDelay10
        }

        private int _motionTimeout = 30;
        private ModuleName _paramTarget;    //目标腔体
        private ModuleName _robot;          //机械手
        private bool _paramIsOpen;          //是否开门
        private bool _bothPressureIsATM;    //判断两边压力是否都为ATM
        private SicTM _tm;
        private LoadLock _loadLock;
        private UnLoad _unLoad;
        private LoadLockServoToRoutine _llServoToRoutine;
        private UnLoadServoToRoutine _unLoadServoToRoutine;
        private LoadLockLiftRoutine _llLiftRoutine;
        private UnLoadLiftRoutine _unLoadLiftRoutine;

        private double _maxPressureDiffOpenSlitValve;

        public void Init(ModuleName targetModule,ModuleName robot, bool isOpen)
        {
            _paramTarget = targetModule;
            _robot = robot;
            _paramIsOpen = isOpen;
        }

        public EfemSlitValveRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ ModuleName.TM.ToString()}");
            _loadLock= DEVICE.GetDevice<SicLoadLock>($"{ ModuleName.LoadLock.ToString()}.{ ModuleName.LoadLock.ToString()}");
            _unLoad = DEVICE.GetDevice<SicUnLoad>($"{ ModuleName.UnLoad.ToString()}.{ ModuleName.UnLoad.ToString()}");
            _llServoToRoutine = new LoadLockServoToRoutine(ModuleName.LoadLock);
            _unLoadServoToRoutine = new UnLoadServoToRoutine(ModuleName.UnLoad);

            _llLiftRoutine = new LoadLockLiftRoutine();
            _unLoadLiftRoutine = new UnLoadLiftRoutine();
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            //只有LoadLock和UnLoad需要开门
            if (_robot == ModuleName.WaferRobot)
            {
                if (_paramTarget != ModuleName.LoadLock  && _paramTarget != ModuleName.Load && _paramTarget != ModuleName.UnLoad)
                {
                    return Result.DONE;
                }
            }
            else if (_robot == ModuleName.TrayRobot)
            {
                if (_paramTarget != ModuleName.LoadLock)
                {
                    return Result.DONE;
                }
            }
            else
            {
                return Result.DONE;
            }

            if (_paramIsOpen)
            {
                if (EfemDevice.CheckSlitValveOpen(_paramTarget, _robot))
                {
                    return Result.DONE;
                }
                //判断TM到LoadLock或者UnLoad的门是否关上
                if (!_tm.CheckSlitValveClose(_paramTarget))
                {
                    EV.PostWarningLog(Module, $"can not open slit valve {_paramTarget},  {_paramTarget} to TM slit valve not closed, can not open at same time");
                    return Result.FAIL;
                }
                if (!EfemDevice.CheckSlitValveClose(ModuleName.LoadLock, ModuleName.WaferRobot)) //此门是必须要关上的
                {
                    EV.PostWarningLog(Module, $"can not open slit valve {_paramTarget},  {ModuleName.LoadLock} to Efem slit valve not closed, can not open at same time");
                    return Result.FAIL;
                }
                if (!EfemDevice.CheckSlitValveClose(ModuleName.UnLoad, _robot))
                {
                    EV.PostWarningLog(Module, $"can not open slit valve {_paramTarget},  {ModuleName.UnLoad} to Efem slit valve not closed, can not open at same time");
                    return Result.FAIL;
                }
                if (!EfemDevice.CheckSlitValveClose(ModuleName.LoadLock, _robot))
                {
                    EV.PostWarningLog(Module, $"can not open slit valve {_paramTarget},  {ModuleName.LoadLock} to Efem slit valve not closed, can not open at same time");
                    return Result.FAIL;
                }

                _maxPressureDiffOpenSlitValve = SC.GetValue<double>("EFEM.MaxPressureDiffOpenSlitValve");
                _motionTimeout = SC.GetValue<int>("EFEM.SlitValveMotionTimeout");

                _llServoToRoutine.Init(SC.GetValue<double>("LoadLock.AtmPressureBase"));
                _unLoadServoToRoutine.Init(SC.GetValue<double>("UnLoad.AtmPressureBase"));
                _llLiftRoutine.Init(false);
                _unLoadLiftRoutine.Init(false);
            }
            else
            {
                if (EfemDevice.CheckSlitValveClose(_paramTarget, _robot))
                {
                    return Result.DONE;
                }
            }

            Notify("Start");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                //开门之前需要对两边腔体进行气压调节,降Lift
                if (_paramIsOpen)
                {
                    if (_paramTarget == ModuleName.LoadLock)
                    {
                        CheckPressureConditionATM((int)RoutineStep.CheckPressureCondition, _paramTarget);//先判断Load腔是否在大气，因为EFEM始终在大气

                        if (!_bothPressureIsATM)
                        {
                            ExecuteRoutine((int)RoutineStep.ServoRoutine, _llServoToRoutine);
                            ExecuteRoutine((int)RoutineStep.LiftRoutine, _llLiftRoutine);
                        }
                    }
                    else if (_paramTarget == ModuleName.UnLoad)
                    {
                        CheckPressureConditionATM((int)RoutineStep.CheckPressureCondition, _paramTarget);//先判断UnLoad腔是否在大气

                        if (!_bothPressureIsATM)
                        {
                            ExecuteRoutine((int)RoutineStep.ServoRoutine, _unLoadServoToRoutine);
                            ExecuteRoutine((int)RoutineStep.LiftRoutine, _unLoadLiftRoutine);
                        }
                    }

                    CheckPressureCondition((int)RoutineStep.CheckPressureCondition2,_paramTarget);
                }

                // 打开闸板阀前先Vent
                if (_paramIsOpen)
                {
                    SetVentValve((int)RoutineStep.SetVentValve, _paramTarget, true);
                    TimeDelay((int)RoutineStep.TimeDelay0, 2);
                }

                SetSlitValve((int)RoutineStep.SetSlitValve, _paramTarget, _robot, _paramIsOpen, _motionTimeout);

                // 关闭闸板阀以后关闭Vent
                if (!_paramIsOpen)
                {
                    TimeDelay((int)RoutineStep.TimeDelay1, 2);
                    SetVentValve((int)RoutineStep.SetVentValve, _paramTarget, false);
                }

                TimeDelay((int)RoutineStep.TimeDelay10, 3);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify("Finished");
            return Result.DONE;

        }

        public void CheckPressureConditionATM(int id, ModuleName target)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"check pressure condition to open {target} slit valve");
                double targetPressure = 0.0;
                if (target == ModuleName.LoadLock)
                {
                    targetPressure = _loadLock.ChamberPressure;
                }
                else
                {
                    targetPressure = _unLoad.ChamberPressure;
                }

                double pressureDiff = Math.Abs(EfemDevice.ChamberPressure - targetPressure);
                _bothPressureIsATM = pressureDiff <= _maxPressureDiffOpenSlitValve;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void CheckPressureCondition(int id, ModuleName target)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"check {target} pressure is atm");
                double targetPressure = 0.0;
                if (target == ModuleName.LoadLock)
                {
                    targetPressure = _loadLock.ChamberPressure;
                }
                else
                {
                    targetPressure = _unLoad.ChamberPressure;
                }

                double pressureDiff = Math.Abs(EfemDevice.ChamberPressure - targetPressure);
                if (pressureDiff > _maxPressureDiffOpenSlitValve)
                {
                    Stop($"pressure difference {pressureDiff:F3}  between Efem and {target} exceed tolerance {_maxPressureDiffOpenSlitValve}");
                    return false;
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
                    throw (new RoutineBreakException());
            }
        }

        public void SetVentValve(int id, ModuleName target, bool isOpen)
        {
            var ret = Execute(id, () =>
            {
                Notify(isOpen ? $"start to vent" : $"stop venting");

                if (target == ModuleName.LoadLock || target == ModuleName.LoadLock)
                {
                    if (!_loadLock.SetSlowVentValve(isOpen, out var reason))
                    {
                        Stop(reason);
                        return false;
                    }
                }
                else if(target == ModuleName.UnLoad)
                {
                    if (!_unLoad.SetSlowVentValve(isOpen, out var reason))
                    {
                        Stop(reason);
                        return false;
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
                    throw (new RoutineBreakException());
            }
        }


        public void SetSlitValve(int id, ModuleName module,ModuleName _robort, bool isOpen, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"set slit valve {module} " + (isOpen ? "Open" : "Close"));

                if (!EfemDevice.SetSlitValve(module, _robort, isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return isOpen ? EfemDevice.CheckSlitValveOpen(module, _robort) : EfemDevice.CheckSlitValveClose(module, _robort);

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"can not complete in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

    }

}
