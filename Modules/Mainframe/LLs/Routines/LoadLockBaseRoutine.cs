using System;
using System.Collections.Generic;
using System.Threading;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Mainframe.Devices;
using Mainframe.TMs;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.Core;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MachineVision.Keyence;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using static Aitex.Core.RT.Device.Unit.E84Passiver;

namespace Mainframe.LLs.Routines
{
    public class LoadLockBaseRoutine : BaseRoutineWithDeviceLocker, IRoutine
    {
        protected LoadLock LoadLockDevice
        {
            get { return _ll; }
        }
        protected TM TMDevice
        {
            get { return _tm; }
        }

        protected IoLift4 Lift
        {
            get { return _llLift; }
        }

        protected IoLoadRotation Rotation
        {
            get { return _llRotation; }
        }

        protected IoClaw WaferClaw
        {
            get { return _llWaferClaw; }
        }
        protected IoClaw TrayClaw
        {
            get { return _llTrayClaw; }
        }

        private LoadLock _ll = null;
        private TM _tm = null; 
        private IoLift4 _llLift = null;
        private IoLoadRotation _llRotation = null;
        private IoClaw _llWaferClaw = null;
        private IoClaw _llTrayClaw = null;
        private KeyenceCVX300F _loadCCD = null;
        public IoSensor _loadTrayHomeSensor;      //Tray��λ
        public IoSensor _loadWaferPlaced;        //���¶��䣬���Wafer����
        public IoSensor _loadTrayPlaced;         //�����������

        public LoadLockBaseRoutine()
        {
             Module = ModuleName.LoadLock.ToString();

            _ll = DEVICE.GetDevice<SicLoadLock>($"{Module}.{Module}");
            _tm = DEVICE.GetDevice<SicTM>($"{ModuleName.System}.{ModuleName.TM}");
            _llLift = DEVICE.GetDevice<IoLift4>($"{Module}.LLLift");
            _llWaferClaw = DEVICE.GetDevice<IoClaw>($"{Module}.LLWaferClaw");
            _llTrayClaw = DEVICE.GetDevice<IoClaw>($"{Module}.LLTrayClaw");
            _llRotation = DEVICE.GetDevice<IoLoadRotation>("Load.Rotation");
            _loadCCD = DEVICE.GetDevice<KeyenceCVX300F>($"TM.KeyenceCVX300F");

            _loadTrayHomeSensor = DEVICE.GetDevice<IoSensor>($"TM.LoadTrayHomeSensor");
            _loadWaferPlaced = DEVICE.GetDevice<IoSensor>($"TM.LLWaferPlaced");
            _loadTrayPlaced = DEVICE.GetDevice<IoSensor>($"TM.LLTrayPresence");
        }

        public virtual Result Start(params object[] objs)
        {
            return Result.DONE;
        }

        public virtual Result Monitor()
        {
            return Result.DONE;
        }

        public override void Abort()
        {
            LoadLockDevice.SetSlowPumpValve(false, out _);
            LoadLockDevice.SetFastPumpValve(false, out _);
            LoadLockDevice.SetSlowVentValve(false, out _);
            
            base.Abort();
        }

        protected void LiftMove(int id, bool up, int timeout)
        {
            string note = up ? "Up" : "Down";
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} Lift to {note}");
                if(up)
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
                if(up)
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

