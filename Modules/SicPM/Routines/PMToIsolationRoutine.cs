using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;

namespace SicPM.Routines
{
    public class PMToIsolationRoutine : PMBaseRoutine
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
            SetM2toM40,
            SetPC,
            SetGroupI,      //V87-96
            SetM19to38,
            CloseFinal1,
            CloseFinal2,
            CloseEPV2,

            SetGasOut,
            SetTv,

            SetGasIn1,      //V65,V67,V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetM19to26,

            SetGasIn2,
            SetGroupJOpen,

            OpenFinal2,
            SetMfcFinal2,

            SetEPV1,
            SetEPV2,
            SetTVCloseMode,
            SetTvPositionToZero,
            SetMFCMode,
            SetPCMode,

            SetV76,
            SetV75,
            SetV31,
            SetV32,
            SetV35V36,
            SetGroupV25,
            SetV65,
            CloseV68,

            CloseM32M35M37,
            CloseM36,
            CloseM38,
            CloseGasIn1,
            CloseFinanl1,

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
            TimeDelayForEPV1,
        }

        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;

        private List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCloseTimeout = 20;   //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间

        public PMToIsolationRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            _pmModule = pm;
            Name = "Isolation";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            Notify("Start");
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
                SetRotationValve((int)RoutineStep.RotationEnable, 0, false, _rotationCloseTimeout);

                //关闭V72
                SetIoValueByGroup((int)RoutineStep.VentPumpClose, IoGroupName.VentPump, false, _IoValueOpenCloseTimeout);

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

                //打开V65
                SetIoValueByGroup((int)RoutineStep.SetV65, IoGroupName.V65, true, _IoValueOpenCloseTimeout);

                //打开D/G 阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupD, IoGroupName.D, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupG, IoGroupName.G, true, _IoValueOpenCloseTimeout);

                //关闭V68
                SetIoValueByGroup((int)RoutineStep.CloseV68, IoGroupName.V68, false, _IoValueOpenCloseTimeout);

                //设置MFC和PC的模式
                SetMfcModeToNormalByGroup((int)RoutineStep.SetMFCMode, MfcGroupName.All);
                SetPcModeToNormal((int)RoutineStep.SetPCMode, _lstPcList);

                //M1-M16设定为default值(M2、M9、M15除外)
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);

                //设置所有PC到默认值
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //M2、M9、M15、M19-M40 MFC 5s ramp到0
                SetMfcByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40, 0, 5);

                //关闭Final1阀
                SetIoValueByGroup((int)RoutineStep.CloseFinal1, IoGroupName.Final1, false, _IoValueOpenCloseTimeout);

                //关闭Final2
                SetIoValueByGroup((int)RoutineStep.CloseFinal2, IoGroupName.Final2, false, _IoValueOpenCloseTimeout);

                //关闭蝶阀
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay1, 1);

                //关闭EPV2
                SetIoValueByGroup((int)RoutineStep.CloseEPV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify("Isolation Finished");

            return Result.DONE;
        }

        public override void Abort()
        {
            PMDevice.SetRotationServo(0, 0);

            base.Abort();
        }

    }
}

