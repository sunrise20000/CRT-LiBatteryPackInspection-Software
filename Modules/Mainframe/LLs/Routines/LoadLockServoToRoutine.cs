using System;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadLockServoToRoutine : LoadLockBaseRoutine
    {
        enum RoutineStep
        {
            CheckForelinePressure,
            RequestPump,
            RequestPumpDelay,
            SlowPump,
            CloseSlowValve,
            FastPump,
            PumpDelay,
            CloseFastValve,
            Delay,
            SlowVent,
            CloseSlowVentValve,
            TimeDelay1,
            TimeDelay2,
        }

        private double _pumpSwitchPressure;

        private int _slowPumpTimeout = 100;
        private int _fastPumpTimeout = 100;
        private int _slowVentTimeout = 100;

        private bool _useSettingValue;
        private double _targetPressure;
        private bool _needPump = false;

        public LoadLockServoToRoutine(ModuleName module)
        {
            Module = module.ToString();
            Name = "LoadLockServoTo";
        }

        public void Init(double pressure)
        {
            _targetPressure = pressure;
            _useSettingValue = true;
        }


        public override Result Start(params object[] objs)
        {
            Reset();

            if (!_useSettingValue)
            {
                _targetPressure = SC.GetConfigItem("TM.PressureBalance.BalancePressure").DoubleValue;
            }

            _pumpSwitchPressure = SC.GetValue<double>("LoadLock.Pump.SlowFastPumpSwitchPressure");

            _slowVentTimeout = SC.GetValue<int>("LoadLock.Vent.RoutineTimeOut");

            string reason;

            if (!LoadLockDevice.SetFastVentValve(false, out reason) ||
                !LoadLockDevice.SetSlowVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            if (!LoadLockDevice.SetFastPumpValve(false, out reason) ||
                !LoadLockDevice.SetSlowPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            bool isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            if (isAtmMode)
            {
                EV.PostInfoLog(Module, $"system in atm mode, {Module} pump skipped");
                return Result.DONE;
            }

            if (!LoadLockDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"Can not pump, door is open");
                return Result.FAIL;
            }

            if (!TMDevice.CheckSlitValveClose(ModuleHelper.Converter(Module)))
            {
                EV.PostAlarmLog(Module, $"LoadLock Can not servo to pressure, slit valve is open");
                return Result.FAIL;
            }


            if (Math.Abs(_targetPressure - LoadLockDevice.ChamberPressure) <
                SC.GetConfigItem("TM.PressureBalance.BalanceMaxDiffPressure").DoubleValue &&
                LoadLockDevice.CheckVacuum())
            {
                return Result.DONE;
            }

            _needPump = LoadLockDevice.ChamberPressure > _targetPressure;

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                TimeDelay((int)RoutineStep.TimeDelay1, 1);

                if (_needPump)
                {
                    //需要调节的压力低于打开快抽的压力
                    if (_targetPressure < _pumpSwitchPressure)
                    {
                        SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _slowPumpTimeout);

                        FastPump((int)RoutineStep.FastPump, _targetPressure, _fastPumpTimeout);

                        TimeDelay((int)RoutineStep.TimeDelay2, 5);

                        CloseFastPumpValve((int)RoutineStep.CloseFastValve);

                        CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);
                    }
                    else
                    {
                        SlowPump((int)RoutineStep.SlowPump, _targetPressure, _slowPumpTimeout);

                        CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);
                    }
                }
                else
                {
                    SlowVent((int)RoutineStep.SlowVent, _targetPressure, _slowVentTimeout);

                    CloseVentValve((int)RoutineStep.CloseSlowVentValve);
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

            Notify("Finished");

            return Result.DONE;
        }
    }
}
