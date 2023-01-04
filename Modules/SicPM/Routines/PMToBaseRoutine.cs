using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;

namespace SicPM.Routines
{
    public class PMToBaseRoutine : PMBaseRoutine
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
            SetGroupV25,

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
            TimeDelayForEPV1,
        }

        private PMModule _pmModule;
        private IoThrottleValve2 _IoThrottle;

        private List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCloseTimeout = 20;   //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间
        private double _timeDelayForEPV1=10; // 关闭EPV1前的等待时间

        public PMToBaseRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            _pmModule = pm;
            Name = "Isolation";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (_pmModule.PT1.FeedBack < 1000)
            {
                Notify("Chamber Pressure Is Lower Than 1000 mbar,Can not Close EPV1!");
                return Result.FAIL;
            }

            //_timeDelayForEPV1 = SC.GetValue<int>($"PM.{Module}.Isolation.TimeDelayForEPV1");

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                SetHeatEnable((int)RoutineStep.HeatEnable, false, _heatTimeOut);
                SetRotationValve((int)RoutineStep.RotationEnable, 0, false, _rotationCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetGroupB, IoGroupName.B, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupC, IoGroupName.C, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupE, IoGroupName.E, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupF, IoGroupName.F, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupH, IoGroupName.H, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupK, IoGroupName.K, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupV25, IoGroupName.V25, true, _IoValueOpenCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetGroupD, IoGroupName.D, true, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupG, IoGroupName.G, true, _IoValueOpenCloseTimeout);

                SetIoValueByGroup((int)RoutineStep.SetV76, IoGroupName.V76, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetV75, IoGroupName.V75, true, _IoValueOpenCloseTimeout);


                SetIoValueByGroup((int)RoutineStep.VentPumpClose, IoGroupName.VentPump, false, _IoValueOpenCloseTimeout);

                //MFC设置模式,M1-16设置为默认值，PC设置为默认值
                SetMfcModeToNormalByGroup((int)RoutineStep.SetMFCMode, MfcGroupName.All);
                SetPcModeToNormal((int)RoutineStep.SetPCMode, _lstPcList);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                //关闭V91到V96
                SetIoValueByGroup((int)RoutineStep.OpenFinal1, IoGroupName.Final1, false, _IoValueOpenCloseTimeout);
                SetMfcByGroup((int)RoutineStep.SetMfcFinal1, MfcGroupName.Final1,0,5);
                TimeDelay((int)RoutineStep.TimeDelay1, 5);
                //关闭V87到90
                SetIoValueByGroup((int)RoutineStep.OpenFinal2, IoGroupName.Final2, false, _IoValueOpenCloseTimeout);
                SetMfcByGroup((int)RoutineStep.SetM19to26, MfcGroupName.M19toM31, 0,5);
                TimeDelay((int)RoutineStep.TimeDelay2, 5);

                //关闭先关EPV2，再关EPV1
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle,8);
                TimeDelay((int)RoutineStep.TimeDelay18, 1);
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelayForEPV1, _timeDelayForEPV1);
                SetIoValueByGroup((int)RoutineStep.SetEPV1, IoGroupName.EPV1, false, _IoValueOpenCloseTimeout);

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

        }

    }
}

