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
    public class PMLidOpenRoutine : PMBaseRoutine
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
            SetM291519to38,

            SetGasOut,
            SetTv,

            SetGasIn1,      //V65,V67,V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetMfc19to26,

            SetTVCloseMode,
            SetTvPositionToZero,

            SetGasIn2,
            SetGroupJOpen,
            SetM291519to26,

            OpenFinal2,
            SetMfcFinal2,

            SetEPV1,
            SetEPV2,
            SetTvMode,

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
            SetMfc28to31Special,
            WaitPmPressureUp,
            SetMfc28to31Default,

            SetV75,
            SetV76,
            SetV751,
            SetV761,

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
            SetGroupV38,
            SetGroupV25,
            SetV94, 
            Mfc36,

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
            TimeDelay18,
            TimeDelay19,

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

        private double _pressureMax;            //Loop压力上限可设置600-800）
        private double _pressureMin;            //Loop压力下限可设置  0-400）
        private int _pressureLoopCount;         //Loop次数
        private int _pressureMinDelay;
        private int _pressureMaxDelay;
        private int _pressureMinTimeout;
        private int _pressureMaxTimeout;

        private int _IoValueOpenCloseTimeout = 10;   //开关阀门超时时间
        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间
        private int _EPV12TimeSapn = 10;
        private int _waitChamberTo1000TimeOut = 300;    //等待腔体压力上升到1000的超时时间

        private double _mfc28SpecialFlow = 15000;
        private double _mfc29SpecialFlow = 5000;
        private double _mfc31SpecialFlow = 10000;
        //private int _routineTimeOut;
        private int _EPV2OpenDelayTime = 9;
        private bool _needV94Open = false;
        private double _mfc36Flow = 0;

        private Stopwatch _swTimer = new Stopwatch();
        public PMLidOpenRoutine(ModuleName module, PMModule pm) : base(module, pm)
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

            _EPV12TimeSapn = SC.GetConfigItem($"PM.{Module}.EPV2OpenTimeDelayAlterEPV1Open").IntValue;
            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _EPV2OpenTimeDelay = SC.GetConfigItem($"PM.{Module}.EPV2OpenTimeDelayAlterEPV1Open").IntValue;
            _mfc28SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc28FlowSpecail");
            _mfc29SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc29FlowSpecail");
            _mfc31SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc31FlowSpecail");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");


            _pressureLoopCount = SC.GetValue<int>($"PM.{Module}.Purge.CyclePurgeCount");
            _pressureMin = SC.GetValue<double>($"PM.{Module}.Purge.PumpBasePressure");
            _pressureMinDelay = SC.GetValue<int>($"PM.{Module}.Purge.PumpDelayTime");
            _pressureMinTimeout = SC.GetValue<int>($"PM.{Module}.Purge.PumpTimeout");
            _pressureMax = SC.GetValue<double>($"PM.{Module}.Purge.VentBasePressure");
            _pressureMaxDelay = SC.GetValue<int>($"PM.{Module}.Purge.VentDelayTime");
            _pressureMaxTimeout = SC.GetValue<int>($"PM.{Module}.Purge.VentTimeout");
            //_routineTimeOut = SC.GetValue<int>($"PM.{Module}.Purge.RoutineTimeOut");

            _EPV2OpenDelayTime = SC.GetValue<int>($"PM.{Module}.TimeDelayAlterEPV2Open");
            _needV94Open = SC.GetValue<bool>($"PM.{Module}.Purge.NeedV94Open");
            _mfc36Flow = SC.GetValue<double>($"PM.{Module}.Purge.Mfc36Flow");

            if (!_pmIoInterLock.SetPMLidOpenRoutineRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Purge,{reason}");
                return Result.FAIL;
            }
            //Clean succeed 清掉
            _pmIoInterLock.DoLidCloseRoutineSucceed = false;
            //purge succeed 清掉
            _pmIoInterLock.DoLidOpenRoutineSucceed = false;

            currentPressureUpOrDown = PressureUpOrDown.None;

            _finalOpen = false;
            _swTimer.Restart();
            Notify($"Start   LoopCount:{_pressureLoopCount}");
            return Result.RUN;

            
        }

        //Purge
        public override Result Monitor()
        {
            try
            {
                //CheckRoutineTimeOut();

                SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);
                SetRotationValve((int)RoutineStep.RotationEnable, _rotationCheckSpeed, false, _rotationCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.VentPumpClose, IoGroupName.VentPump, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.ArSupply, IoGroupName.ArSupply, true, _IoValueOpenCloseTimeout);

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

                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16,0);
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                CheckFinalIoStatue((int)RoutineStep.CheckFinal1Open);
                if (!_finalOpen)
                {
                    SetMfcByGroup((int)RoutineStep.SetM291519to38, MfcGroupName.M291519to38, 0, 5);
                    TimeDelay((int)RoutineStep.TimeDelay1, 5);
                }


                //1.打开抽气系统 （打开EPV1,关闭EPV2,打开throttle valve enable压力伺服当前压力值） [由于InterLock原因,需要先开EPV1再开EPV2]
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay19, 1);
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetEPV1, IoGroupName.EPV1, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay2, _EPV2OpenTimeDelay);
               

                //2.打开进气系统 （V65,V67,V68)
                SetIoValueByGroup((int)RoutineStep.SetGasIn1, IoGroupName.GasIn1, true, _IoValueOpenCloseTimeout);

                //3.打开Final valves 和对应的MFC值
                SetIoValueByGroup((int)RoutineStep.OpenFinal2, IoGroupName.Final2, true, _IoValueOpenCloseTimeout);
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfcFinal2, MfcGroupName.Final2, 3);
                TimeDelay((int)RoutineStep.TimeDelay3, 3);
                SetIoValueByGroup((int)RoutineStep.OpenFinal1, IoGroupName.Final1, true, _IoValueOpenCloseTimeout);
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfcFinal1, MfcGroupName.Final1, 3);
                TimeDelay((int)RoutineStep.TimeDelay4, 3);

                //4.打开J Valves
                SetIoValueByGroup((int)RoutineStep.SetGroupJOpen, IoGroupName.J, true, _IoValueOpenCloseTimeout);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM291519to26, MfcGroupName.M291519to26, 3);


                //打开EPV2,等待9秒,打开蝶阀
                SetIoValueByGroup((int)RoutineStep.OpenEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay5, _EPV2OpenDelayTime);
                SetThrottleEnableAndWait((int)RoutineStep.SetTvOpen, _IoThrottle, 10);
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTvModeToPress, _IoThrottle, 10);

                ////Loop
                ////Loop((int)RoutineStep.StartLoop, _pressureLoopCount);
                ////SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown1, PressureUpOrDown.Dowing);
                ////SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureDown, _IoThrottle, 20);
                ////WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.WaitPressureDown, _IoThrottle, 20, _pmPressureMaxDiff, _throttleTimeout);

                //////等待Pm压力到20，设置Pm压力到0                
                ////SetMfcByGroup((int)RoutineStep.SetMfc291519to38, MfcGroupName.M291519to38, 0, 2);
                ////SetThrottlePressureAndWait((int)RoutineStep.SetPmPressureTo20, _IoThrottle, 0, _pmPressureMaxDiff, _throttleTimeout);
                ////TimeDelay((int)RoutineStep.TimeDelay7, _pressureMinDelay);

                //////设置Pm压力到300
                ////SetMfcToDefaultByGroup((int)RoutineStep.SetMfc291519to38ToDefault, MfcGroupName.M291519to38, 2);
                ////SetThrottlePressureAndWait((int)RoutineStep.WaitPressUpTo20, _IoThrottle, 20, _pmPressureMaxDiff, _throttleTimeout);
                ////SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown2, PressureUpOrDown.Uping);
                ////SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _pressureMax);
                ////WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.WaitPressureUp, _IoThrottle, _pressureMax, _pmPressureMaxDiff, _throttleTimeout);
                ////TimeDelay((int)RoutineStep.TimeDelay8, _pressureMaxDelay);
                ////EndLoop((int)RoutineStep.EndLoop);
                
                Loop((int)RoutineStep.StartLoop, _pressureLoopCount);
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown1, PressureUpOrDown.Dowing);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureDown, _IoThrottle, _pressureMin);
                WaitThrottleToPressureAndSetMfcSpecialForLidOpen((int)RoutineStep.WaitPressureDown, _IoThrottle, _pressureMin, _pmPressureMaxDiff, _pressureMinTimeout);
                TimeDelay((int)RoutineStep.TimeDelay7, _pressureMinDelay);

                //设置Pm压力到300                
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown2, PressureUpOrDown.Uping);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _pressureMax);
                WaitThrottleToPressureAndSetMfcSpecialForLidOpen((int)RoutineStep.WaitPressureUp, _IoThrottle, _pressureMax, _pmPressureMaxDiff, _pressureMaxTimeout);
                TimeDelay((int)RoutineStep.TimeDelay8, _pressureMaxDelay);
                EndLoop((int)RoutineStep.EndLoop);




                //关闭先关蝶阀,再EPV2
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode2, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.CLoseTvvvv, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay18, 1);
                SetIoValueByGroup((int)RoutineStep.SetEPVVV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);

                //判断反应腔压力是否超过1000mbar
                GetPressureCondition((int)RoutineStep.CheckPM1000, 1000);
                if (!_checkPMPressureOver1000)
                {
                    //设置M28,M29,M31 为特定值
                    SetMfc28to31Special((int)RoutineStep.SetMfc28to31Special, _mfc28SpecialFlow, _mfc29SpecialFlow, _mfc31SpecialFlow, 1);
                }

                //等待反应腔压力到达1000
                NotifyInfo((int)RoutineStep.Notify1, "Wait pm pressure to 1000 mbar!");
                WaitChamberPressUpTo((int)RoutineStep.WaitPressUpTo1000, 1000, _waitChamberTo1000TimeOut);                
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfc28to31Default, MfcGroupName.M28toM31, 0);
                TimeDelay((int)RoutineStep.TimeDelay9, 1);
                SetIoValueByGroup((int)RoutineStep.CLoseEPV1, IoGroupName.EPV1, false, _IoValueOpenCloseTimeout);

                //等待反应腔压力到达1020
                NotifyInfo((int)RoutineStep.Notify2, "Wait pm pressure to 1020 mbar!");
                WaitChamberPressUpTo((int)RoutineStep.WaitPressUpTo1020, 1020, _waitChamberTo1000TimeOut);

                //关闭V87-V96
                SetMfcByGroup((int)RoutineStep.CloseMfcFinal1, MfcGroupName.M32toM38, 0, 5);
                TimeDelay((int)RoutineStep.TimeDelay10, 5);
                SetIoValueByGroup((int)RoutineStep.CloseFinanl1, IoGroupName.Final1, false, _IoValueOpenCloseTimeout);
                SetMfcByGroup((int)RoutineStep.CloseMfcFinal2, MfcGroupName.M19toM31, 0, 5);
                TimeDelay((int)RoutineStep.TimeDelay11, 5);
                SetIoValueByGroup((int)RoutineStep.CloseFinanl2, IoGroupName.Final2, false, _IoValueOpenCloseTimeout);
                SetMfcByGroup((int)RoutineStep.CloseMfc2915, MfcGroupName.M2915, 0, 5);

                //打开V94,M36设置为Default值
                if (_needV94Open)
                {
                    //I Group 去掉V94 qbh 20220319
                    //SetIoValueByGroup((int)RoutineStep.SetV94, IoGroupName.V94, true, _IoValueOpenCloseTimeout);                    
                    SetMfcByGroup((int)RoutineStep.Mfc36, MfcGroupName.M36, _mfc36Flow, 3);
                }


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
            SetRoutineRuningDo();
            SetRoutineSuccessedDo();
            return Result.DONE;
        }
        


        public override void Abort()
        {
            SetRoutineRuningDo();
            PMDevice._ioThrottleValve.StopRamp();
            PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
            base.Abort();
        }

        private void SetRoutineRuningDo()
        {
            _pmIoInterLock.DoLidOpenRoutineRunning = false;
        }

        private void SetRoutineSuccessedDo()
        {
            _pmIoInterLock.SetLidOpenRoutineSucceed(true, out string reason);
            _pmIoInterLock.DoLidCloseRoutineSucceed = false;
        }


        //private void CheckRoutineTimeOut()
        //{
        //    if (_routineTimeOut > 10)
        //    {
        //        if ((int)(_swTimer.ElapsedMilliseconds / 1000) > _routineTimeOut)
        //        {
        //            Notify($"Routine TimeOut! over {_routineTimeOut} s");
        //            throw (new RoutineFaildException());
        //        }
        //    }
        //}
    }
}
