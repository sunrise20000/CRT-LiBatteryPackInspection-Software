using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using SicPM.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMPumpRoutine : PMBaseRoutine
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
            SetGroupJ,

            SetM1to16,
            SetPC,
            SetGroupI,      //V87-96
            SetM2to40,
            SetM2to40_1,
            MfcAll,

            SetGasOut,
            SetTv, 
            SetTv1,
            WaitTv1,

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
            SetTVEnable,
            SetTVPressMode,
            SetTvPositionToZero,
            SetMFCMode,
            SetPCMode,

            SetTvOpen,
            SetTvModeToPress,
            SetTVto1050,
            OpenGasIn1,
            CloseGasIn1,
            CloseFinanl1,
            CloseFinanl2,
            SetGroupV38,

            SetV76,
            SetV75, 
            SetV65,
            SetGroupV25,
            SetV31,
            SetV32,
            SetV35V36,
            SetTVPositionMode,
            SetTVPositionMax,

            CloseM32M35M37,
            CloseM36,
            CloseM38,

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
        }

        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;
        private IoInterLock _pmIoInterLock;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed =0 ;            //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;          //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10;   //开关超时时间
        private int _heatTimeOut = 5;               //Heat关闭等待Di反馈超时时间

        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间

        private double _pumpBasePressure; 
        private int _routineTimeOut; 
        //private int _EPV2OpenDelayTime = 9;

        private Stopwatch _swTimer = new Stopwatch();
        public PMPumpRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            _pmModule = pm;
            Name = "PumpDown";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }

        public override Result Start(params object[] objs)
        {
             Reset();

            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");

            _pumpBasePressure= SC.GetValue<double>($"PM.{Module}.Pump.PumpBasePressure");
            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.Pump.RoutineTimeOut");


            if (!_pmIoInterLock.SetPMPumpRoutineRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Pump,{reason}");
                return Result.FAIL;
            }
            _swTimer.Restart();
            Notify("Start");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CheckRoutineTimeOut();

                //关闭V76,打开V75
                SetIoValueByGroup((int)RoutineStep.SetV76, IoGroupName.V76, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetV75, IoGroupName.V75, true, _IoValueOpenCloseTimeout);

                //旋转停止,加热停止
                SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);
                SetRotationValve((int)RoutineStep.RotationEnable, _rotationCheckSpeed, false, _rotationCloseTimeout);

                //关闭V72,打开V25
                SetIoValueByGroup((int)RoutineStep.VentPumpClose, IoGroupName.VentPump, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupV25, IoGroupName.V25, true, _IoValueOpenCloseTimeout);

                //打开V31,打开V32
                SetIoValueByGroup((int)RoutineStep.SetV31, IoGroupName.V31, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetV32, IoGroupName.V32, true, _IoValueOpenCloseTimeout);

                //打开V35,打开V36
                SetIoValueByGroup((int)RoutineStep.SetV35V36, IoGroupName.V35V36, true, _IoValueOpenCloseTimeout);

                //关闭B/C/E/F/H/K 阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupB, IoGroupName.B, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupC, IoGroupName.C, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupE, IoGroupName.E, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupF, IoGroupName.F, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupH, IoGroupName.H, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupK, IoGroupName.K, false, _IoValueOpenCloseTimeout);
               
                //打开D/G 阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupD, IoGroupName.D, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupG, IoGroupName.G, true, _IoValueOpenCloseTimeout);

                //设置MFC和PC的模式
                SetMfcModeToNormalByGroup((int)RoutineStep.SetMFCMode, MfcGroupName.All);
                SetPcModeToNormal((int)RoutineStep.SetPCMode, _lstPcList);

                //M1-M16设定为default值(M2、M9、M15除外)
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);

                //M2、M9、M15、M19-M40 MFC 3s ramp 到 default值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM2to40_1, MfcGroupName.M2toM40, 3);

                //设置所有PC到默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //打开V68
                SetIoValueByGroup((int)RoutineStep.OpenGasIn1, IoGroupName.GasIn1, true, _IoValueOpenCloseTimeout);

                //打开V65
                SetIoValueByGroup((int)RoutineStep.SetV65, IoGroupName.V65, true, _IoValueOpenCloseTimeout);

                //打开J阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupJ, IoGroupName.J, true, _IoValueOpenCloseTimeout);

                //打开EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay5, 3);

                //设置蝶阀Enable
                SetThrottleEnableAndWait((int)RoutineStep.SetTVEnable, _IoThrottle, 5);

               //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode, _IoThrottle, 5);

                //伺服压力设定值到0mbar
                SetThrottlePressureAndWaitSetPoint((int)RoutineStep.SetTv1, _IoThrottle, _pumpBasePressure, _pmPressureMaxDiff, _throttleTimeout);

                //M2、M9、M15、M19-M40 MFC 3s ramp到0
                SetMfcByGroup((int)RoutineStep.SetM2to40, MfcGroupName.M2toM40, 0, 3);

                //等待腔体压力Pump到设定值
                WaitChamberPressDownTo((int)RoutineStep.WaitTv1, _pumpBasePressure, _pmPressureMaxDiff,_throttleTimeout);

                //蝶阀设置为位置模式,开度设置为最大
                SetThrottleToPositionMode((int)RoutineStep.SetTVPositionMode, _IoThrottle, _throttleTimeout);
                SetThrottleSetPosition((int)RoutineStep.SetTVPositionMax, _IoThrottle, 100, _throttleTimeout);

                _pmIoInterLock.SetPMPumpRoutineRunning(false, out _);
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
            _pmIoInterLock.SetPMPumpRoutineRunning(false, out _);
            PMDevice.SetRotationServo(0, 0);

            base.Abort();
        }

        private void CheckRoutineTimeOut()
        {
            if (_routineTimeOut > 10)
            {
                if ((int)(_swTimer.ElapsedMilliseconds / 1000) > _routineTimeOut)
                {
                    EV.PostAlarmLog(Module, $"Routine TimeOut! over {_routineTimeOut} s");
                    throw (new RoutineFaildException());
                }
            }
        }
    }
}
