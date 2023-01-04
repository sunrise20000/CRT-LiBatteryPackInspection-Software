using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using Mainframe.LLs;
using Mainframe.UnLoads;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.EFEM;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad;
using static MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase.RobotBaseDevice;

namespace Mainframe.EFEMs.Routines
{
    public class EfemBaseRoutine : ModuleRoutine, IRoutine
    {
        private RobotBaseDevice _waferRobot = null;
        private RobotBaseDevice _trayRobot = null;


        private LoadLock _ll;
        private UnLoad _ul;

        private IEFEM _efemDevice;

        public IoSensor _loadTrayHomeSensor;      //Tray定位
        public IoSensor _loadWaferPlaced;        //上下对射，检测Wafer有无
        public IoSensor _loadTrayPresence;         //检测托盘有无
        public IoSensor _cassAL6Inch;           //6寸检测
        public IoSensor _cassAR6Inch;           //6寸检测
        public IoSensor _cassBL6Inch;           //6寸检测

        public IoSensor _cassALWaferConvex;     //凸片检测
        public IoSensor _cassARWaferConvex;     //凸片检测
        public IoSensor _cassBLWaferConvex;     //凸片检测
        private IoInterLock _tmIoInterLock;

        public IoLift4 _llLift;
        public IoClaw _loadWaferClaw;
        public IoLift4 _unLoadLift;
        public IoClaw _unLoadWaferClaw;

        protected double _ventBasePressure;
        protected int _slowVentTimeout;

        private HwAlignerGuide _alignerDevice = null;

        protected RobotBaseDevice WaferRobot
        {
            get { return _waferRobot; }
        }

        protected RobotBaseDevice TrayRobot
        {
            get { return _trayRobot; }
        }

        protected IEFEM EfemDevice
        {
            get { return _efemDevice; }
        }

        public EfemBaseRoutine()
        {
            Module = ModuleName.EFEM.ToString();



            _ll = DEVICE.GetDevice<SicLoadLock>($"{ModuleName.LoadLock}.{ModuleName.LoadLock}");
            _ul = DEVICE.GetDevice<SicUnLoad>($"{ModuleName.UnLoad}.{ModuleName.UnLoad}");

            _waferRobot = DEVICE.GetDevice<SicWaferRobot>($"WaferRobot.WaferRobot");
            _trayRobot = DEVICE.GetDevice<SicTrayRobot>($"TrayRobot.TrayRobot");
            _efemDevice = DEVICE.GetDevice<SicEFEM>($"EFEM.EFEM");
            _loadTrayHomeSensor = DEVICE.GetDevice<IoSensor>($"TM.LoadTrayHomeSensor");
            _alignerDevice = DEVICE.GetDevice<HwAlignerGuide>($"TM.HiWinAligner");
            _loadWaferPlaced = DEVICE.GetDevice<IoSensor>($"TM.LLWaferPlaced");
            _loadTrayPresence = DEVICE.GetDevice<IoSensor>($"TM.LLTrayPresence");
            _cassBLWaferConvex = DEVICE.GetDevice<IoSensor>($"TM.CassBLWaferConvexSensor");
            _cassALWaferConvex = DEVICE.GetDevice<IoSensor>($"TM.CassALWaferConvexSensor");
            _cassARWaferConvex = DEVICE.GetDevice<IoSensor>($"TM.CassARWaferConvexSensor");
            _cassAL6Inch = DEVICE.GetDevice<IoSensor>($"TM.CassALInch6Sensor");
            _cassAR6Inch = DEVICE.GetDevice<IoSensor>($"TM.CassARInch6Sensor");
            _cassBL6Inch = DEVICE.GetDevice<IoSensor>($"TM.CassBLInch6Sensor");
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");

            _llLift= DEVICE.GetDevice<IoLift4>($"LoadLock.LLLift");
            _unLoadLift= DEVICE.GetDevice<IoLift4>($"UnLoad.UnLoadLift");

            _loadWaferClaw = DEVICE.GetDevice<IoClaw>($"LoadLock.LLWaferClaw");
            _unLoadWaferClaw = DEVICE.GetDevice<IoClaw>($"UnLoad.UnLoadWaferClaw");
        }

        public virtual void Init(ModuleName target)
        {

        }

