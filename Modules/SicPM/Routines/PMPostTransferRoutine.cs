using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Servo.NAIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SicPM.PMModule;

namespace SicPM.Routines
{
    class PMPostTransferRoutine : PMBaseRoutine
    {
        enum RoutineStep
        {
            SetUpLimit,
            SetBlockOn,
            Delay,
            WaitStatusCorrect,
            SetBlockOff,
            EnableRotation,
            EnableHeater,
            SetRotation1, 
            SetTC1Mode, 
            SetTC1Ratio, 
            SetTC1Ref,
            SetTC2Mode, 
            SetTC2Ratio, 
            SetTC2Ref,
            SetChamberPressure,
            EnableTC1,
            EnableTC2,
            SetServoUp,
            SetScrReset,

            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
            TimeDelay4,
            TimeDelay5,
            TimeDelay6,
            TimeDelay7,
            TimeDelay8,
            TimeDelay9,
            TimeDelay10,
            TimeDelay11,
            TimeDelay12,
            TimeDelay13,
            TimeDelay14,
            TimeDelay15
        }

        bool hasWafer;

        private int _timeout;
        private int _rotationSpeed = 60;

        private bool _psuHeatEnable = false;          //是否启用PSU 加热
        private float _psuHeatMode = 0;
        //private float _psuPowerRef = 0;
        private float _psuL1Ratio = 0;
        private float _psuL2Ratio = 0;
        private float _psuL3Ratio = 0;

        private bool _scrHeatEnable = false;          //是否启用SCR 加热
        private float _scrHeatMode = 0;
        //private float _scrPowerRef = 0;
        private float _scrL1Ratio = 0;
        private float _scrL2Ratio = 0;
        private float _scrL3Ratio = 0;


        private double _targetPressure;
        private PMServoToPressure _pmServoToPressure;
        private bool _needEnableHeat = true;

        private NAISServo servo;

        public PMPostTransferRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "Post Transfer Routine";

            _pmServoToPressure = new PMServoToPressure(module, pm);
        }


        public void Init(bool needEnableHeat)
        {
            _needEnableHeat = needEnableHeat;
        }

        public void Init(float basePressure, int ventDelayTime)
        {

        }


