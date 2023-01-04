using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using SicPM.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMLeakCheckRoutine : PMBaseRoutine
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

            SetGasIn1,      //V68
            OpenFinal1,     //V91-96
            SetMfcFinal1,   //M32-38
            SetMfc19to26,

            SetTVCloseMode,
            SetTvPositionToZero,

            SetGasIn2,
            SetGroupJOpen,
            SetM291519to26,

            OpenFinal2,
            CloseFinal2,
            SetMfcFinal2,

            SetEPV1,
            SetEPV2,
            SetTvMode,
            SetTVEnable,
            SetTVPressMode,
            SetTv1,
            WaitTv1,
            SetM2toM40,
            SetTVPositionMode,
            SetTVPositionMode_1,
            SetTVPositionMax,
            SetTVPositionMax_1,
            SetTVPressMode1,
            SetM2TOM40Default,
            SetM2toM40_1,

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
            SetMfc28to40Default,

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
            DoLeakCheck,
            CalcLeakCheck, 
            SetTVClose, 
            CloseTV,
            CloseV27,
            CloseV28,
            CloseV69,
            CloseV70,
            SetGroupV25,
            SetV31,
            SetV32,
            SetV35V36,

            SetV92V93V95,
            SetV94,
            SetV96,
            SetV68,
            SetM32M35M37,
            SetM36,
            SetM38,

            CloseM32M35M37,
            CloseM36,
            CloseM38,
            CloseGasIn1,


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
            TimeDelay17,
        }

        enum LeakCheckType
        {
            ChamberOnly,

            ChamberAndGasline,

            ChamberAndGaslineToFacility,
        }

        private ModuleName moduleName;

        private IoThrottleValve2 _IoThrottle;
        private IoInterLock _pmIoInterLock;


        List<int> _lstPcList = new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private int _rotationCheckSpeed = 0; //设置旋转速度为0后检查是否转速低于此数值
        private int _rotationCloseTimeout;   //旋转停止超时
        private int _heatTimeOut = 5;             //Heat关闭等待Di反馈超时时间

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

        private int _paramContinuePumpTime;
        private int _paramLeakCheckTime ;
        private double _beginPressure;
        private double _beginTime;
        private int _calTimes;              //第几次计算LeakCheck的值（每60秒计算一次）
        private double _leakSpec;
        private int _routineTimeOut;
        //private int _EPV2OpenDelayTime = 9;

        private bool _useSettingValue = false;

        private Stopwatch _swTimer = new Stopwatch();
        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }

        public PMLeakCheckRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "Leak Check";


            _IoThrottle = DEVICE.GetDevice<IoThrottleValve2>($"{Module}.TV");
        }

        internal void Init(int pumpTime, int leakCheckTime)
        {
            _paramContinuePumpTime = pumpTime;
            _paramLeakCheckTime = leakCheckTime;
            _useSettingValue = true;
        }

        internal void Init()
        {
            _useSettingValue = false;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _swTimer.Restart();


            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");
            _pmPressureMaxDiff = SC.GetValue<double>($"PM.{Module}.ThrottlePressureMaxDiff");
            _throttleTimeout = SC.GetValue<int>($"PM.{Module}.ThrottlePressureTimeout");
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
            _rotationCloseTimeout = SC.GetValue<int>($"PM.{Module}.RotationCloseTimeout");

            _pressureLoopCount = SC.GetValue<int>($"PM.{Module}.LeakCheck.CyclePurgeCount");
            _pressureMin = SC.GetValue<double>($"PM.{Module}.LeakCheck.PumpBasePressure");
            _pressureMinDelay = SC.GetValue<int>($"PM.{Module}.LeakCheck.PumpDelayTime");
            _pressureMinTimeout = SC.GetValue<int>($"PM.{Module}.LeakCheck.PumpTimeout");
            _pressureMax = SC.GetValue<double>($"PM.{Module}.LeakCheck.VentBasePressure");
            _pressureMaxDelay = SC.GetValue<int>($"PM.{Module}.LeakCheck.VentDelayTime");
            _pressureMaxTimeout = SC.GetValue<int>($"PM.{Module}.LeakCheck.VentTimeout");             
            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.LeakCheck.RoutineTimeOut");
            //_EPV2OpenDelayTime = SC.GetValue<int>($"PM.{Module}.TimeDelayAlterEPV2Open");

            if (!_useSettingValue)
            {
                _paramContinuePumpTime = SC.GetValue<int>($"PM.{Module}.LeakCheck.ContinuePumpTime");
                _paramLeakCheckTime = SC.GetValue<int>($"PM.{Module}.LeakCheck.LeakCheckDelayTime");
            }

            _leakSpec = SC.GetValue<double>($"PM.{Module}.LeakCheck.LeakSpec");

            if (!PMDevice.SetIOValueByGroup(IoGroupName.V70, false))
            {
                EV.PostAlarmLog(Module,"Can not close V70-1");
                return Result.FAIL;
            }

            Notify($"Start   LoopCount:{_pressureLoopCount}");
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

                //设置M2,M9,M15,M19-M26 3s ramp 到default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM291519to26, MfcGroupName.M2toM26, 3);

                //打开V68
                SetIoValueByGroup((int)RoutineStep.SetV68, IoGroupName.GasIn1, true, _IoValueOpenCloseTimeout);

                //打开J阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupJ, IoGroupName.J, true, _IoValueOpenCloseTimeout);

                //打开EPV2
                SetIoValueByGroup((int)RoutineStep.SetEPV2, IoGroupName.EPV2, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay1, 3);

                //设置蝶阀Enable
                SetThrottleEnableAndWait((int)RoutineStep.SetTVEnable, _IoThrottle, 5);

                //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode, _IoThrottle, 5);

                //伺服压力设定值到0mbar
                SetThrottlePressureAndWaitSetPoint((int)RoutineStep.SetTv1, _IoThrottle, _pressureMin, _pmPressureMaxDiff, _throttleTimeout);

                //M2、M9、M15、M19-M40 MFC 3s ramp到0
                SetMfcByGroup((int)RoutineStep.SetM2toM40, MfcGroupName.M2toM40, 0, 3);

                //等待腔体压力Pump到设定值
                WaitChamberPressDownTo((int)RoutineStep.WaitTv1, _pressureMin, _pmPressureMaxDiff, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay8, 3);

                //蝶阀设置为位置模式,开度设置为最大
                SetThrottleToPositionMode((int)RoutineStep.SetTVPositionMode, _IoThrottle, _throttleTimeout);
                SetThrottleSetPosition((int)RoutineStep.SetTVPositionMax, _IoThrottle, 100, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay9,3);

                //循环开始
                Loop((int)RoutineStep.StartLoop, _pressureLoopCount);

                //M2、M9、M15、M19-M40 MFC 3s ramp 到 default 值
                SetMfcToDefaultByGroup((int)RoutineStep.SetM2TOM40Default, MfcGroupName.M2toM40, 3);

                //设置蝶阀为压力模式
                SetThrottleToPressModeAndWait((int)RoutineStep.SetTVPressMode1, _IoThrottle, 5);

                //使用动态流量伺服到300mbar            
                SetPressureUpOrDown((int)RoutineStep.SetPressUpOrDown1, PressureUpOrDown.Uping);
                SetThrottleToTargetAndNoWait((int)RoutineStep.SetPressureUp, _IoThrottle, _pressureMax);
                WaitThrottleToPressureAndSetMfcSpecial((int)RoutineStep.WaitPressureUp, _IoThrottle, _pressureMax, _pmPressureMaxDiff, _pressureMaxTimeout);
                TimeDelay((int)RoutineStep.TimeDelay10, _pressureMaxDelay);

                //M2、M9、M15、M19-M40 MFC 3s ramp 0
                SetMfcByGroup((int)RoutineStep.SetM2toM40_1, MfcGroupName.M2toM40, 0, 3);

                //伺服到0mbar 
                SetThrottlePressureAndWait((int)RoutineStep.SetPressureDown, _IoThrottle, _pressureMin, _pmPressureMaxDiff, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay11,3);

                //蝶阀设置为位置模式,开度设置为最大
                SetThrottleToPositionMode((int)RoutineStep.SetTVPositionMode_1, _IoThrottle, _throttleTimeout);
                SetThrottleSetPositionNoWait((int)RoutineStep.SetTVPositionMax_1, _IoThrottle, 100, _throttleTimeout);

                TimeDelay((int)RoutineStep.TimeDelay12, _pressureMinDelay);

                EndLoop((int)RoutineStep.EndLoop);

                //依次关闭Final1
                SetIoValueByGroup((int)RoutineStep.CloseGasIn1, IoGroupName.GasIn1, false, _IoValueOpenCloseTimeout);
                Delay((int)RoutineStep.TimeDelay13, 5);

                //关闭M32,M35,M36,M37,M38
                SetMfcByGroup((int)RoutineStep.CloseM32M35M37, MfcGroupName.M32M35M37, 0, 5);
                SetMfcByGroup((int)RoutineStep.CloseM36, MfcGroupName.M36, 0, 5);
                SetMfcByGroup((int)RoutineStep.CloseM38, MfcGroupName.M38, 0, 5);

                Delay((int)RoutineStep.TimeDelay14, 5);

                //关闭Final1阀
                SetIoValueByGroup((int)RoutineStep.CloseFinanl1, IoGroupName.Final1, false, _IoValueOpenCloseTimeout);

                //关闭Final2
                SetIoValueByGroup((int)RoutineStep.CloseFinal2, IoGroupName.Final2, false, _IoValueOpenCloseTimeout);

                //延时
                TimeDelay((int)RoutineStep.TimeDelay15, _paramContinuePumpTime);

                //先关蝶阀,再关闭EPV2
                SetThrottleToCloseMode((int)RoutineStep.SetTVCloseMode2, _IoThrottle, 8);
                SetThrottleDisable((int)RoutineStep.CLoseTvvvv, _IoThrottle, 8);
                TimeDelay((int)RoutineStep.TimeDelay16, 1);
                SetIoValueByGroup((int)RoutineStep.SetEPVVV2, IoGroupName.EPV2, false, _IoValueOpenCloseTimeout);

                //计算漏率
                DoLeakCheck((int)RoutineStep.DoLeakCheck, _paramLeakCheckTime);
                TimeDelay((int)RoutineStep.TimeDelay17, _paramLeakCheckTime);
                CalcLeakCheck((int)RoutineStep.CalcLeakCheck, _paramLeakCheckTime);
                //CalcLeackCheckPerMinute(_paramLeakCheckTime);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            _swTimer.Stop();
            return Result.DONE;
        }

        public override void Abort()
        {
            Stop($"{Module} leak check aborted");
            _swTimer.Stop();
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

        public void DoLeakCheck(int id, double time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Keep pressure for {time} seconds");

                _beginTime = _swTimer.ElapsedMilliseconds;
                _beginPressure = PMDevice.ChamberPressure;
                _calTimes = 1;
                return true;
            });
            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CalcLeackCheckPerMinute(int time)
        {
            //大于LeakCheck时间,
            if ((int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000) >= time)
            {
                int leakSecond = (int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000);
                double endPressure = PMDevice.ChamberPressure;
                double leakSpan = endPressure - _beginPressure;
                double leakRate = (endPressure - _beginPressure) / (leakSecond / 60.0);
                if (leakRate > _leakSpec)
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.FAIL.ToString(), "", Module);
                    EV.PostInfoLog(Module, $"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    
                }
                else
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.Succeed.ToString(), "", Module);
                    EV.PostInfoLog(Module, $"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    
                }
            }
            else 
            {
                if ((int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000) >= 60 * _calTimes)
                {
                    _calTimes++;
                    int leakSecond = (int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000);
                    double endPressure = PMDevice.ChamberPressure;
                    double leakSpan = endPressure - _beginPressure;
                    double leakRate = (endPressure - _beginPressure) / (leakSecond / 60.0);
                    if (leakRate > _leakSpec)
                    {
                        EV.PostInfoLog(Module, $"Leak check Failed Count {_calTimes - 1}: using {leakSecond} seconds, leak rate: {leakRate:F2} ,Rate over {_leakSpec}");

                        throw (new RoutineFaildException());
                    }
                    EV.PostInfoLog(Module, $"Leak check Count {_calTimes - 1}: using {leakSecond} seconds, leak rate: {leakRate:F2}");                    
                }
                throw (new RoutineBreakException());
            }
        }


        public void CalcLeakCheck(int id, int time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                double endPressure = PMDevice.ChamberPressure;
                double leakSpan = endPressure - _beginPressure;
                double leakRate = (endPressure - _beginPressure) / (time / 60.0);
                if (leakRate > _leakSpec)
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.FAIL.ToString(), "", Module);

                    EV.PostInfoLog(Module,$"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    return false;
                }
                else
                {                    
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.Succeed.ToString(), "", Module);

                    EV.PostInfoLog(Module,$"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    return true;
                }
            });
        }
    }
}
