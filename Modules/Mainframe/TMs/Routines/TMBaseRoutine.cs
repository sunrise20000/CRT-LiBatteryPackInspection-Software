using System;
using System.Collections.Generic;
using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;

namespace Mainframe.TMs.Routines
{
    public class TMBaseRoutine : ModuleRoutine, IRoutine
    {
        public bool IsPicking = false; //机械手是否在Pick和Place过程中,在此过程中Abort不停止机械手动作

        protected RobotBaseDevice RobotDevice
        {
            get { return _robot; }
        }

        protected TM TMDevice
        {
            get { return _tm; }
        }

        protected IoLift4 LoadLift
        {
            get { return _loadLift; }
        }

        protected IoLift4 UnLoadLift
        {
            get { return _unLoadLift; }
        }


        protected bool _isATMMode;
        private RobotBaseDevice _robot = null;
        private RobotBaseDevice _casseteRobot = null;
        private TM _tm = null;
        protected IoSensor SensorLLTrayPresence = null;
        protected IoSensor SensorUnloadWaferPresence = null;
        protected IoSensor SensorBufferHighWaferPresence = null;
        protected IoSensor SensorBufferMiddleWaferPresence = null;
        protected IoSensor SensorBufferLowWaferPresence = null;
        private IoInterLock _tmIoInterLock;
        //传盘后PM腔检查Tray是否放好Sensor
        public SicPM.Devices.IoSensor _reactorSuspectorCheck;

        private IoLift4 _loadLift;
        private IoLift4 _unLoadLift;

        public TMBaseRoutine()
        {
            Module = ModuleName.TM.ToString();

            _robot = DEVICE.GetDevice<SicTMRobot>($"{ModuleName.TMRobot}.{ModuleName.TMRobot}");

            _tm = DEVICE.GetDevice<SicTM>($"{ModuleName.System}.{ModuleName.TM}");
            _isATMMode = SC.GetValue<bool>("System.IsATMMode");
            SensorLLTrayPresence = DEVICE.GetDevice<IoSensor>($"{ModuleName.TM}.LLTrayPresence");
            SensorUnloadWaferPresence = DEVICE.GetDevice<IoSensor>($"{ModuleName.TM}.UnLoadWaferPlaced");
            SensorBufferHighWaferPresence = DEVICE.GetDevice<IoSensor>($"{ModuleName.TM}.BufferWaferHigh");
            SensorBufferMiddleWaferPresence = DEVICE.GetDevice<IoSensor>($"{ModuleName.TM}.BufferWaferMiddle");
            SensorBufferLowWaferPresence = DEVICE.GetDevice<IoSensor>($"{ModuleName.TM}.BufferWaferLow");

            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
            _loadLift = DEVICE.GetDevice<IoLift4>($"{ModuleName.LoadLock}.LLLift");
            _unLoadLift = DEVICE.GetDevice<IoLift4>($"{ModuleName.UnLoad}.UnLoadLift");


            _reactorSuspectorCheck = DEVICE.GetDevice<SicPM.Devices.IoSensor>($"PM1.SensorReactorSuspectorCheck");
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
            _tm.SetAllValvesClose(out string reason);
        }

        /// <summary>
        /// this method should be call just after check load
        /// </summary>
        /// <param name="id"></param>
        protected void UpdateWaferInfoByRobotSensor(int id, RobotBaseDevice robot)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update wafer info by robot wafer present");

                if (robot.IsWaferPresenceOnBlade1 && WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(robot.Module), 0))
                {
                    EV.PostInfoLog(Module, "TM Robot sensor found wafer on blade 1");
                    WaferManager.Instance.CreateWafer(ModuleHelper.Converter(robot.Module), 0, WaferStatus.Normal);
                }

