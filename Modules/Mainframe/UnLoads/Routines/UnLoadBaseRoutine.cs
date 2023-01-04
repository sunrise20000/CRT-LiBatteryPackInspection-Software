using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Mainframe.Devices;
using Mainframe.EFEMs;
using Mainframe.LLs.Routines;
using Mainframe.TMs;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.Core;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.EFEM;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad;
using Sicentury.Core;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadBaseRoutine : LoadLockBaseRoutine, IRoutine
    {
        protected UnLoad UnLoadDevice
        {
            get { return _unLoad; }
        }
        protected TM TMDevice
        {
            get { return _tm; }
        }

        protected EFEM EFEMDevice
        {
            get { return _efem; }
        }

        protected IoClaw WaferClaw
        {
            get { return _ulWaferCyShuttle; }
        }

        protected IoLift4 Lift
        {
            get { return _ulLift; }
        }

        protected IoInterLock TmIoInterLock
        {
            get { return _tmIoInterLock; }
        }



        private UnLoad _unLoad = null;
        private TM _tm = null;
        private EFEM _efem = null;
        private IoClaw _ulWaferCyShuttle;
        private IoLift4 _ulLift;
        private IoInterLock _tmIoInterLock;

        public UnLoadBaseRoutine()
        {
            Module = ModuleName.UnLoad.ToString();

            _unLoad = DEVICE.GetDevice<SicUnLoad>($"{Module}.{Module}");
            _tm = DEVICE.GetDevice<SicTM>($"{ModuleName.System}.{ModuleName.TM}");
            _efem= DEVICE.GetDevice<SicEFEM>($"{ModuleName.EFEM}.{ModuleName.EFEM}");
            _ulWaferCyShuttle= DEVICE.GetDevice<IoClaw>($"{Module}.UnLoadWaferClaw");
            _ulLift = DEVICE.GetDevice<IoLift4>($"{Module}.UnLoadLift");
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
        }

        public override void Abort()
        {
            UnLoadDevice.SetFastVentValve(false, out _);
            UnLoadDevice.SetSlowPumpValve(false, out _);
            UnLoadDevice.SetFastPumpValve(false, out _);

            TmIoInterLock.SetUnloadVentRoutineRunning(false, out _);
            TmIoInterLock.SetUnloadPumpRoutineRunning(false, out _);
            TmIoInterLock.SetUnloadLeakCheckRoutineRunning(false, out _);
            TmIoInterLock.SetUnloadPurgeRoutineRunning(false, out _);
            
            base.Abort();

        }

        public virtual Result Monitor()
        {
            return Result.DONE;
        }

        public virtual Result Start(params object[] objs)
        {
            return Result.RUN;
        }

        protected void LiftMove(int id, bool up, int timeout)
        {
            string note = up ? "Up" : "Down";
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} Lift to {note}");
                if (up)
                {
                    if (!Lift.MoveUp(out string reason))
                    {
                        Stop($"Set {Module} Lift to {note} failed:" + reason);
                        return false;
                    }
                }
                else
                {
                    if (!Lift.MoveDown(out string reason))
                    {
                        Stop($"Set {Module} Lift to {note} failed:" + reason);
                        return false;
                    }
                }

                return true;
            }, () =>
            {
                if (up)
                {
                    return Lift.IsUp && !Lift.IsDown;
                }
                else
                {
                    return !Lift.IsUp && Lift.IsDown;
                }

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set {Module} Lift to {note} Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void ClawMove(int id, IoClaw _claw, bool up, int timeout)
        {
            string note = up ? "Clawing" : "Opening";
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} {note}");
                if (!_claw.SetValue(up, out string reason))
                {
                    Stop($"Set {Module} {note} failed:" + reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _claw.State == (up ? ClawStateEnum.Clamp : ClawStateEnum.Open);
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set {Module} {note} Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void SlowPump(int id, double switchPressure, int timeout)
        {
            var reason = "";

            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {UnLoadDevice.Name} slow pump valve to {switchPressure} mbar");

                // 锁定Pump2
                if (!LockPump2(out reason, timeout * 1000))
                {
                    Stop(reason);
                    return false;
                }

                if (!UnLoadDevice.SetSlowPumpValve(true, out reason))
                {
                    Stop(reason);
                    EV.PostAlarmLog(Module, Module + " " + Name + " failed, " + reason);
                    return false;
                }

                return true;
            }, () => { return UnLoadDevice.ChamberPressure <= switchPressure; }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    UnLoadDevice.SetSlowPumpValve(false, out string _);

                    UnlockPump2(out _);
                    Stop($"{UnLoadDevice.Name} pressure can not pump to {switchPressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void FastPump(int id, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {UnLoadDevice.Name} fast pump valve to {basePressure} mbar");

                if (!UnLoadDevice.SetFastPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return UnLoadDevice.ChamberPressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    UnLoadDevice.SetSlowPumpValve(false, out string _);
                    UnLoadDevice.SetFastPumpValve(false, out string _);

                    UnlockPump2(out _);

                    Stop($"{UnLoadDevice.Name} pressure can not pump to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowPumpValve(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {UnLoadDevice.Name} slow pump valve");

                var succeed = UnLoadDevice.SetSlowPumpValve(false, out var reason);

                // 解锁Pump2
                if (!UnlockPump2(out reason))
                {
                    ResetLocker();
                }

                if (!succeed)
                {
                    Stop(reason);
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

        public void CloseFastPumpValve(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {UnLoadDevice.Name} fast pump valve");

                if (!UnLoadDevice.SetFastPumpValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    UnlockPump2(out _);
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void SlowVent(int id, double switchPressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {UnLoadDevice.Name} slow vent valve to {switchPressure} mbar");

                if (!UnLoadDevice.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return UnLoadDevice.ChamberPressure >= switchPressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    UnLoadDevice.SetSlowVentValve(false, out string _);

                    Stop($"{UnLoadDevice.Name} pressure can not vent to {switchPressure} mbar in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void SlowVent(int id, double switchPressure, double pressureDiff, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {Module} slow vent valve to {switchPressure} mbar");

                if (!UnLoadDevice.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return  UnLoadDevice.ChamberPressure >= switchPressure - pressureDiff;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    UnLoadDevice.SetSlowVentValve(false, out string _);

                    Stop($"{Module} pressure can not vent to {switchPressure} mbar in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseVentValve(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {UnLoadDevice.Name} slow vent valve");

                if (!UnLoadDevice.SetSlowVentValve(false, out string reason))
                {
                    Stop(reason);
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


    }
}
