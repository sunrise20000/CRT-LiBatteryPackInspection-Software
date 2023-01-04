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
    public class PMPurgeRoutine : PMBaseRoutine
    {
        private enum RoutineStep
        {
            RotationEnable,
            HeatEnable,
            VentPumpClose,
            ArSupply,

            SetGroupA,
            SetGroupB,
            SetGroupC,
            SetGroupE,
            SetGroupF,
            SetGroupH,
            SetGroupD,
            SetGroupG,
            SetGroupK,

            SetMFCMode,
            SetPCMode,

            SetM1to16,
            SetPC,
            SetGroupJ,
            CloseGroupJ,
            SetM291519to38,

            SetGasOut,
            SetTv,

            SetGasIn1,      //V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetMfc19to26,

            SetTVCloseMode,
            SetTvPositionToZero,

            SetGasIn2,
            SetGroupJOpen,
            SetM291519to26,
            SetM291519to26_1,

            OpenFinal2,
            SetMfcFinal2,

            SetEPV1,
            SetEPV2,
            SetTvMode,

            SetTVEnable,
            SetTVPressMode,
            SetTVPressMode1,
            SetTv1,
            WaitTv1,
            SetTVPositionMode,
            SetTVPositionMode_1,
            SetTVPositionMax,
            SetTVPositionMax_1,

            SetTvOpen,
            SetTvModeToPress,
            SetTVtoCurrent,
            RoutinePre,

            SetPressureUp,
            SetPressureDown,
            WaitPressureDown,
            WaitPressureUp,
            VentPumpOpen,

            PressureToMax,
            PressureToMin,

            StartLoop,
            EndLoop,

            SetMfc28to31,
            SetReactorPress,
            CloseFinanl1,
            CloseFinanl2,
            CloseEPV2,
            SetTVCloseMode2,
            CLoseTvvvv,
            SetEPVVV2,
            CLoseEPV1,
            CheckPM1000,
            WaitPressUpTo1000,
            WaitPressUpTo1020,

            SetPressUpOrDown1,
            SetPressUpOrDown2,
            SetMfc28to40Special,
            WaitPmPressureUp,
            SetMfc27to40Default,

            SetV75,
            SetV76,
            SetV31,
            SetV32,
            SetV35V36,
            SetV65,
            SetV751,
            SetV761,
            SetM2toM40,
            SetM2toM40_1,
            SetM2TOM40Default,

            SetV92V93V95,
            SetV94,
            SetV96,
            SetV68,
            SetM32M35M37,
            SetM36,
            SetM38,

            SetPC567Close,
            SetPC567Mode,
            SetPC567Default,
            OpenEPV2,
            WaitPressUpTo20, 
            SetMfc291519to38, 
            WaitPressUpTo0, 
            SetMfc291519to38ToDefault,
            WaitPressDownTo20, 
            SetPmPressureTo20,
            CloseMfcFinal1,
            CloseMfcFinal2,
            CloseMfc2915,
            CheckFinal1Open,
            SetGroupV25,
            Mfc36, 
            SetM39,
            CloseMfc1to16, CloseMfc2to26, CloseJ,

            CheckTVOpen1,
            CheckTVOpen2,
            CheckEPV1Open1,
            CheckEPV2Open1,
            CheckEPV2Open2,

            CloseM32M35M37,
            CloseM36,
            CloseM38,
            CloseGasIn1,
            WaitPmPressureUp1,

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
            TimeDelay14,
            TimeDelay18,
            TimeDelay19,
            TimeDelay20,
            TimeDelay21,
            TimeDelay22,

            Notify1,
            Notify2
        }

        private ModuleName moduleName;
        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;
        private IoInterLock _pmIoInterLock;


        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed =0 ; //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;   //旋转停止超时
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间

        private int _EPV2OpenTimeDelay = 10;

        private double _ventBasePressure;           
        private double _pumpBasePressure;           
        private int _pressureLoopCount;         //Loop次数
        private int _pressureMinDelay;
        private int _pressureMaxDelay;
        private int _pressureMinTimeout;
        private int _pressureMaxTimeout;

        private double _ventFinalPressure;

        private int _IoValueOpenCloseTimeout = 10;   //开关阀门超时时间
        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间
        private int _EPV12TimeSapn = 10;
        private int _waitChamberTo1000TimeOut = 300;    //等待腔体压力上升到1000的超时时间

        private Stopwatch _swTimer = new Stopwatch();
        public PMPurgeRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            moduleName = module;
            _pmModule = pm;
            Name = "Purge";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }
     

        public override Result Start(params object[] objs)
        {
            Reset(); 

            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");

            _pressureLoopCount = SC.GetValue<int>($"PM.{Module}.Purge.CyclePurgeCount");
            _pumpBasePressure = SC.GetValue<double>($"PM.{Module}.Purge.PumpBasePressure");
            _pressureMinDelay = SC.GetValue<int>($"PM.{Module}.Purge.PumpDelayTime");
            _pressureMinTimeout = SC.GetValue<int>($"PM.{Module}.Purge.PumpTimeout");
            _ventBasePressure = SC.GetValue<double>($"PM.{Module}.Purge.VentBasePressure");
            _ventFinalPressure = SC.GetValue<double>($"PM.{Module}.Vent.VentBasePressure");
            _pressureMaxDelay = SC.GetValue<int>($"PM.{Module}.Purge.VentDelayTime");
            _pressureMaxTimeout = SC.GetValue<int>($"PM.{Module}.Purge.VentTimeout");

            _waitChamberTo1000TimeOut = _pressureMaxTimeout;
                
            //_routineTimeOut = SC.GetValue<int>($"PM.{Module}.Purge.RoutineTimeOut");

            //Clean succeed 清掉
            _pmIoInterLock.DoLidCloseRoutineSucceed = false;
            _pmIoInterLock.DoLidOpenRoutineSucceed = false;

            if (!_pmIoInterLock.SetPMPurgeRoutineRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Purge,{reason}");
                return Result.FAIL;
            }

            currentPressureUpOrDown = PressureUpOrDown.None;

            _finalOpen = false;
            _swTimer.Restart();
            Notify($"Start   LoopCount:{_pressureLoopCount}");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                //CheckRoutineTimeOut();

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

                //打开V65
                SetIoValueByGroup((int)RoutineStep.SetV65, IoGroupName.V65, true, _IoValueOpenCloseTimeout);

                //设置MFC和PC的模式
                SetMfcModeToNormalByGroup((int)RoutineStep.SetMFCMode, MfcGroupName.All);
                SetPcModeToNormal((int)RoutineStep.SetPCMode, _lstPcList);

                //打开J阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupJ, IoGroupName.J, true, _IoValueOpenCloseTimeout);

                //M1-M16设定为default值(M2、M9、M15除外)
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);

                //设置所有PC到默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //设置M2,M9,M15,M19-M26 3s ramp 到default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM291519to26, MfcGroupName.M2toM26, 3);

                //打开EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay1, 3);

                //设置蝶阀Enable
                SetThrottleEnableAndWait((int)RoutineStep.SetTVEnable, _IoThrottle, 5);

                //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode, _IoThrottle, 5);

                //伺服压力设定值到0mbar
                SetThrottlePressureAndWaitSetPoint((int)RoutineStep.SetTv1, _IoThrottle, _pumpBasePressure, _pmPressureMaxDiff, _throttleTimeout);

                //M2、M9、M15、M19-M40 MFC 5s ramp 到0
                SetMfcByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40,0, 5);

                //等待腔体压力Pump到设定值
                WaitChamberPressDownTo((int)RoutineStep.WaitTv1, _pumpBasePressure, _pmPressureMaxDiff, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay2, 3);

                //蝶阀设置为位置模式,开度设置为最大
                SetThrottleToPositionMode((int)RoutineStep.SetTVPositionMode, _IoThrottle, _throttleTimeout);
                SetThrottleSetPosition((int)RoutineStep.SetTVPositionMax, _IoThrottle, 100, _throttleTimeout);

                //Final2阀对应的MFC 3s Ramp到Default值
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfcFinal2, MfcGroupName.Final2, 3);

                //打开Final2阀
                SetIoValueByGroup((int)RoutineStep.OpenFinal2, IoGroupName.Final2, true, _IoValueOpenCloseTimeout);

                //打开V68
                SetIoValueByGroup((int)RoutineStep.SetV68, IoGroupName.GasIn1, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay3, 1);

                //打开V92,V93和V95,再打开V94,在打开V96
                SetMfcToDefaultByGroup((int)RoutineStep.SetM32M35M37, MfcGroupName.M32M35M37, 1);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM36, MfcGroupName.M36, 1);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM38, MfcGroupName.M38, 1);
                TimeDelay((int)RoutineStep.TimeDelay4, 3);
                SetIoValueByGroup((int)RoutineStep.SetV92V93V95, IoGroupName.V92V93V95, true, _IoValueOpenCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetV94, IoGroupName.V94, true, _IoValueOpenCloseTimeout);

                TimeDelay((int)RoutineStep.TimeDelay5, 3);
                SetIoValueByGroup((int)RoutineStep.SetV96, IoGroupName.V96, true, _IoValueOpenCloseTimeout);

                //循环开始
                Loop((int)RoutineStep.StartLoop, _pressureLoopCount);

                //M2、M9、M15、M19-M40 MFC 3s ramp 到 default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM2TOM40Default, MfcGroupName.M2toM40, 3);

                //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode1, _IoThrottle, 5);

                //使用动态流量伺服到300mbar            
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown1, PressureUpOrDown.Uping);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _ventBasePressure);
                WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.WaitPressureUp, _IoThrottle, _ventBasePressure, _pmPressureMaxDiff, _pressureMaxTimeout);
                TimeDelay((int)RoutineStep.TimeDelay6, _pressureMaxDelay);

                //M2、M9、M15、M19-M40 MFC 3s ramp 0
                SetMfcByGroup((int)RoutineStep.SetM2toM40_1, MfcGroupName.M2toM40, 0, 3);

                //伺服到0mbar 
                SetThrottlePressureAndWait((int)RoutineStep.SetPressureDown, _IoThrottle, _pumpBasePressure, _pmPressureMaxDiff, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay7, 3);

                //蝶阀设置为位置模式,开度设置为最大
                SetThrottleToPositionMode((int)RoutineStep.SetTVPositionMode_1, _IoThrottle, _throttleTimeout);
                SetThrottleSetPositionNoWait((int)RoutineStep.SetTVPositionMax_1, _IoThrottle, 100, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay8, _pressureMinDelay);

                EndLoop((int)RoutineStep.EndLoop);

                //M2,M9,M15,M19-M26 3s ramp 到0
                SetMfcByGroup((int)RoutineStep.SetM291519to26_1, MfcGroupName.M2toM26, 0,3);

                //M27-M40 MFC 3s ramp 到 default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfc27to40Default, MfcGroupName.M27toM40, 3);

                //关闭J阀门
                SetIoValueByGroup((int)RoutineStep.CloseGroupJ, IoGroupName.J, false, _IoValueOpenCloseTimeout);

                //关闭先关蝶阀,再EPV2
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode2, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.CLoseTvvvv, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay9, 1);
                SetIoValueByGroup((int)RoutineStep.SetEPVVV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);

                //等待反应腔压力到达1020
                NotifyInfo((int)RoutineStep.Notify1, $"Wait pm pressure to {_ventFinalPressure} mbar!");
                WaitChamberPressUpTo((int)RoutineStep.WaitPressUpTo1000, _ventFinalPressure, _waitChamberTo1000TimeOut);                
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
            SetRoutineSuccessedDo();
            return Result.DONE;
        }
        


        public override void Abort()
        {
            _pmIoInterLock.DoLidOpenRoutineRunning = false;
            PMDevice._ioThrottleValve.StopRamp();
            PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
            PMDevice.SetRotationServo(0, 0);

            base.Abort();
        }


        private void SetRoutineSuccessedDo()
        {
            _pmIoInterLock.DoLidOpenRoutineRunning = false;
            _pmIoInterLock.SetLidOpenRoutineSucceed(true, out string reason);
            _pmIoInterLock.DoLidCloseRoutineSucceed = false;
        }


        //private void CheckRoutineTimeOut()
        //{
        //    if (_routineTimeOut > 10)
        //    {
        //        if ((int)(_swTimer.ElapsedMilliseconds / 1000) > _routineTimeOut)
        //        {
        //            EV.PostAlarmLog(Module,$"Routine TimeOut! over {_routineTimeOut} s");
        //            throw (new RoutineFaildException());
        //        }
        //    }
        //}
    }
}
