using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using System;

namespace SicPM.Routines
{
    public class PMServoToPressure : PMBaseRoutine
    {
        enum RoutineStep
        {
            RotationEnable,
            HeatEnable,

            SetM1to16,
            SetM19to38,
            TimeDelayForMFC,

            SetTvOpen,
            SetTvMode,
            SetTVto1050,
            TimeDelayForTV,
            SetPressUpOrDown2,
            SetPressureUp,
            WaitPressureUp,

            SetTvPressure,
            SetEPV22,
            TimeDelay5,
        }


        private ModuleName moduleName;
        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;
        private double _throTagertPressure;            //蝶阀目标压力
        private double _throttleRate;               //蝶阀调节速率
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间
        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)

        //private int _rotationCheckSpeed = 0; //设置旋转速度为0后检查是否转速低于此数值
        //private int _rotationCloseTimeout = 20;   //旋转停止超时
        //private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间
        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        //private int _EPV2OpenDelayTime = 9;

        private bool _isEPV2Open = false;
        private bool _isBTVOpen = false;

        public PMServoToPressure(ModuleName module, PMModule pm) : base(module, pm)
        {
            moduleName = module;
            _pmModule = pm;
            Name = "Servo To Pressure";

        }

        public void Init(double targetPressure)
        {
            _throTagertPressure = targetPressure;
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            //_throTagertPressure = SC.GetValue<double>($"TM.PressureBalance.BalancePressure");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");
            //_EPV2OpenDelayTime = SC.GetValue<int>($"PM.{Module}.TimeDelayAlterEPV2Open");
            currentPressureUpOrDown = PressureUpOrDown.None;

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");

            _isEPV2Open = _pmModule.EPV2.Status;
            _isBTVOpen = _IoThrottle.TVValveEnable;

            if(Math.Abs(_throTagertPressure - _pmModule.GetChamberPressure()) <= _pmPressureMaxDiff)
            {
                return Result.DONE;
            }

            Notify("Start");
            Notify($"ThrottlePressureMaxDiff set to {_pmPressureMaxDiff:F1}");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                if (SC.GetValue<bool>("System.IsATMMode"))
                {
                    return Result.DONE;
                }

                if (!_isEPV2Open)
                {
                    SetIoValueByGroup((int)RoutineStep.SetEPV22, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                    TimeDelay((int)RoutineStep.TimeDelay5, 2);
                }

                if (!_isBTVOpen)
                {
                    SetThrottleEnableAndWait((int)RoutineStep.SetTvOpen, _IoThrottle, 10);
                    SetThrottleToPressModeAndWait((int)RoutineStep.SetTvMode, _IoThrottle, 10);
                    SetThrottleToTargetAndNoWait((int)RoutineStep.SetTVto1050, _IoThrottle, 1050);
                    TimeDelay((int)RoutineStep.TimeDelayForTV, 2);
                }

                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown2,PressureUpOrDown.Dowing);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _throTagertPressure);
                WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.WaitPressureUp, _IoThrottle, _throTagertPressure, _pmPressureMaxDiff, _throttleTimeout);

                SetThrottlePressureAndWait((int)RoutineStep.SetTvPressure, _IoThrottle, _throTagertPressure, _pmPressureMaxDiff, _throttleTimeout);
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


        public override void Abort()
        {
            base.Abort();
        }

    }
}
