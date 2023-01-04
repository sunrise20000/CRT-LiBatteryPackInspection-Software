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
    public class PMVentRoutine : PMBaseRoutine
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
            SetM2toM40,
            SetPC,
            SetM291519to38,
            SetGroupJ,
            SetM19to38,

            SetGasOut,
            SetTv,

            SetGasIn1,      //V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetMfc19to26,

            SetTVCloseMode,
            SetTvPositionToZero,
            CheckPM1000,

            SetGasIn2,
            SetGroupJOpen,
            SetM291519to26,

            OpenFinal2,
            SetMfcFinal2,
            WaitPmPressureUp,
            WaitPmPressureUp1,

            SetEPV1,
            SetEPV2,
            SetTvMode,
            SetEPVV1,
            SetMfc28to40Special,
            SetMfc28to40Default,


            SetPC567Close,
            SetPC567Mode,
            SetPC567Default,
            CheckFinal1Open,

            SetV76,
            SetV75,
            SetV31,
            SetV32,
            SetV35V36,
            SetGroupV25,

            SetV92V93V95,
            SetV94,
            SetV96,
            SetV68,
            SetM32M35M37,
            SetM36,
            SetM38,

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
            TimeDelay20,
            TimeDelay21,
            TimeDelay22,
            Notify1,
        }

        private IoThrottleValve2 _IoThrottle;
        private IoInterLock _pmIoInterLock;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed = 0; //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;   //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间

        private int _waitPMPressureUpTimeout = 300;     //等待反应腔压力到达1000的超时时间

        private int _routineTimeOut;
        private double _ventBasePressure;

        private Stopwatch _swTimer = new Stopwatch();
        public PMVentRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "Vent";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");

            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.Vent.RoutineTimeOut");
            _ventBasePressure= SC.GetValue<double>($"PM.{Module}.Vent.VentBasePressure");

            _waitPMPressureUpTimeout = SC.GetValue<int>($"PM.{Module}.Vent.VentTimeout");

            currentPressureUpOrDown = PressureUpOrDown.None;
            _swTimer.Restart();
            _finalOpen = false;

            if (!_pmIoInterLock.SetPMVentRoutineRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Vent,{reason}");
                return Result.FAIL;
            }

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

                //设置所有PC到默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //M2、M9、M15、M19-M40 MFC 3s ramp 到默认值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40, 3);

                //关闭蝶阀
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay1, 1);

                //关闭EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);

                //打开A 组阀
                SetIoValueByGroup((int)RoutineStep.SetGroupA, IoGroupName.A, true, _IoValueOpenCloseTimeout);

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

                //开V94,V96需等到腔体压力大于950
                WaitPmPressureUPto((int)RoutineStep.WaitPmPressureUp1, 950, _waitPMPressureUpTimeout);

                SetIoValueByGroup((int)RoutineStep.SetV94, IoGroupName.V94, true, _IoValueOpenCloseTimeout);

                TimeDelay((int)RoutineStep.TimeDelay4, 3);
                SetIoValueByGroup((int)RoutineStep.SetV96, IoGroupName.V96, true, _IoValueOpenCloseTimeout);

                //等待反应腔压力到达1020
                NotifyInfo((int)RoutineStep.Notify1, $"Wait pm pressure to {_ventBasePressure} mbar!");
                WaitPmPressureUPto((int)RoutineStep.WaitPmPressureUp, _ventBasePressure, _waitPMPressureUpTimeout); //等待反应腔压力大于1000 

                _pmIoInterLock.SetPMVentRoutineRunning(false, out _);

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
            _pmIoInterLock.SetPMVentRoutineRunning(false, out _);
            PMDevice._ioThrottleValve.StopRamp();
            PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
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