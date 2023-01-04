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
    public class PMToProcessIdleRoutine : PMBaseRoutine
    {
        private enum RoutineStep
        {
            ConfinementServoOn,
            RotationServoOn,
            RotationEnable,
            HeatEnable,
            VentPumpClose,
            ArSupply,

            OpenH2Valve,

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
            SetM2toM40,

            SetPC567Close,
            SetPC567Default,
            SetPC567Mode,

            SetGasOut,
            SetTv,

            SetM291519to38,
            SetGasIn1,      //V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetM291519to26,

            SetTVCloseMode,
            SetTvPositionToZero,
            CheckPM1000,

            SetGasIn2,
            SetGroupJOpen,

            CloseFinal2,
            OpenFinal2,
            SetMfcFinal2,
            WaitPmPressureUp,
            WaitPmPressureUp1020,

            SetTvOpen,
            SetTVEnable,
            SetTVPressMode,
            SetTV,
            SetTvMode,
            SetTVto1050,
            SetTVto300,
            WaitTvTo300,
            CheckPmVac,
            CheckPmAtm,
            CheckV72Open,

            SetV72,
            SetV25,
            SetV76,
            SetV75,
            SetV31,
            SetV32,
            SetV35V36,
            SetV65,

            SetEPV1,
            SetEPV2,
            SetEPV11,
            SetEPV22,
            SetTvModeToPress,
            SetMfc28to40Special,
            SetMfc28to40Default, 
            SetPressUpOrDown1, 
            SetPressureDown,
            EnableRotation,
            EnableHeater,
            SetRotation1,
            SetTC1Mode,
            SetTC1Ratio,
            SetTC1RatioTo0,
            SetTC1Ref,
            SetTC2Mode, 
            SetTC2Ratio,
            SetTC2RatioTo0,
            SetTC2Ref,
            EnableTC1,
            EnableTC2,
            CloseTC1,
            CloseTC2,
            SetFinal1ToDefault, 
            SetFinal1To0, 
            SetFinal2ToDefault, 
            SetFinal2To0,
            CheckFinal1Open,
            CheckFinal2Open,
            CheckTvOpen,
            SetRotation2,
            SetGroupV25,
            SetScrReset,
            CheckEpv2Open,
            CheckEpv1Open,

            SetV92V93V95,
            SetV94,
            SetV96,
            SetV68,
            SetM32M35M37,
            SetM36,
            SetM38,
            WaitPmPressureUp1,

            DelayV25,
            TimeDelay0,
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
            TimeDelay15,
            TimeDelay16,
            TimeDelay20,
            TimeDelay21,
            TimeDelay22,

            Notify1,
            Notify2
        }

        private IoThrottleValve2 _IoThrottle;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed=0; //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;   //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间
        private double _pmPressureMaxDiff;          //蝶阀与目标压力的差值范围(认为调整到位了)
        private int _throttleTimeout;               //蝶阀调整到指定压力的超时时间
        private double _throttleFinalPressure = 300;//蝶阀最终调整对的压力

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

        private int _rotationSpeed = 60;
        private int _routineTimeOut;

        private Stopwatch _swTimer = new Stopwatch();
        private IoInterLock _pmInterLock;
        public PMToProcessIdleRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();           
            Name = "ProcessIdle";
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
            _pmInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");

            if (!_pmInterLock.SetPMProcessIdleRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not run ProcessIdle, {reason}");
                return Result.FAIL;
            }

            if (!PMDevice.CheckHeadHeaterEnable(true))
            {
                EV.PostAlarmLog(Module, $"Should set HeatEnable on first!");
                return Result.FAIL;
            }

            //检查隔热罩状态
            if(PMDevice.ConfinementRing.RingServoError || PMDevice.ConfinementRing.RingIsBusy || 
                !PMDevice.ConfinementRing.RingServoOn)
            {
                EV.PostAlarmLog(Module, $"ConfinementRing State abnormal,please check");
                return Result.FAIL;
            }

            ////检查磁流体状态
            //if(!PMDevice._sicServo.ServoReady)
            //{
            //    EV.PostAlarmLog(Module, $"NAIS Servo State abnormal,please check");
            //    return Result.FAIL;
            //}

            _v72IsOpen = false;
            _checkPMPressureOver1000 = false;

            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");

            _rotationSpeed = SC.GetValue<int>($"PM.{Module}.ProcessIdle.RotationSpeed");
            _throttleFinalPressure = SC.GetValue<double>($"PM.{Module}.ProcessIdle.FinalPressure");

            _psuHeatEnable= SC.GetValue<bool>($"PM.{Module}.ProcessIdle.PSUHeaterEnable");
            _psuHeatMode = (float)SC.GetValue<int>($"PM.{Module}.ProcessIdle.PSUHeaterMode");
            _psuL1Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUInnerRatio");
            _psuL2Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUMiddleRatio");
            _psuL3Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUOuterRatio");


            _scrHeatEnable = SC.GetValue<bool>($"PM.{Module}.ProcessIdle.SCRHeaterEnable");
            _scrHeatMode = (float)SC.GetValue<int>($"PM.{Module}.ProcessIdle.SCRHeaterMode");
            _scrL1Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRUpperRatio");
            _scrL2Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRMiddleRatio");
            _scrL3Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRLowerRatio");

            _routineTimeOut= SC.GetValue<int>($"PM.{Module}.ProcessIdle.RoutineTimeOut");

            _finalOpen = false;
            _isTvOpen = false;

            currentPressureUpOrDown = PressureUpOrDown.None;
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

                //旋转停止
                //SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);
                SetRotationValve((int)RoutineStep.RotationEnable, _rotationCheckSpeed, false, _rotationCloseTimeout);

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

                //M1-M16设定为default值(M2、M9、M15除外)
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);

                //设置所有PC到默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                ////M2、M9、M15、M19-M40 MFC 5s ramp 到0
                //SetMfcByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40, 0, 5);

                //打开EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay1, 3);

                //Final2阀对应的MFC 3s Ramp到Default值
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfcFinal2, MfcGroupName.Final2, 3);

                //打开Final2阀
                SetIoValueByGroup((int)RoutineStep.OpenFinal2, IoGroupName.Final2, true, _IoValueOpenCloseTimeout);

                //打开V68
                SetIoValueByGroup((int)RoutineStep.SetV68, IoGroupName.GasIn1, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay2, 1);

                //打开V92,V93和V95,再打开V94,在打开V96
                SetMfcToDefaultByGroup((int)RoutineStep.SetM32M35M37, MfcGroupName.M32M35M37, 1);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM36, MfcGroupName.M36, 1);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM38, MfcGroupName.M38, 1);
                TimeDelay((int)RoutineStep.TimeDelay3, 3);
                SetIoValueByGroup((int)RoutineStep.SetV92V93V95, IoGroupName.V92V93V95, true, _IoValueOpenCloseTimeout);

                //打开J Valves
                SetIoValueByGroup((int)RoutineStep.SetGroupJOpen, IoGroupName.J, true, _IoValueOpenCloseTimeout);

                //设置M2,M9,M15,M19-M26 3s ramp 到default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM291519to26, MfcGroupName.M2toM26, 3);

                ////开V94,V96需等到腔体压力大于950
                //WaitPmPressureUPto((int)RoutineStep.WaitPmPressureUp1, 950, _throttleTimeout);

                SetIoValueByGroup((int)RoutineStep.SetV94, IoGroupName.V94, true, _IoValueOpenCloseTimeout);

                TimeDelay((int)RoutineStep.TimeDelay4, 3);
                SetIoValueByGroup((int)RoutineStep.SetV96, IoGroupName.V96, true, _IoValueOpenCloseTimeout);

                //设置蝶阀Enable
                SetThrottleEnableAndWait((int)RoutineStep.SetTVEnable, _IoThrottle, 5);

                //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode, _IoThrottle, 5);

                //伺服压力设定值到1050mbar
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetTV, _IoThrottle, 1050);

                //打开V72
                SetIoValueByGroup((int)RoutineStep.SetV72, IoGroupName.VentPump, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay5, 1);

                //关闭V25
                SetIoValueByGroup((int)RoutineStep.SetV25, IoGroupName.V25, false, _IoValueOpenCloseTimeout);

                //蝶阀调整到300
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown1, PressureUpOrDown.Dowing);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureDown, _IoThrottle, _throttleFinalPressure);
                WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.SetTVto300, _IoThrottle, _throttleFinalPressure, _pmPressureMaxDiff, _throttleTimeout);
                //TimeDelay((int)RoutineStep.TimeDelay8, 3);

                //转速Enable,加热Enable, 电流功率可配置
                EnableRotation((int)RoutineStep.EnableRotation, 5);
                SetRotationValveAndNoWait((int)RoutineStep.SetRotation1, _rotationSpeed);

                if (_psuHeatEnable)
                {
                    //打开PSU加热
                    SetPSUEnable((int)RoutineStep.EnableTC1, true, 5);
                    SetPSUHeatMode((int)RoutineStep.SetTC1Mode, _psuHeatMode);
                    TimeDelay((int)RoutineStep.TimeDelay6, 1);
                    SetPSUHeatRatio((int)RoutineStep.SetTC1Ratio, _psuL1Ratio, _psuL2Ratio, _psuL3Ratio);
                }
                else
                {
                    //关闭PSU加热
                    SetPSUHeatRatio((int)RoutineStep.SetTC1RatioTo0, 0, 0, 0);
                    SetPSUEnable((int)RoutineStep.CloseTC1, false, 5);
                }

                if (_scrHeatEnable)
                {
                    //打开SCR加热
                    SetSCRReset((int)RoutineStep.SetScrReset);
                    TimeDelay((int)RoutineStep.TimeDelay7, 1);

                    SetSCREnable((int)RoutineStep.EnableTC2, true, 5);
                    SetSCRHeatMode((int)RoutineStep.SetTC2Mode, _scrHeatMode);
                    TimeDelay((int)RoutineStep.TimeDelay8, 1);
                    SetSCRHeatRatio((int)RoutineStep.SetTC2Ratio, _scrL1Ratio, _scrL2Ratio, _scrL3Ratio);
                }
                else
                {
                    //关闭SCR加热
                    SetSCRHeatRatio((int)RoutineStep.SetTC2RatioTo0, 0, 0, 0);
                    SetSCREnable((int)RoutineStep.CloseTC2, false, 5);
                }

                SetRotationValve((int)RoutineStep.SetRotation2, _rotationSpeed, true, _rotationSpeed / 2);
            }

            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            _pmInterLock.SetPMProcessIdleRunning(false, out string reason);
            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _swTimer.Stop();

            return Result.DONE;
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

        public override void Abort()
        {
            _pmInterLock.SetPMProcessIdleRunning(false, out string reason);
            PMDevice._ioThrottleValve.StopRamp();
            PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
            PMDevice.SetHeaterStopRamp();
            PMDevice.SetRotationStopRamp();
            PMDevice.SetRotationServo(0, 0);

            base.Abort();
        }
    }
}