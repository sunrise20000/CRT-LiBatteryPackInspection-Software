using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using SicPM.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMLidCloseRoutine : PMBaseRoutine
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

            RoutinePre,
            V27Open,
            V27Close,

            PressureTo1050,
            SetPressureUp,
            SetPressureDown,
            VentPumpOpen,

            PressureToMax,
            PressureToMin,

            SetPressUpOrDown1,
            SetPressUpOrDown2,

            WaitPressureDown,
            WaitPressureUp,
            SetMFCMode,
            SetPCMode,
            WaitPmTo1050,
            SetTvOpen,
            SetTvModeToPress,
            SetEPV22,

            SetV76,
            SetV75,
            SetV751,
            SetV761,

            CheckPM1000, 
            SetMfc28to31Special, 
            WaitPressUpTo1000, 
            SetMfc28to31Default,

            SetPC567Close,
            SetPC567Mode,
            SetPC567Default,
            OpenEPV2,
            WaitV27Open,
            SetTvTo1150,
            WaitPressUpTo20, 
            SetMfc28to31To0, 
            WaitPressUpTo0,
            SetMfc28to31ToDefault,
            WaitPressDownTo20,
            CheckPMPt21000,
            SetPressureDown1,
            SetMfc28to31Default1,
            CheckFinal1Open,
            SetGroupV38,
            SetGroupV25,

            StartLoop,
            EndLoop,
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

            Notify1,
            Notify2,
            Notify3,
            Notify4,
        }

        private ModuleName moduleName;
        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;
        private IoInterLock _pmIoInterLock;



        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed =0 ; //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout=20;   //旋转停止超时
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间

        private int _pressureMaxDelay = 10;         //到低压保持时间（采用固定值10s）
        private int _pressureMinDelay = 10;          //到高压保持时间（采用固定值10s）
        private double _pressureMax;            //Loop压力上限可设置600-800）
        private double _pressureMin;            //Loop压力下限可设置  0-400）
        private int _pressureMinTimeout;
        private int _pressureMaxTimeout;
        private int _pressureLoopCount;         //Loop次数

        private int _IoValueOpenCloseTimeout = 10;   //开关阀门超时时间
        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间
        private int _EPV2OpenTimeDelay =10;             //EPV2
        private int _waitChamberTo1000TimeOut = 180;    //等待腔体压力上升到1000的超时时间

        private double _mfc28SpecialFlow = 15000;
        private double _mfc29SpecialFlow = 5000;
        private double _mfc31SpecialFlow = 10000;
        private int _EPV2OpenDelayTime = 9;


        private int _pmCleanTime = 20;
        //private int _routineTimeOut;

        private Stopwatch _swTimer = new Stopwatch();

        public PMLidCloseRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            moduleName = module;
            _pmModule = pm;
            Name = "Clean";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _mfc28SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc28FlowSpecail");
            _mfc29SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc29FlowSpecail");
            _mfc31SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc31FlowSpecail");
            _EPV2OpenTimeDelay = SC.GetConfigItem($"PM.{Module}.EPV2OpenTimeDelayAlterEPV1Open").IntValue;
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");

            _pressureLoopCount = SC.GetValue<int>($"PM.{Module}.Clean.CycleCleanCount");
            _pressureMin = SC.GetValue<double>($"PM.{Module}.Clean.PumpBasePressure");
            _pressureMinDelay = SC.GetValue<int>($"PM.{Module}.Clean.PumpDelayTime");
            _pressureMinTimeout = SC.GetValue<int>($"PM.{Module}.Clean.PumpTimeout");
            _pressureMax = SC.GetValue<double>($"PM.{Module}.Clean.VentBasePressure");
            _pressureMaxDelay = SC.GetValue<int>($"PM.{Module}.Clean.VentDelayTime");
            _pressureMaxTimeout = SC.GetValue<int>($"PM.{Module}.Clean.VentTimeout");
            _pmCleanTime = SC.GetValue<int>($"PM.{Module}.Clean.CleanTime");

            _EPV2OpenDelayTime = SC.GetValue<int>($"PM.{Module}.TimeDelayAlterEPV2Open");

            if (!_pmIoInterLock.SetPMLidCloseRoutineRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Clean,{reason}");
                return Result.FAIL;
            }

            //Purge Succeed 清掉
            _pmIoInterLock.DoLidOpenRoutineSucceed = false;
            //Clean Succeed 清掉
            _pmIoInterLock.DoLidCloseRoutineSucceed = false; 
            
            

            _checkPMPt2Over1000 = false;
            _checkPMPressureOver1000 = false;
            _finalOpen = false;

            currentPressureUpOrDown = PressureUpOrDown.None;
            _swTimer.Restart();
            Notify($"Start   LoopCount:{_pressureLoopCount}");
            return Result.RUN;
        }

        //PM Clean
        public override Result Monitor()
        {
            try
            {

                SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);
                SetRotationValve((int)RoutineStep.RotationEnable, _rotationCheckSpeed,false, _rotationCloseTimeout);

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


                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                CheckFinalIoStatue((int)RoutineStep.CheckFinal1Open);
                if (!_finalOpen)
                {
                    SetMfcByGroup((int)RoutineStep.SetM291519to38, MfcGroupName.M291519to38, 0, 5);
                    TimeDelay((int)RoutineStep.TimeDelay1, 5);
                }

                GetPressureConditionPT2((int)RoutineStep.CheckPMPt21000, 1000);
                if (!_checkPMPt2Over1000)
                {
                    //1.打开EPV1,等待10s
                    SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                    SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                    TimeDelay((int)RoutineStep.TimeDelay18, 1);
                    SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);
                    SetIoValueByGroup((int)RoutineStep.SetEPV1, IoGroupName.EPV1, true, _IoValueOpenCloseTimeout);
                    TimeDelay((int)RoutineStep.TimeDelay2, _EPV2OpenTimeDelay);
                }

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


                if (!_checkPMPt2Over1000)
                {
                    //判断反应腔压力是否超过1000mbar
                    GetPressureCondition((int)RoutineStep.CheckPM1000, 1000);
                    if (!_checkPMPressureOver1000)
                    {
                        //设置M28,M29,M31 为特定值
                        SetMfc28to31Special((int)RoutineStep.SetMfc28to31Special, _mfc28SpecialFlow, _mfc29SpecialFlow, _mfc31SpecialFlow, 1);
                    }

                    //等待反应腔压力到达1000
                    NotifyInfo((int)RoutineStep.Notify1, "Wait PM Pressure over 1000 mbar");
                    WaitChamberPressUpTo((int)RoutineStep.WaitPressUpTo1000, 1000, _waitChamberTo1000TimeOut);
                    SetMfcToDefaultByGroup((int)RoutineStep.SetMfc28to31Default, MfcGroupName.M28toM31, 0);
                }


                NotifyInfo((int)RoutineStep.Notify2, "Wait V27 open!");
                WaitV27Open((int)RoutineStep.WaitV27Open,1000);
                TimeDelay((int)RoutineStep.TimeDelay5, _pmCleanTime);

                if (_checkPMPt2Over1000)
                {
                    //1.打开EPV1,等待10s
                    SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                    SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                    SetIoValueByGroup((int)RoutineStep.SetEPV1, IoGroupName.EPV1, true, _IoValueOpenCloseTimeout);
                    TimeDelay((int)RoutineStep.TimeDelay2, _EPV2OpenTimeDelay);
                }

                //打开EPV2,等待9秒,打开蝶阀
                SetIoValueByGroup((int)RoutineStep.OpenEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay6, _EPV2OpenDelayTime);
                SetThrottleEnableAndWait((int)RoutineStep.SetTvOpen, _IoThrottle, 10);
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTvModeToPress, _IoThrottle, 10);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetTvTo1150, _IoThrottle, 1150);

                //打开V72 
                //SetIoValueByGroup((int)RoutineStep.VentPumpOpen, IoGroupName.VentPump, true, _IoValueOpenCloseTimeout);

                Loop((int)RoutineStep.StartLoop, _pressureLoopCount);
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown1, PressureUpOrDown.Dowing);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureDown, _IoThrottle, _pressureMin);
                WaitThrottleToPressureAndSetMfcSpecialForLidOpen((int)RoutineStep.WaitPressureDown, _IoThrottle, _pressureMin, _pmPressureMaxDiff, _pressureMinTimeout);                
                TimeDelay((int)RoutineStep.TimeDelay8, _pressureMinDelay);
               
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown2, PressureUpOrDown.Uping);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _pressureMax);
                WaitThrottleToPressureAndSetMfcSpecialForLidOpen((int)RoutineStep.WaitPressureUp, _IoThrottle, _pressureMax, _pmPressureMaxDiff, _pressureMaxTimeout);
                TimeDelay((int)RoutineStep.TimeDelay9, _pressureMaxDelay);
                EndLoop((int)RoutineStep.EndLoop);


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
            _pmIoInterLock.DoLidCloseRoutineRunning = false;
        }

        private void SetRoutineSuccessedDo()
        {
            _pmIoInterLock.SetLidClosedRoutineSucceed(true, out string reason);
            _pmIoInterLock.DoLidOpenRoutineSucceed = false;
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
