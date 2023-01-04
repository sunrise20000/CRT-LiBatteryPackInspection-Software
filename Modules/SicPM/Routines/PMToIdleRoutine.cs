using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMToIdleRoutine : PMBaseRoutine
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
            SetM291519to38,
            SetGroupJ,
            SetM19to38,

            SetGasOut,
            SetTv,

            SetGasIn1,      //V65,V67,V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetMfc19to26,

            SetTVCloseMode,
            SetTvPositionToZero,
            CheckPM1000,

            SetGasIn2,
            SetGroupJOpen,
            SetM291519to26,
            SetGroupV38,

            OpenFinal2,
            SetMfcFinal2,
            WaitPmPressureUp,

            SetEPV1,
            SetEPV2,
            SetTvMode,
            SetEPVV1,
            SetMfc28to31Special,
            SetMfc28to31Default,


            SetPC567Close,
            SetPC567Mode,
            SetPC567Default,
            CheckFinal1Open,

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
            TimeDelay18,
            Notify1,
        }

        private IoThrottleValve2 _IoThrottle;

        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed=0; //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;   //旋转停止超时
        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间

        private int _EPV2OpenTimeDelay = 10;
        private int _waitPMPressureUpTimeout = 300;     //等待反应腔压力到达1000的超时时间

        private double _mfc28SpecialFlow = 15000;
        private double _mfc29SpecialFlow = 5000;
        private double _mfc31SpecialFlow = 10000;

        private Stopwatch _swTimer = new Stopwatch();
        public PMToIdleRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "AtmIdle";

            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
            _checkPMPressureOver1000 = false;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _checkPMPressureOver1000 = false;
            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _EPV2OpenTimeDelay = SC.GetValue<int>($"PM.{Module}.EPV2OpenTimeDelayAlterEPV1Open");

            _mfc28SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc28FlowSpecail");
            _mfc29SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc29FlowSpecail");
            _mfc31SpecialFlow = SC.GetValue<double>($"PM.{Module}.Mfc31FlowSpecail");

            currentPressureUpOrDown = PressureUpOrDown.None;
            _swTimer.Restart();
            _finalOpen = false;

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                {
                    SetHeatEnable((int)RoutineStep.HeatEnable, true, _heatTimeOut);
                    SetIoValueByGroup((int)RoutineStep.SetEPV1, IoGroupName.EPV1, true, _IoValueOpenCloseTimeout);
                    return Result.DONE;
                }

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

                SetMfcToDefaultByGroup((int)RoutineStep.SetM1to16, MfcGroupName.M1to16, 0);
                SetPcToDefault((int)RoutineStep.SetPC, _lstPcList);

                CheckFinalIoStatue((int)RoutineStep.CheckFinal1Open);
                if (!_finalOpen)
                {
                    SetMfcByGroup((int)RoutineStep.SetM291519to38, MfcGroupName.M291519to38, 0, 5);
                    TimeDelay((int)RoutineStep.TimeDelay1, 5);
                }


                //1.打开抽气系统 （关闭EPV2,打开EPV1）
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.SetTvPositionToZero, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay18, 1);
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
                

                //5.打开J Valves
                SetIoValueByGroup((int)RoutineStep.SetGroupJOpen, IoGroupName.J, true, _IoValueOpenCloseTimeout);
                SetMfcToDefaultByGroup((int)RoutineStep.SetM291519to26, MfcGroupName.M291519to26, 3);
                TimeDelay((int)RoutineStep.TimeDelay5, 3);
                

                //判断反应腔压力是否超过1000mbar
                GetPressureCondition((int)RoutineStep.CheckPM1000,1000);
                if (!_checkPMPressureOver1000)
                {
                    //设置M28,M29,M31 为特定值
                    SetMfc28to31Special((int)RoutineStep.SetMfc28to31Special, _mfc28SpecialFlow, _mfc29SpecialFlow, _mfc31SpecialFlow, 1);
                }

                //等待反应腔压力到达1000
                NotifyInfo((int)RoutineStep.Notify1, "Wait pm pressure to 1000 mbar!");
                WaitPmPressureUPto((int)RoutineStep.WaitPmPressureUp, 1000, _waitPMPressureUpTimeout); //等待反应腔压力大于1000 
                SetMfcToDefaultByGroup((int)RoutineStep.SetMfc28to31Default, MfcGroupName.M28toM31, 0);


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
            PMDevice._ioThrottleValve.StopRamp();
            PMDevice.SetMfcStopRamp(PMDevice.GetMfcListByGroupName(MfcGroupName.All));
            base.Abort();
        }

    }
}
