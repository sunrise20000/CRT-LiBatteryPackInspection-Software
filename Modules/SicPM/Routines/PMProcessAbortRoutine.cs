using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMProcessAbortRoutine : PMBaseRoutine
    {
        private enum RoutineStep
        {
            HeatEnable,
            SetRotation,
            SetM2toM40,
            SetM1to16,
            WaitForMfcRamp,
            SetPC,
            SetV32,
            SetV35V36,
            SetV68,
            SetV65,
            SetGroupB,
            SetGroupC,
            SetGroupE,
            SetGroupF,
            SetGroupH,
            SetGroupK,
            SetGroupD,
            SetGroupG,
        }

        private int _heatTimeOut = 5;

        private int _mfc1to16RampTime = 30;
        private int _mfc2to40RampTime = 30;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _IoValueOpenCloseTimeout = 10; 
                 
        private int _routineTimeOut = 10;

        private Stopwatch _swTimer = new Stopwatch();
        public PMProcessAbortRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "ProcessAbort";
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _mfc1to16RampTime = SC.GetValue<int>($"PM.{Module}.ProcessIdle.MFC1to16RampTime");
            _mfc2to40RampTime = SC.GetValue<int>($"PM.{Module}.ProcessIdle.MFC19to40RampTime");

            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.ProcessIdle.RoutineTimeOut");

            _swTimer.Restart();
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //CheckRoutineTimeOut();

                //停止加热
                SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);

                //停止旋转
                SetRotationValveAndNoWait((int)RoutineStep.SetRotation, 0);

                //M2、M9、M15、M19-M40 MFC 30s ramp 到 default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40, _mfc2to40RampTime);

                //M1 - M16 MFC 30s ramp 到 default 值(M2、M9、M15除外)
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, _mfc1to16RampTime);

                // 等待MFC Ramp
                TimeDelay((int)RoutineStep.WaitForMfcRamp, Math.Max(_mfc2to40RampTime, _mfc1to16RampTime));
                
                //所有PC设定为默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //打开V32
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