        public override Result Start(params object[] objs)
        {
            servo = DEVICE.GetDevice<NAISServo>($"{Module}.NAISServo");

            _timeout = SC.GetValue<int>($"NAISServo.UpTimeout");
            _rotationSpeed = SC.GetValue<int>($"PM.{Module}.ProcessIdle.RotationSpeed");

            _psuHeatEnable = SC.GetValue<bool>($"PM.{Module}.ProcessIdle.PSUHeaterEnable");
            _psuHeatMode = (float)SC.GetValue<int>($"PM.{Module}.ProcessIdle.PSUHeaterMode");
            //_psuPowerRef = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUPowerRef");
            _psuL1Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUInnerRatio");
            _psuL2Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUMiddleRatio");
            _psuL3Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUOuterRatio");

            _scrHeatEnable = SC.GetValue<bool>($"PM.{Module}.ProcessIdle.SCRHeaterEnable");
            _scrHeatMode = (float)SC.GetValue<int>($"PM.{Module}.ProcessIdle.SCRHeaterMode");
            //_scrPowerRef = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRPowerRef");
            _scrL1Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRUpperRatio");
            _scrL2Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRMiddleRatio");
            _scrL3Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRLowerRatio");
            _targetPressure = SC.GetValue<double>($"PM.{Module}.ProcessIdle.FinalPressure");
            _pmServoToPressure.Init(_targetPressure);


            hasWafer = WaferManager.Instance.CheckHasWafer(Module, 0);

            Reset();

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                EnableRotation((int)RoutineStep.EnableRotation, 5);
                SetRotationValveAndNoWait((int)RoutineStep.SetRotation1, _rotationSpeed);

                TimeDelay((int)RoutineStep.TimeDelay1, 5);
                
                //! 无论有无Wafer，传完盘后均关闭隔热罩。
                SetConfinementRingUpAndWait((int)RoutineStep.SetServoUp, 30);

                ExecuteRoutine((int)RoutineStep.SetChamberPressure, _pmServoToPressure);

                TimeDelay((int)RoutineStep.TimeDelay8, 1);

                if (_psuHeatEnable && _needEnableHeat)
                {
                    SetPSUEnable((int)RoutineStep.EnableTC1, true, 5);

                    SetPSUHeatMode((int)RoutineStep.SetTC1Mode, _psuHeatMode);
                    TimeDelay((int)RoutineStep.TimeDelay9, 1);
                    SetPSUHeatRatio((int)RoutineStep.SetTC1Ratio, _psuL1Ratio, _psuL2Ratio, _psuL3Ratio);
                }
                if (_scrHeatEnable && _needEnableHeat)
                {
                    SetSCRReset((int)RoutineStep.SetScrReset);
                    TimeDelay((int)RoutineStep.TimeDelay10, 1);

                    SetSCREnable((int)RoutineStep.EnableTC2, true, 5);

                    SetSCRHeatMode((int)RoutineStep.SetTC2Mode, _scrHeatMode);
                    TimeDelay((int)RoutineStep.TimeDelay11, 1);
                    SetSCRHeatRatio((int)RoutineStep.SetTC2Ratio, _scrL1Ratio, _scrL2Ratio, _scrL3Ratio);
                }

                //SetRotationValve((int)RoutineStep.SetRotation1, _rotationSpeed, true, _rotationSpeed/2);
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

        //private void ServoUpLimit(int id, PMModuleBase pm, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run {pm.Name} set Block No High.");

        //        //if (!servo.SetBlockNoHigh())
        //        //{
        //        //    Stop(reason);
        //        //    return false;
        //        //}
        //        servo.SetBlockNoHigh();
        //        return true;
        //    }, () =>
        //    {
        //        if (servo.AlarmStatus)
        //        {
        //            Stop($"{pm.Name} Servo in error State.");
        //            return null;
        //        }
        //        if (!servo.IsBusy)
        //            return true;

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
        //            Stop($"{pm.Name} prepare vent timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}
        //private void ServoBlockOn(int id, PMModuleBase pm, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run {pm.Name} set Stb On.");

        //        //if (!servo.SetBlockNoHigh())
        //        //{
        //        //    Stop(reason);
        //        //    return false;
        //        //}
        //        servo.SetStbOn();
        //        return true;
        //    }, () =>
        //    {
        //        if (servo.AlarmStatus)
        //        {
        //            Stop($"{pm.Name} Servo in error State.");
        //            return null;
        //        }
        //        if (!servo.IsBusy)
        //        {
        //            PMDevice.ServoState = ServoStates.Uping;
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
        //            Stop($"{pm.Name} prepare vent timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}
        //private void ServoBlockOff(int id, PMModuleBase pm, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run {pm.Name} set Stb Off.");

        //        //if (!servo.SetBlockNoHigh())
        //        //{
        //        //    Stop(reason);
        //        //    return false;
        //        //}
        //        servo.SetStbOff();
        //        return true;
        //    }, () =>
        //    {
        //        if (servo.AlarmStatus)
        //        {
        //            Stop($"{pm.Name} Servo in error State.");
        //            return null;
        //        }
        //        if (!servo.IsBusy)
        //            return true;

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
        //            Stop($"{pm.Name} prepare vent timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}
        //private void WaitStatusCorrect(int id, PMModule pm, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run {pm.Name} Wait, Wait Status Correct");

        //        return true;
        //    }, () =>
        //    {
        //        if (servo.AlarmStatus)
        //        {
        //            Stop($"{pm.Name} Servo in error State error");
        //            return null;
        //        }
        //        if ((!servo.MotorBusy && servo.PositionComplete) && pm.ConfinementRing.IsUp)
        //        {
        //            PMDevice.ServoState = ServoStates.Up;
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
        //            Stop($"{pm.Name} vent timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

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
