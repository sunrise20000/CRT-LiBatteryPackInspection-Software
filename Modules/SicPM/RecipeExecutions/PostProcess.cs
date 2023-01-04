using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using SicPM.Routines;

namespace SicPM.RecipeExecutions
{
    public class PostProcess : PMBaseRoutine
    {
        private enum RoutineStep
        {
            SetV31,
            SetV32,
            SetV35V36,
            SetGroupB,
            SetGroupC,
            SetGroupE,
            SetGroupF,
            SetGroupH,
            SetGroupK,
            SetGroupD,
            SetGroupG,
            SetV65,
            SetV68,
            SetM1to16,
            SetM2toM40,
            SetPC,
            SetEPV2,
            VentPumpClose,
            SetGroupV25,
            SetGroupJOpen,
            SetTVEnable,
            SetTVPressMode,
            SetPressUpOrDown,
            SetPressureUp,
            WaitPressureUp,
            EnableRotation,
            SetRotation1,
            SetRotation2,
            EnableTC1,
            SetTC1Mode,
            SetTC1Ratio,
            SetTC1Ratio1,
            CloseTC1,
            SetScrReset,
            EnableTC2,
            SetTC2Mode,
            SetTC2Ratio,
            SetTC2Ratio1,
            CloseTC2,

            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
        }

        private int _IoValueOpenCloseTimeout = 10;

        private int _routineTimeOut = 10;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private IoThrottleValve2 _IoThrottle;

        private double _pmPressureMaxDiff;
        private int _throttleTimeout;
        private double _pressureMax;

        private int _rotationSpeed = 60;

        private bool _psuHeatEnable = false;
        private float _psuHeatMode = 0;
        private float _psuL1Ratio = 0;
        private float _psuL2Ratio = 0;
        private float _psuL3Ratio = 0;

        private bool _scrHeatEnable = false;
        private float _scrHeatMode = 0;
        private float _scrL1Ratio = 0;
        private float _scrL2Ratio = 0;
        private float _scrL3Ratio = 0;

        private Stopwatch _swTimer = new Stopwatch();
        public PostProcess(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "PostProcess";
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                return Result.DONE;
            }

            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.ProcessIdle.RoutineTimeOut");

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");

            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");

            _pressureMax = SC.GetValue<double>($"PM.{Module}.ProcessIdle.FinalPressure");

            _rotationSpeed = SC.GetValue<int>($"PM.{Module}.ProcessIdle.RotationSpeed");

            _psuHeatEnable = SC.GetValue<bool>($"PM.{Module}.ProcessIdle.PSUHeaterEnable");
            _psuHeatMode = (float)SC.GetValue<int>($"PM.{Module}.ProcessIdle.PSUHeaterMode");
            _psuL1Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUInnerRatio");
            _psuL2Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUMiddleRatio");
            _psuL3Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.PSUOuterRatio");

            _scrHeatEnable = SC.GetValue<bool>($"PM.{Module}.ProcessIdle.SCRHeaterEnable");
            _scrHeatMode = (float)SC.GetValue<int>($"PM.{Module}.ProcessIdle.SCRHeaterMode");
            _scrL1Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRUpperRatio");
            _scrL2Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRMiddleRatio");
            _scrL3Ratio = (float)SC.GetValue<double>($"PM.{Module}.ProcessIdle.SCRLowerRatio");

            _swTimer.Restart();
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                CheckRoutineTimeOut();

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

                //打开V68
                SetIoValueByGroup((int)RoutineStep.SetV68, IoGroupName.GasIn1, true, _IoValueOpenCloseTimeout);

                //M2、M9、M15、M19-M40 MFC 30s ramp 到 default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40, 5);

                //M1 - M16 MFC 30s ramp 到 default 值(M2、M9、M15除外)
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 5);

                //所有PC设定为默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //打开EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);

                //打开J Valves
                SetIoValueByGroup((int)RoutineStep.SetGroupJOpen, IoGroupName.J, true, _IoValueOpenCloseTimeout);

                //打开V72,关闭V25
                SetIoValueByGroup((int)RoutineStep.VentPumpClose, IoGroupName.VentPump, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupV25, IoGroupName.V25, false, _IoValueOpenCloseTimeout);

                //设置蝶阀Enable
                SetThrottleEnableAndWait((int)RoutineStep.SetTVEnable, _IoThrottle, 5);

                //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode, _IoThrottle, 5);

                //使用动态流量伺服到300mbar            
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown, PressureUpOrDown.Uping);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _pressureMax);
                WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.WaitPressureUp, _IoThrottle, _pressureMax, _pmPressureMaxDiff, _throttleTimeout);

                //转速Enable,加热Enable, 电流功率可配置
                EnableRotation((int)RoutineStep.EnableRotation, 5);
                SetRotationValveAndNoWait((int)RoutineStep.SetRotation1, _rotationSpeed);

                if (_psuHeatEnable)
                {
                    //打开PSU加热
                    SetPSUEnable((int)RoutineStep.EnableTC1, true, 5);
                    SetPSUHeatMode((int)RoutineStep.SetTC1Mode, _psuHeatMode);
                    TimeDelay((int)RoutineStep.TimeDelay1, 1);
                    SetPSUHeatRatio((int)RoutineStep.SetTC1Ratio, _psuL1Ratio, _psuL2Ratio, _psuL3Ratio);
                }
                else
                {
                    //关闭PSU加热
                    SetPSUHeatRatio((int)RoutineStep.SetTC1Ratio1, 0, 0, 0);
                    SetPSUEnable((int)RoutineStep.CloseTC1, false, 5);
                }

                if (_scrHeatEnable)
                {
                    //打开SCR加热
                    SetSCRReset((int)RoutineStep.SetScrReset);
                    TimeDelay((int)RoutineStep.TimeDelay2, 1);

                    SetSCREnable((int)RoutineStep.EnableTC2, true, 5);
                    SetSCRHeatMode((int)RoutineStep.SetTC2Mode, _scrHeatMode);
                    TimeDelay((int)RoutineStep.TimeDelay3, 1);
                    SetSCRHeatRatio((int)RoutineStep.SetTC2Ratio, _scrL1Ratio, _scrL2Ratio, _scrL3Ratio);
                }
                else
                {
                    //关闭SCR加热
                    SetSCRHeatRatio((int)RoutineStep.SetTC1Ratio1, 0, 0, 0);
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
                    EV.PostAlarmLog(Module, $"Routine TimeOut! over {_routineTimeOut} s");
                    throw (new RoutineFaildException());
                }
            }
        }

        public override void Abort()
        {
            PMDevice._ioThrottleValve.StopRamp();
            PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
            PMDevice.SetHeaterStopRamp();
            PMDevice.SetRotationStopRamp();

            base.Abort();
        }
    }
}
