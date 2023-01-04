using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Servo.NAIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SicPM.PMModule;

namespace SicPM.Routines
{
    class PMPrepareTransferRoutine : PMBaseRoutine
    {
        enum RoutineStep
        {
            SetDownLimit,

            SetBlockOn,

            Delay,

            WaitStatusCorrect,

            SetBlockOff,
            RotationEnable,
            HeatEnable,
            SetPSUDisable,
            SetSCRDisable,
            WaitTempBelow900,
        }



        private int _timeout;

        private int _rotationCloseTimeout = 100;   //旋转停止超时
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间
        private bool _preTransferPSUEnable = false;
        private bool _preTransferSCREnable = false;
        SicPM.Devices.IoInterLock _pmIoInterLock;

    private NAISServo servo;

        public PMPrepareTransferRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "Servo Down";
            servo = DEVICE.GetDevice<NAISServo>($"{Module}.NAISServo");
            _pmIoInterLock = DEVICE.GetDevice<SicPM.Devices.IoInterLock>($"{Module}.PMInterLock");

            _timeout = SC.GetValue<int>($"NAISServo.DownTimeout");
        }


        public override Result Start(params object[] objs)
        {
            _preTransferPSUEnable = SC.GetValue<bool>($"PM.{Module}.PreTransferPSUEnable");
            _preTransferSCREnable = SC.GetValue<bool>($"PM.{Module}.PreTransferSCREnable");

            if (PMDevice.CheckServoAlarm())
            {
                EV.PostWarningLog(Module, "can not up,confinementring is error.");
                return Result.FAIL;
            }
            if (PMDevice.CheckServoIsBusy())
            {
                EV.PostWarningLog(Module, "can not vent,confinementring is busy.");
                return Result.FAIL;
            }
            Reset();
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                if (!_preTransferPSUEnable)
                {
                    SetPSUEnable((int)RoutineStep.SetPSUDisable, false, _heatTimeOut);
                }
                if (!_preTransferSCREnable)
                {
                    SetSCREnable((int)RoutineStep.SetSCRDisable, false, _heatTimeOut);
                }
                SetRotationValve((int)RoutineStep.RotationEnable, 0, false, _rotationCloseTimeout);

                //if (!_preTransferPSUEnable )
                //{
                //    WaitTempratureBelow900((int)RoutineStep.WaitTempBelow900,600);
                //}

                if (SC.GetValue<bool>($"System.IsSimulatorMode"))
                {
                    PMDevice.ServoState = ServoStates.Down;
                    return Result.DONE;
                }

                //ServoDownLimit((int)RoutineStep.SetDownLimit, PMDevice, _timeout);

                //ServoBlockOn((int)RoutineStep.SetBlockOn, PMDevice, _timeout);

                //TimeDelay((int)RoutineStep.Delay, 2);

                //WaitStatusCorrect((int)RoutineStep.WaitStatusCorrect, PMDevice, _timeout);

                //ServoBlockOff((int)RoutineStep.SetBlockOff, PMDevice, _timeout);
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

        private void ServoDownLimit(int id, PMModuleBase pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} set Block No Low.");

                servo.SetBlockNoLow();
                return true;
            }, () =>
            {
                if (servo.AlarmStatus)
                {
                    Stop($"{pm.Name} error");
                    return null;
                }
                if (!servo.IsBusy)
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
                    Stop($"{pm.Name}set Block No Low timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        private void ServoBlockOn(int id, PMModuleBase pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} set Stb On.");

                servo.SetStbOn();
                return true;
            }, () =>
            {
                if (servo.AlarmStatus)
                {
                    Stop($"{pm.Name} Servo in error State");
                    return null;
                }
                if (!servo.IsBusy)
                {
                    PMDevice.ServoState = ServoStates.Downing;
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
                    Stop($"{pm.Name}set Stb On timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        private void ServoBlockOff(int id, PMModuleBase pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} set Stb Off.");
                servo.SetStbOff();
                return true;
            }, () =>
            {
                if (servo.AlarmStatus)
                {
                    Stop($"{pm.Name} Servo in error State");
                    return null;
                }
                if (!servo.IsBusy)
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
                    Stop($"{pm.Name} set Stb Off timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        private void QueryStatusCorrect(int id, PMModule pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} Wait, Query Status Correct");
                servo.Query();
                return true;
            }, () =>
            {
                if (servo.AlarmStatus)
                {
                    Stop($"{pm.Name} error");
                    return null;
                }
                if ((!servo.MotorBusy && servo.PositionComplete) && pm.ConfinementRing.RingDownSensor)
                {
                    PMDevice.ServoState = ServoStates.Down;
                    return true;
                }
                else
                {
                    servo.Query();
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
                    Stop($"{pm.Name} Wait Status Correct timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        private void WaitStatusCorrect(int id, PMModule pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} Wait, Wait Status Correct");

                return true;
            }, () =>
            {
                if (servo.AlarmStatus)
                {
                    Stop($"{pm.Name} Servo in error State");
                    return null;
                }
                if ((!servo.MotorBusy && servo.PositionComplete) && pm.ConfinementRing.RingDownSensor)
                {
                    PMDevice.ServoState = ServoStates.Down;
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
                    Stop($"{pm.Name} Wait Status Correct timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        private void WaitTempratureBelow900(int id,int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                if (_pmIoInterLock != null)
                {
                    return _pmIoInterLock.DiHeaterTempBelow900CSW;
                }
                else
                {
                    return true;
                }
            },
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Wait PM Temprature below 900 timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public override void Abort()
        {
            if (!PMDevice.StopVent(out string reason))
            {
                EV.PostWarningLog(Module, reason);
            }

            Stop("aborted");
        }
    }
}