        protected void MoveRelativeHome(int id, int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set {Module} Rotation move relative home");
                if (!_llRotation.MoveRelativeHome(out string reason))
                {
                    Stop($"Set {Module} Rotation move relative home:" + reason);
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

        protected void MoveLoadRotationHomeOffset(int id, float offset, int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set {Module} Rotation move offset");
                if (!_llRotation.JogCW(offset, out string reason))
                {
                    Stop($"Set {Module} Rotation move offset:" + reason);
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

        protected void WaitLoadRotationDone(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                //�����κζ���
                return true;
            },
            () =>
             {
                 //���Rotation���Error
                 if (_llRotation.IsServoError)
                 {
                     Stop($"Set {Module} Rotation Servo Error");
                     return null;
                 }
                 return !_llRotation.IsServoBusy;

             }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Wait Load Rotation Done timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void TryResetServo(int id, int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                var reason = "";
               
                // ��tray mark���ŷ����ܱ������´�����ʱ����ŷ������ȳ����������
                if (_llRotation.IsServoError)
                {
                    Notify($"Resetting the {Module} Rotation...");
                    
                    if (_llRotation.ServoReset(out reason) == false)
                    {
                        Notify($"Unable to perform 'ServoReset' of {Module} Rotation, {reason}");
                        return false;
                    }
                    
                    Thread.Sleep(1000);

                    if (_llRotation.ServoOn(out reason) == false)
                    {
                        Notify($"Unable to perform 'ServoOn' of {Module} Rotation, {reason}");
                        return false;
                    }
                    
                    Thread.Sleep(1000);

                    if (_llRotation.IsServoOn == false)
                    {
                        Notify($"Unable to servo on {Module} Rotation, 'ServoOn' performed but the PLC reports servo-off.");
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

        protected void MoveOneCircle(int id,int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                var reason = "";
               
                Notify($"Start Warpage Measurement");
                if (!_llRotation.MoveOneCircle(out reason))
                {
                    Stop($"Unable to set {Module} warpage measurement sensor:" + reason);
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

        //Result WaitMoveOneCircleDoneResult;
        //protected Result WaitMoveOneCircleDone(int id, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Wait {Module} Rotation One Circle Wafter check sensor");
        //        WaitMoveOneCircleDoneResult = Result.RUN;
        //        return true;
        //    }, () =>
        //    {
        //        //��⵽δ�ź�Sensor�ź�
        //        if (!_loadWaferPlaced.Value)
        //        {
        //            //ʧ�ܺ�Stop���
        //            _llRotation.Stop(out _);
        //            Stop($"Set {Module} Rotation One Circle Wafter check sensor fail [TM DI-35]");
        //            WaitMoveOneCircleDoneResult = Result.VERIFYFAIL;
        //            return true;
        //        }

        //        //���Rotation���Error
        //        if (_llRotation.IsServoError)
        //        {
        //            //ʧ�ܺ�Stop���
        //            _llRotation.Stop(out _);
        //            Stop($"Set {Module} Rotation Servo Error");
        //            WaitMoveOneCircleDoneResult = Result.VERIFYFAIL;
        //            return true;
        //        }

        //        if (!_llRotation.IsServoBusy)
        //        {
        //            Notify($"{Module} Rotation One Circle Wafter check result ok");
        //            WaitMoveOneCircleDoneResult = Result.Succeed;
        //            return true;
        //        }

        //        return false;
        //    }, timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT) //timeout
        //        {
        //            Stop($"Set {Module}Rotation One Circle Wafter check sensor Timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }

        //    return WaitMoveOneCircleDoneResult;
        //}


        Result WaitMoveOneCircleDoneResult;
        public Queue<bool> DISensroQueuen = new Queue<bool>();
        protected Result WaitMoveOneCircleDone(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Capturing warpage measurement sensor");
                WaitMoveOneCircleDoneResult = Result.RUN;
                DISensroQueuen = new Queue<bool>();
                return true;
            }, () =>
            {
                DISensroQueuen.Enqueue(_loadWaferPlaced.Value);
                //???��???Sensor???
                //if (!_loadWaferPlaced.Value)
                //{
                //    //????Stop???
                //    _llRotation.Stop(out _);
                //    Stop($"Set {Module} Rotation One Circle Wafter check sensor fail [TM DI-35]");
                //    WaitMoveOneCircleDoneResult = Result.VERIFYFAIL;
                //    return true;
                //}

                //???Rotation???Error
                if (_llRotation.IsServoError)
                {
                    //????Stop???
                    _llRotation.Stop(out _);
                    Stop($"Unable to capture warpage measurement sensor, Servo Error");
                    return null;
                }

                if (!_llRotation.IsServoBusy)
                {
                    //Notify($"{Module} Rotation One Circle Wafter check finish");
                    WaitMoveOneCircleDoneResult = Result.Succeed;
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
                    Stop($"It's timeout to capture warpage measurement sensor, over {timeout:F1} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }

            return WaitMoveOneCircleDoneResult;
        }

        protected void MoveHomePos(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"{Module} Rotation move Home Pos");
                if (!_llRotation.MoveRelativeHome(out string reason))
                {
                    Stop($"Unable to go to Home position, " + reason);
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

        protected void MoveCCD1Pos(int id,int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"{Module} Rotation move CCD Pos1");
                if (!_llRotation.MoveCCD1Pos(out string reason))
                {
                    Stop($"{Module} Rotation move CCD Pos1 fail:" + reason);
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

        protected void MoveCCD2Pos(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"{Module} Rotation move CCD Pos2");
                if (!_llRotation.MoveCCD2Pos(out string reason))
                {
                    Stop($"{Module} Rotation move CCD Pos2 fail:" + reason);
                    return false;
                }

                return true;
            }, () =>
            {
                if (!_llRotation.IsServoBusy)
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
                    Stop($"Set {Module} Rotation move CCD Pos2 Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void ClawMove(int id, IoClaw _claw,bool claw, int timeout)
        {
            string note = claw ? "Clawing" : "Opening";
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} {note}");
                if (!_claw.SetValue(claw, out string reason))
                {
                    Stop($"Set {Module} {note} failed:" + reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _claw.State == (claw ? ClawStateEnum.Clamp : ClawStateEnum.Open);
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

        public void CheckForelinePressure(int id,double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check {Module} foreline pressure under {basePressure} mbar");

                return true;
            }, () =>
            {
                return LoadLockDevice.ForelinePressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{Module} foreline pressure can not lower than {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void SlowPump(int id, double switchPressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {Module} slow pump valve to {switchPressure} mbar");

                // 锁定Pump2
                if (!LockPump2(out var reason, timeout * 1000))
                {
                    Stop(reason);
                    return false;
                }

                if (!LoadLockDevice.SetSlowPumpValve(true, out reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return LoadLockDevice.ChamberPressure <= switchPressure;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    LoadLockDevice.SetSlowPumpValve(false, out string _);
                    UnlockPump2(out _);
                    Stop($"{Module} pressure can not pump to {switchPressure} in {timeout} seconds");
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
                Notify($"Open {Module} fast pump valve to {basePressure} mbar");

                if (!LoadLockDevice.SetFastPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return LoadLockDevice.ChamberPressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    LoadLockDevice.SetSlowPumpValve(false, out string _);
                    LoadLockDevice.SetFastPumpValve(false, out string _);

                    UnlockPump2(out _);

                    Stop($"{Module} pressure can not pump to {basePressure} in {timeout} seconds");
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
                Notify($"Close {Module} slow pump valve");

                var succeed = LoadLockDevice.SetSlowPumpValve(false, out var reason);

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
                Notify($"Close {Module} fast pump valve");

                if (!LoadLockDevice.SetFastPumpValve(false, out string reason))
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

        public void SlowVent(int id,double switchPressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {Module} slow vent valve to {switchPressure} mbar");

                if (!LoadLockDevice.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return LoadLockDevice.ChamberPressure >= switchPressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    LoadLockDevice.SetSlowVentValve(false, out string _);

                    Stop($"{Module} pressure can not vent to {switchPressure} mbar in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void SlowVent(int id,double switchPressure, double pressureDiff, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {Module} slow vent valve to {switchPressure} mbar");

                if (!_ll.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return LoadLockDevice.ChamberPressure >= switchPressure - pressureDiff;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    _ll.SetSlowVentValve(false, out string _);

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
                Notify($"Close {Module} slow vent valve");

                if (!LoadLockDevice.SetSlowVentValve(false, out string reason))
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

        public void CCDModeSet(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set CCD Mode to 0");
                _loadCCD.RunR0();

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

        Result CCDTriggerResult;
        public Result CCDTrigger(int id,int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Load CCD Check Trigger");

                _loadCCD.ClearResult();
                _loadCCD.GetResult(1);
                CCDTriggerResult = Result.RUN;
                return true;
            }, () =>
            {
                if(_loadCCD._sResult != "")
                {
                    if(_loadCCD._sResult.Contains("OK"))
                    {
                        //Notify($"Load CCD Check Result OK");
                        CCDTriggerResult = Result.Succeed;
                        return true;
                    }
                    else
                    {
                        //Stop($"Load CCD Check Result NG");
                        CCDTriggerResult = Result.VERIFYFAIL;
                        return true;
                    }
                }

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    return Result.VERIFYFAIL;
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Load CCD receive result  in {timeout} seconds");
                    return Result.VERIFYFAIL;
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }

            return CCDTriggerResult;
        }

    }
}