        public virtual void Init(ModuleName target, int slot)
        {

        }

        public virtual Result Start(params object[] objs)
        {
            return Result.DONE;
        }

        public virtual Result Monitor()
        {
            return Result.DONE;
        }

        public virtual void Abort()
        {

        }

        protected void CheckTraySensor(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                if (SC.GetValue<bool>($"System.IsSimulatorMode"))
                {
                    return true;
                }
                return _loadTrayPresence.Value;
            });
            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Sensor[DI-32] check no tray");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void ClearRobortExtendToDO(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify("Clear RobortExtendTO DO");
                _tmIoInterLock.DoATMRobotExtendLoaLSideEnable = false;
                _tmIoInterLock.DoATMRobotExtendLoaRSideEnable = false;
                _tmIoInterLock.DoATMRobotExtendUnloadEnable = false;
                return true;
            });

        }

        public void SlowVent(int id, ModuleName source, double switchPressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {source} slow vent valve to {switchPressure} mbar");

                var result = false;
                var reason = "";
                if (source == ModuleName.LoadLock)
                    result = _ll.SetSlowVentValve(true, out reason);
                else if (source == ModuleName.UnLoad)
                    result = _ul.SetSlowVentValve(true, out reason);
                else
                    reason = $"The source to vent must be LoadLock or UnLoad";
                
                if (result == false)
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                switch (source)
                {
                    case ModuleName.LoadLock:
                        return _ll.ChamberPressure >= switchPressure;

                    case ModuleName.UnLoad:
                        return _ul.ChamberPressure >= switchPressure;

                    default:
                        return false;
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
                    if (source == ModuleName.LoadLock)
                        _ll.SetSlowVentValve(false, out _);
                    else if (source == ModuleName.UnLoad)
                        _ul.SetSlowVentValve(false, out _);

                    Stop($"{source} pressure can not vent to {switchPressure} mbar in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseVentValve(int id, ModuleName source)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {source} slow vent valve");
                var result = false;
                var reason = "";
                if (source == ModuleName.LoadLock)
                    result = _ll.SetSlowVentValve(false, out reason);
                else if (source == ModuleName.UnLoad)
                    result = _ul.SetSlowVentValve(false, out reason);
                else
                    reason = $"The source to vent must be LoadLock or UnLoad";

                if (result == false)
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


        protected void Pick(int id, RobotBaseDevice robot, ModuleName source, int slot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Pick from {source} {slot + 1} use  Blade1 ");
                if (!robot.Pick(MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotArmEnum.Blade1, source.ToString(), slot))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Pick failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop(string.Format("pick timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void ExtendForPick(int id, RobotBaseDevice robot, ModuleName source, int slot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"ExtendForPick from {source} {slot + 1} use  Blade1 ");
                if (!robot.CheckToPostMessage(RobotMsg.ExtendForPick, RobotArmEnum.Blade1, source.ToString(), slot))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"ExtendForPick failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("ExtendForPick timeout, can not complete in {0} seconds", timeout));
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void ExtendForPlace(int id, RobotBaseDevice robot, ModuleName source, int slot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"ExtendForPlace to {source} {slot + 1} use  Blade1 ");
                if (!robot.CheckToPostMessage(RobotMsg.ExtendForPlace, MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotArmEnum.Blade1, source.ToString(), slot))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"ExtendForPlace failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("ExtendForPlace timeout, can not complete in {0} seconds", timeout));
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void RobotActionComplete(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Robot Action Complete ");
                if (!robot.CheckToPostMessage(RobotMsg.ActionDone))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Robot Action failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("Robot Action timeout, can not complete in {0} seconds", timeout));
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void Place(int id, RobotBaseDevice robot, ModuleName source, int slot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Place wafer to {source} {slot + 1} use  Blade1 ");
                if (!robot.Place(MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotArmEnum.Blade1, source.ToString(), slot))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Place failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop(string.Format("Place timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckWaferStatuBeforePick(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check Wafer Status Before Pick");
                if (!robot.ExecuteCommand(new object[] { true }))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                {
                    if (WaferManager.Instance.CheckHasWafer(robot.Name, 0))
                    {
                        EV.PostWarningLog(ModuleName.EFEM.ToString(), "Robot Check Has Wafer!");
                        return null;
                    }
                    return true;
                }
                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Place failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop(string.Format("Place timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckWaferStatuAfterPlace(int id, RobotBaseDevice robot,int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check Wafer Status After Place");
                if (!robot.ExecuteCommand(new object[] { true}))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                {
                    if (WaferManager.Instance.CheckHasWafer(robot.Name, 0))
                    {
                        EV.PostWarningLog(ModuleName.EFEM.ToString(), "Place Finished and Robot Check Has Wafer!");
                        return null;
                    }
                    return true;
                    
                }
                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Place failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop(string.Format("Place timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void CheckTrayStatuBeforePick(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check Tray Status Before Pick");
                if (!robot.ExecuteCommand(new object[] { true }))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                {
                    if (WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Name), 0))
                    {
                        EV.PostWarningLog(ModuleName.EFEM.ToString(), "Robot Check Has Tray!");
                        return null;
                    }
                    return true;

                }
                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Place failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop(string.Format("Place timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void Map(int id, RobotBaseDevice robot, ModuleName source, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Mapping {source}");
                if (!robot.WaferMapping(source))
                {
                    robot.IsBusy = false;
                    return false;
                }

                return true;

            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Mapping failed, error {robot.ErrorCode}");
                    robot.Abort();
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop(string.Format("Mapping timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void CheckRobotReady(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"{robot.Name} Check robot ready");
             
                return true;
            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    robot.Abort();
                    Stop($"{robot.Name} timeout, can not complete in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void RobotHomeExcute(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"{robot.Name} Execute home");
                if (!robot.Home(null))
                {
                    EV.PostAlarmLog(Module, $"{robot.Name} Can not home robot");
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
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    robot.Abort();
                    Stop($"{robot.Name} home timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void CloseVacuum(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Close Vacuum");
                _alignerDevice.CloseVaccum();
                return true;
            }, () =>
            {
                //return !_alignerDevice.IsBusy;

                return true;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Close Vacuum Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void SetWaferRobortExtendToDO(int id, ModuleName target, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("Set RobortExtendTO DO");

                _tmIoInterLock.DoATMRobotExtendLoaLSideEnable = false;
                _tmIoInterLock.DoATMRobotExtendLoaRSideEnable = false;
                _tmIoInterLock.DoATMRobotExtendUnloadEnable = false;

                if (ModuleHelper.IsLoadLock(target) || target == ModuleName.Load)
                {
                    if (!_tmIoInterLock.SetWaferRobotExtendLoadEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }
                }
                else if (target == ModuleName.UnLoad)
                {
                    if (!_tmIoInterLock.SetWaferRobotExtendUnLoadEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }
                }
                return true;

            }, () =>
            {
                if (ModuleHelper.IsLoadLock(target) || target == ModuleName.Load)
                {
                    return _tmIoInterLock.DiATMRobotExtendLoadLSideEnableFB;
                }
                else if (target == ModuleName.UnLoad)
                {
                    return _tmIoInterLock.DiATMRobotExtendUnloadEnableFB;
                }
                return true;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set Robot extend to Do timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetTrayRobortExtendToDO(int id, ModuleName target, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("Set RobortExtendTO DO");

                _tmIoInterLock.DoATMRobotExtendLoaLSideEnable = false;
                _tmIoInterLock.DoATMRobotExtendLoaRSideEnable = false;
                _tmIoInterLock.DoATMRobotExtendUnloadEnable = false;

                if (ModuleHelper.IsLoadLock(target) || target == ModuleName.Load)
                {
                    if (!_tmIoInterLock.SetTrayRobotExtendLoadEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }
                }
                return true;

            }, () =>
            {
                if (ModuleHelper.IsLoadLock(target) || target == ModuleName.Load)
                {
                    return _tmIoInterLock.DiATMRobotExtendLoadRSideEnableFB;
                }
                return true;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set Robot extend to Do timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckLoadTrayPresence(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                if (SC.GetValue<bool>("LoadLock.TrayPresenceCheckEnable"))
                {
                    if (!_loadTrayPresence.Value)
                    {
                        EV.PostAlarmLog(Module, $"check load Tray Presence sensor false,no Tray in load");
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


    }
}