                if (!robot.IsWaferPresenceOnBlade1 && WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Module), 0))
                {
                    EV.PostInfoLog(Module, "TM Robot sensor no wafer on blade 1");
                    WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(robot.Module), 0);
                }

                if (robot.IsWaferPresenceOnBlade2 && WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(robot.Module), 1))
                {
                    EV.PostInfoLog(Module, "TM Robot sensor found wafer on blade 2");
                    WaferManager.Instance.CreateWafer(ModuleHelper.Converter(robot.Module), 1, WaferStatus.Normal);
                }

                if (!robot.IsWaferPresenceOnBlade2 && WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Module), 1))
                {
                    EV.PostInfoLog(Module, "TM Robot sensor no wafer on blade 2");
                    WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(robot.Module), 1);
                }

                return true;
            });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }


        protected void UpdateWaferInfoDueHandoff(int id, ModuleName moduleFrom, int slotFrom, ModuleName moduleTo, int slotTo)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update wafer info because of handoff");

                WaferManager.Instance.WaferMoved(moduleFrom, slotFrom, moduleTo, slotTo);

                return true;
            });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }

        protected void ClearRobortExtendToDO(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify("Clear RobortExtendTO DO");
                _tmIoInterLock.DoVacRobotExtenLLEnable = false;
                _tmIoInterLock.DoVacRobotExtendBufferEnable = false;
                _tmIoInterLock.DoVacRobotExtendPMAEnable = false;
                _tmIoInterLock.DoVacRobotExtendPMBEnable = false;
                _tmIoInterLock.DoVacRobotExtendUnloadEnable = false;
                return true;
            });

        }

        protected void WaitPMReadySensor(int id, ModuleName md, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                return true;
            }, () =>
            {
                if (ModuleHelper.IsPm(md))
                {
                    return _tmIoInterLock.DoRectorAATMTransferReady || _tmIoInterLock.DoRectorAProcessTransferReady;
                }
                else
                {
                    return true;
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
                    Stop($"Wait ReactorATMTransferReady or RectorProcessTransferReady  timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        /// <summary>
        /// 发送DO信号
        /// </summary>
        /// <param name="id"></param>
        protected void SetRobortExtendToDO(int id, ModuleName target, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("Set RobortExtendTO DO");

                _tmIoInterLock.DoVacRobotExtendBufferEnable = false;
                _tmIoInterLock.DoVacRobotExtendPMAEnable = false;
                _tmIoInterLock.DoVacRobotExtendPMBEnable = false;
                _tmIoInterLock.DoVacRobotExtendUnloadEnable = false;
                _tmIoInterLock.DoVacRobotExtenLLEnable = false;


                if (ModuleHelper.IsLoadLock(target) || target == ModuleName.Load)
                {
                    if (!_tmIoInterLock.SetRobotExtenLLEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }
                }
                else if (target == ModuleName.UnLoad)
                {
                    if (!_tmIoInterLock.SetRobotExtendUnLoadEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }
                }
                else if (ModuleHelper.IsBuffer(target))
                {
                    if (!_tmIoInterLock.SetRobotExtendBufferEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }
                }
                else if (target == ModuleName.PMA || target == ModuleName.PM1)
                {
                    if (!_tmIoInterLock.SetRobotExtendPMAEnable(true, out string reasen))
                    {
                        Notify(reasen);
                        return false;
                    }

                }
                else if (target == ModuleName.PMB || target == ModuleName.PM2)
                {
                    if (!_tmIoInterLock.SetRobotExtendPMBEnable(true, out string reasen))
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
                    return _tmIoInterLock.DiVacRobotExtenLLEnableFB;
                }
                else if (target == ModuleName.UnLoad)
                {
                    return _tmIoInterLock.DiVacRobotExtendUnloadEnableFB;
                }
                else if (ModuleHelper.IsBuffer(target))
                {
                    return _tmIoInterLock.DiVacRobotExtendBufferEnableFB;
                }
                else if (target == ModuleName.PMA || target == ModuleName.PM1)
                {
                    return _tmIoInterLock.DiVacRobotExtendPMAEnableFB;

                }
                else if (target == ModuleName.PMB || target == ModuleName.PM2)
                {
                    return _tmIoInterLock.DiVacRobotExtendPMBEnableFB;

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

        public void RobotRequestWaferPresent(int id, RobotBaseDevice robot, RobotArmEnum arm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("request wafer present");

                List<object> parm = new List<object>() { "QueryWaferPresent", arm };
                if (!robot.ReadParameter(parm.ToArray()))
                {
                    EV.PostAlarmLog(Module, $"Can not request wafer present");
                    return false;
                }
                return true;

            }, () =>
            {
                if (robot.IsReady())
                {
                    return true;
                }
                return false;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void RobotRequestWaferAWCData(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("request wafer AWC Data");

                List<object> parm = new List<object>() { "QueryWaferCentData" };
                if (!robot.ReadParameter(parm.ToArray()))
                {
                    EV.PostAlarmLog(Module, $"Can not request wafer AWC Data");
                    return false;
                }

                return true;
            }, () =>
            {
                if (robot.IsReady())
                {
                    return true;
                }

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop("robot error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void CheckWaferInfoByRobotRQ(int id, RobotBaseDevice robot, RobotArmEnum hand, int timeOut)
        {
            string strMessage = "";
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                    return true;

                if (hand == RobotArmEnum.Blade1 || hand == RobotArmEnum.Both)
                {
                    if (!robot.IsWaferPresenceOnBlade1 && WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Module), 0))
                    {
                        strMessage = "Robort sensor no wafer and UI have tray!";
                        return false;
                    }

                    if (robot.IsWaferPresenceOnBlade1 && !WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Module), 0))
                    {
                        strMessage = "Robort sensor have wafer and UI no tray!";
                        return false;
                    }
                }

                if (hand == RobotArmEnum.Blade2 || hand == RobotArmEnum.Both)
                {
                    if (robot.IsWaferPresenceOnBlade2 && !WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Module), 1))
                    {
                        EV.PostWarningLog(Module, "TM Robot sensor found wafer on blade 2");
                        return false;
                    }

                    if (!robot.IsWaferPresenceOnBlade2 && !WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(robot.Module), 1))
                    {
                        EV.PostWarningLog(Module, "TM Robot sensor no wafer on blade 2");
                        return false;
                    }
                }

                return true;
            }, timeOut);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"check wafer info failed." + strMessage);
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckReactorSuspector(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                if (SC.GetValue<bool>("PM.PM1.ReactorSuspectorCheckEnable"))
                {
                    if (_reactorSuspectorCheck.Value)
                    {
                        EV.PostAlarmLog(Module, $"check reactor suspector sensor present failed");
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


        protected void CheckWaferInfoByRobotSensor(int id, RobotBaseDevice robot, RobotArmEnum hand, int timeOut)
        {
            Notify($"check wafer info by robot sensor present");
            string strMessage = "";
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                    return true;

                if (hand == RobotArmEnum.Blade1 || hand == RobotArmEnum.Both)
                {
                    if (!robot.IsWaferPresenceOnBlade1 && WaferManager.Instance.CheckHasWafer(robot.Module, 0))
                    {
                        strMessage = "Robort sensor no wafer and UI have wafer!!";
                        return false;
                    }

                    if (robot.IsWaferPresenceOnBlade1 && WaferManager.Instance.CheckNoWafer(robot.Module, 0))
                    {
                        strMessage = "Robort sensor have wafer and UI no wafer!!";
                        return false;
                    }
                }

                if (hand == RobotArmEnum.Blade2 || hand == RobotArmEnum.Both)
                {
                    if (robot.IsWaferPresenceOnBlade2 && WaferManager.Instance.CheckNoWafer(robot.Module, 1))
                    {
                        EV.PostWarningLog(Module, "TM Robot sensor found wafer on blade 2");
                        return false;
                    }

                    if (!robot.IsWaferPresenceOnBlade2 && WaferManager.Instance.CheckHasWafer(robot.Module, 1))
                    {
                        EV.PostWarningLog(Module, "TM Robot sensor no wafer on blade 2");
                        return false;
                    }
                }

                return true;
            }, timeOut);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"check wafer info failed.." + strMessage);
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        protected void WaitSlitValveOpenInterlock(int id, IoSlitValve slitValve, int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return slitValve != null;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"wait {slitValve.Name} open timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void WaitSlitValveCloseInterlock(int id, IoSlitValve slitValve, int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return slitValve == null;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"wait {slitValve.Name} close timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void QueryOffset(int id, AlignerBase aligner, int timeout)
        {
            //Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            //{
            //    Notify($"Query {Module} offset");
            //    string reason;
            //    if (!aligner.QueryOffset(out reason))
            //    {
            //        Stop(reason);
            //        return false;
            //    }
            //    return true;
            //}, () =>
            //{
            //    if (aligner.IsIdle)
            //    {
            //        EV.PostInfoLog(aligner.Module, $"{aligner.Module} offset RO={aligner.RadialOffset} TO={aligner.ThetaOffset}");
            //        return true;
            //    }
            //    return false;
            //}, timeout * 1000);

            //if (ret.Item1)
            //{
            //    if (ret.Item2 == Result.FAIL)
            //    {
            //        Stop(string.Format("failed."));
            //        throw (new RoutineFaildException());
            //    }
            //    else if (ret.Item2 == Result.TIMEOUT) //timeout
            //    {
            //        Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
            //        throw (new RoutineFaildException());
            //    }
            //    else
            //        throw (new RoutineBreakException());
            //}
        }

        protected void PickEx(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, float ro, float to, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"PickEX from {source} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")} RO={ro} TO={to}");
                if (!robot.PickEx(hand, source.ToString(), slot, ro, to))
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
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("pick timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void Pick(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                IsPicking = true;
                Notify($"Pick from {source} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
                if (!robot.Pick(hand, source.ToString(), slot))
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
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("pick timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void PickExtend(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Pick extend to {source} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
                List<object> para = new List<object>() { "ExtendForPick", hand, source, slot, false, true };
                if (!robot.GoTo(para.ToArray()))
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
                    Stop($"Pick extend failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void PickRetract(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("send command to device");
                List<object> para = new List<object>() { "RetractFromPick", hand, source, slot, true, true };
                if (!robot.GoTo(para.ToArray()))
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
                    Stop($"Pick retract failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CheckBeforePick(int id, ModuleName source, int slot, RobotArmEnum blade)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify("Check pick condition");
                string reason = string.Empty;

                if (!RobotDevice.IsReady())
                {
                    Stop("robot is not Ready.");
                    return false;
                }

                if (blade == RobotArmEnum.Blade1)
                {
                    if (!WaferManager.Instance.CheckHasWafer(source, slot) && !WaferManager.Instance.CheckHasTray(source, slot))
                    {
                        Stop("Source no wafer");
                        return false;
                    }

                    if (!WaferManager.Instance.CheckNoWafer(Module, 0))
                    {
                        Stop("Blade has wafer");
                        return false;
                    }
                }
                else if (blade == RobotArmEnum.Blade2)
                {
                    if (!WaferManager.Instance.CheckHasWafer(source, slot))
                    {
                        Stop("Source no wafer");
                        return false;
                    }

                    if (!WaferManager.Instance.CheckNoWafer(Module, 1))
                    {
                        Stop("Blade has wafer");
                        return false;
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (!WaferManager.Instance.CheckHasWafer(source, slot + i))
                        {
                            Stop("Source no wafer");
                            return false;
                        }

                        if (!WaferManager.Instance.CheckNoWafer(Module, i))
                        {
                            Stop("Blade has wafer");
                            return false;
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
            }
        }

        public void CheckBeforeExtend(int id, ModuleName target, int slot, RobotArmEnum blade)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify("Check extend condition");
                string reason = string.Empty;

                if (RobotDevice.IsReady())
                {
                    Stop("robot is not Ready.");
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
            }
        }


        public void Extend(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("send extend command to device");
                List<object> para = new List<object>() { hand, source, slot, false, true };
                if (!robot.GoTo(para.ToArray()))
                {
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
                    Stop($"Extend failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void Retract(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("send retract command to device");
                List<object> para = new List<object>() { hand, source, slot, true, true };
                if (!robot.GoTo(para.ToArray()))
                {
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
                    Stop($"Retract failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        //protected void SetShutter(int id, CoralPM pm, bool isOpen, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Set shutter {(isOpen ? "open" : "close")}");

        //        if (!pm.SetShutter(isOpen, out string reason))
        //        {
        //            Stop($"{pm.Name} {reason}");
        //            return false;
        //        }

        //        return true;
        //    }, () =>
        //    {
        //        if (isOpen && pm.CheckShutterDown())
        //        {
        //            return true;
        //        }

        //        if (!isOpen && pm.CheckShutterUp())
        //        {
        //            return true;
        //        }

        //        return false;

        //    }, timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            Stop($"{pm.Name} error");
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT) //timeout
        //        {
        //            Stop($"{pm.Name} shutter operate timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        protected void PrepareTransfer(int id, PM pm, EnumTransferType type, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                string reason = "";
                Notify($"Run {pm.Name} prepare transfer ");

                pm.PrepareTransfer(type, out reason);

                return true;
            }, () =>

            {
                if (pm.IsError)
                {
                    return null;
                }

                return pm.IsIdle && pm.CheckEnableTransfer(type, out string reason);

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"{pm.Name} error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} prepare transfer timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void PrepareTransferNotWait(int id, PM pm, EnumTransferType type, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                string reason = "";
                Notify($"Run {pm.Name} prepare transfer ");

                pm.PrepareTransfer(type, out reason);

                return true;
            }, () =>

            {
                return true;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"{pm.Name} error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} prepare transfer timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckTransferPrepared(int id, PM pm, EnumTransferType type, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check {pm.Name} transfer prepared");

                return true;
            }, () =>

            {
                if (pm.IsError)
                {
                    return null;
                }

                return pm.IsIdle && pm.CheckEnableTransfer(type, out string reason);

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} prepare transfer timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void TransferHandoff(int id, PM pm, EnumTransferType type, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("send handoff command to device");

                pm.TransferHandoff(type);

                return true;
            }, () =>
            {
                if (pm.IsError)
                {
                    return null;
                }

                return pm.IsIdle && pm.CheckHandoff(type);

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"{pm.Name} error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} transfer handoff timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        //public void PostTransfer(int id, PMEntity pmEntity, PM pm, EnumTransferType type, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run {pm.Name} post transfer ");

        //        pmEntity.InvokePostTransfer(type);

        //        return true;
        //    }, () =>
        //    {
        //        if (pm.IsError)
        //        {
        //            return null;
        //        }

        //        return pm.IsIdle && pm.CheckPostTransfer(type);

        //    }, timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            Stop($"{pm.Name} error");
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT) //timeout
        //        {
        //            Stop($"{pm.Name} post transfer timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        public void CheckBeforePlace(int id, ModuleName target, int slot, RobotArmEnum blade)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify("Check place condition");
                string reason = string.Empty;

                if (!RobotDevice.IsReady())
                {
                    Stop("robot is not Ready.");
                    return false;
                }

                if (blade == RobotArmEnum.Blade1)
                {

                    if (target != ModuleName.LoadLock && target != ModuleName.Load)
                    {
                        if (WaferManager.Instance.CheckHasWafer(target, slot))
                        {
                            Stop($"Target {target}.{slot + 1} has wafer");
                            return false;
                        }
                    }

                    if (WaferManager.Instance.CheckHasTray(target, slot))
                    {
                        Stop($"Target {target}.{slot + 1} has tray");
                        return false;
                    }

                    if (!WaferManager.Instance.CheckHasWafer(Module, 0) && !WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(Module), 0))
                    {
                        Stop("Blade no wafer");
                        return false;
                    }
                }
                else if (blade == RobotArmEnum.Blade2)
                {
                    if (!WaferManager.Instance.CheckNoWafer(target, slot))
                    {
                        Stop($"Target {target}.{slot + 1} has wafer");
                        return false;
                    }

                    if (!WaferManager.Instance.CheckHasWafer(Module, 1))
                    {
                        Stop("Blade no wafer");
                        return false;
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (!WaferManager.Instance.CheckNoWafer(target, slot + i))
                        {
                            Stop("Target has wafer");
                            return false;
                        }

                        if (!WaferManager.Instance.CheckHasWafer(Module, i))
                        {
                            Stop("Blade no wafer");
                            return false;
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
            }
        }


        public void Place(int id, RobotBaseDevice robot, ModuleName target, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                IsPicking = true;
                Notify($"Place to {target} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
                if (!robot.Place(hand, target.ToString(), slot))
                {
                    robot.IsBusy = false;
                    return false;
                }
                return true;
            }, () =>
            {
                if (robot.IsReady())
                    return true;
                if (robot.IsError)
                    return null;
                return false;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Place failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("place timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                //else
                //    throw (new RoutineBreakException());
            }
        }


        public void PlaceExtend(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Place extend to {source} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
                List<object> para = new List<object>() { "ExtendForPlace", hand, source, slot, false, true };
                if (!robot.GoTo(para.ToArray()))
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
                    Stop($"Place extend failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void PlaceRetract(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("place retract");
                List<object> para = new List<object>() { "RetractFromPlace", hand, source, slot, true, true };
                if (!robot.GoTo(para.ToArray()))
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
                    Stop($"Place retract failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void RobotGotoNotWait(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Goto {source} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
                List<object> para = new List<object>() { "GoTo", hand, source, slot, true, true };
                if (!robot.GoTo(para.ToArray()))
                {
                    robot.IsBusy = false;
                    return false;
                }
                return true;
            }, () =>
            {
                return true;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Goto failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
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
                Notify($"Check robot ready");
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
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void CloseTMVent(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {TMDevice.Name} vent valve");

                if (!TMDevice.SetFastVentValve(false, out string reason))
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

        public void OpenBufferVent(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Open Buffer vent valve");

                if (!TMDevice.SetBufferVentValve(true, out string reason))
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

        public void CloseBufferVent(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close Buffer vent valve");

                if (!TMDevice.SetBufferVentValve(false, out string reason))
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

        public void OpenSlowPump(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} slow pump valve");

                if (!tm.SetSlowPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.ChamberPressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastPumpValve(false, out string _);

                    Stop($"{tm.Name} pressure can not pump to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowPump(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} pump valves");

                if (!_tm.SetSlowPumpValve(false, out string reason))
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


        public void OpenFastPump(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} pump valve");

                if (!tm.SetFastPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.ChamberPressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastPumpValve(false, out string _);

                    Stop($"{tm.Name} pressure can not pump to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void CloseFastPump(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} pump valves");

                if (!_tm.SetFastPumpValve(false, out string reason))
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

        public void CloseFastPump(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close  {TMDevice.Name} Fast pump valve");

                if (!TMDevice.SetFastPumpValve(false, out string reason))
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

        public void CloseSlowPump(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close  {TMDevice.Name} Slow pump valve");

                if (!TMDevice.SetSlowPumpValve(false, out string reason))
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

        public void SetMFCToSetPoint(int id, SicPM.Devices.IoMFC _mfc, double setPoint)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set  MFC value to default");
                _mfc.Ramp(setPoint, 0);
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

        public void OpenFastVent(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} slow vent valve");

                if (!tm.SetFastVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.ChamberPressure >= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastVentValve(false, out string _);

                    Stop($"{tm.Name} pressure can not vent to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void OpenSlowVent(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} slow vent valve");

                if (!tm.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.ChamberPressure >= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastVentValve(false, out string _);

                    Stop($"{tm.Name} pressure can not vent to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowVentValve(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} fast pump valve");

                if (!tm.SetSlowVentValve(false, out string reason))
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
