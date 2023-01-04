using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using SicPM.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMPumpDownRoutine : PMBaseRoutine
    {
        private enum RoutineStep
        {
            RotationEnable,
            HeatEnable,
            VentPumpClose,
            ArSupply,

            SetGroupB,
            SetGroupC,
            SetGroupE,
            SetGroupF,
            SetGroupH,
            SetGroupD,
            SetGroupG,
            SetGroupK,

            SetM1to16,
            SetPC,
            SetGroupI,      //V87-96
            SetM19to38,
            MfcAll,

            SetGasOut,
            SetTv, 
            SetTv1,

            SetGasIn1,      //V65,V67,V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38

            SetGasIn2,
            SetGroupJOpen,

            OpenFinal2,
            SetMfcFinal2,

            PumpDownTimeout,
            SetTvMode,

            SetEPV1,
            SetEPV2,
            SetTVCloseMode,
            SetTvPositionToZero,
            SetMFCMode,
            SetPCMode,

            SetTvOpen,
            SetTvModeToPress,
            SetTVto1050,
            CloseFinanl1,
            CloseFinanl2,
            SetGroupV38,

            SetV76,
            SetV75, SetGroupV25,

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
        }

        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed =0 ;            //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;          //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10;   //开关超时时间
        private int _heatTimeOut = 5;               //Heat关闭等待Di反馈超时时间

        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间

        private int _pumpDownKeep = 10;
        private int _EPV12TimeSapn = 10;
        private double _pumpBasePressure; 
        private int _routineTimeOut; 
        private int _EPV2OpenDelayTime = 9;

        private Stopwatch _swTimer = new Stopwatch();
        public PMPumpDownRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            _pmModule = pm;
            Name = "PumpDown";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");

            _pumpBasePressure= SC.GetValue<double>($"PM.{Module}.Pump.PumpBasePressure");
            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.Pump.RoutineTimeOut");

            _EPV2OpenDelayTime = SC.GetValue<int>($"PM.{Module}.TimeDelayAlterEPV2Open");

            _swTimer.Restart();
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                CheckRoutineTimeOut();

                SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);
                SetRotationValve((int)RoutineStep.RotationEnable, _rotationCheckSpeed, false, _rotationCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetGroupB, IoGroupName.B, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupC, IoGroupName.C, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupE, IoGroupName.E, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupF, IoGroupName.F, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupH, IoGroupName.H, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupV38, IoGroupName.V38, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupK, IoGroupName.K, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupV25, IoGroupName.V25, true, _IoValueOpenCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetGroupD, IoGroupName.D, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupG, IoGroupName.G, true, _IoValueOpenCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetV76, IoGroupName.V76, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetV75, IoGroupName.V75, true, _IoValueOpenCloseTimeout);

                SetMfcModeToNormalByGroup((int)RoutineStep.SetMFCMode, MfcGroupName.All);
                SetPcModeToNormal((int)RoutineStep.SetPCMode, _lstPcList);


                //1.关闭V72,所有PC,MFC设置到默认值
                SetIoValueByGroup((int)RoutineStep.VentPumpClose, IoGroupName.VentPump, false, _IoValueOpenCloseTimeout);
                SetMfcToDefaultByGroup((int)RoutineStep.MfcAll, MfcGroupName.All, 5);
                TimeDelay((int)RoutineStep.TimeDelay1, 5);
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);
                SetMfcByGroup((int)RoutineStep.SetM19to38, MfcGroupName.M291519to38, 0,3);
                TimeDelay((int)RoutineStep.TimeDelay2, 3);

                //2.依次关闭I Value
                SetIoValueByGroup((int)RoutineStep.CloseFinanl1, IoGroupName.Final1, false, _IoValueOpenCloseTimeout); 
                TimeDelay((int)RoutineStep.TimeDelay3, 1);
                SetIoValueByGroup((int)RoutineStep.CloseFinanl2, IoGroupName.Final2, false, _IoValueOpenCloseTimeout);

                //3.打开抽气系统 （打开EPV2,EPV1,打开throttle valve enable压力伺服当前压力值） [由于InterLock原因,需要先开EPV1再开EPV2]
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                SetIoValueByGroup((int)RoutineStep.SetEPV1, IoGroupName.EPV1, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay4, _EPV12TimeSapn);

                //打开EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay5, _EPV2OpenDelayTime);

                //打开蝶阀,压力设定为1050
                SetThrottleEnableAndWait((int)RoutineStep.SetTvOpen, _IoThrottle, 10);
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTvModeToPress, _IoThrottle, 10);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetTVto1050, _IoThrottle, 1050);
                TimeDelay((int)RoutineStep.TimeDelay6, 2);

                //4.Pump down到0mbar,达到稳定后保持10秒
                SetThrottlePressureAndWait((int)RoutineStep.SetTv1, _IoThrottle, _pumpBasePressure, _pmPressureMaxDiff, _throttleTimeout);
                Delay((int)RoutineStep.TimeDelay7, _pumpDownKeep);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _swTimer.Stop();

            return Result.DONE;
        }

        

        

        public override void Abort()
        {
            base.Abort();
        }

        private void CheckRoutineTimeOut()
        {
            if (_routineTimeOut > 10)
            {
                if ((int)(_swTimer.ElapsedMilliseconds / 1000) > _routineTimeOut)
                {
                    Notify($"Routine TimeOut! over {_routineTimeOut} s");
                    throw (new RoutineFaildException());
                }
            }
        }
    }
}
