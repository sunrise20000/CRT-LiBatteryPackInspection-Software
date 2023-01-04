using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadLockPrepareTransferRoutine : LoadLockBaseRoutine
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
            TimeDelay3,
            StartLoop,
            EndLoop,
            ClosePumpValveDelay
        }

        private double _pumpSwitchPressure = 100;

        private int _slowPumpTimeout = 100;
        private int _fastPumpTimeout = 100;
        private int _slowVentTimeout = 100;

        private double _targetPressure;
        private double _llVacPressure = 0;
        private double _balancePressureDiff = 0;
        private double _pumpBasePressure = 0;

        private int _purgeCount;
        private int _pumpDelayTime;

        public LoadLockPrepareTransferRoutine(ModuleName module)
        {
            Module = module.ToString();
            Name = "PrepareTransfer";
        }


        public override Result Start(params object[] objs)
        {
            Reset();
            _targetPressure = SC.GetValue<double>("TM.PressureBalance.BalancePressure");
            _llVacPressure = SC.GetValue<double>("LoadLock.VacuumPressureBase");

            _balancePressureDiff = SC.GetValue<double>("TM.PressureBalance.BalanceMaxDiffPressure"); 
            _pumpSwitchPressure = SC.GetValue<double>("LoadLock.Pump.SlowFastPumpSwitchPressure");

            _pumpBasePressure = SC.GetValue<double>("LoadLock.Purge.PumpBasePressure");
            _slowPumpTimeout = SC.GetValue<int>("LoadLock.Pump.SlowPumpTimeout");
            _fastPumpTimeout = SC.GetValue<int>("LoadLock.Pump.FastPumpTimeout");
            _slowVentTimeout = SC.GetValue<int>("LoadLock.Vent.SlowVentTimeout");

            //_purgeCount = SC.GetValue<int>("LoadLock.Purge.CyclePurgeCount");
            _pumpDelayTime = SC.GetValue<int>("LoadLock.Purge.PumpDelayTime");
           

            //PrepareTrasfer需要把压力调到VacuumBasePressure以下
            if (_targetPressure > _llVacPressure)
            {
                _targetPressure = _llVacPressure;
            }

            string reason;

            if (!LoadLockDevice.SetFastVentValve(false, out reason) || !LoadLockDevice.SetSlowVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }
            if (!LoadLockDevice.SetFastPumpValve(false, out reason) || !LoadLockDevice.SetSlowPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            bool isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            if (isAtmMode)
            {
                EV.PostInfoLog(Module, $"system in atm mode, {LoadLockDevice.Module} pump skipped");
                return Result.DONE;
            }

            if (!LoadLockDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"Can not pump, door is open");
                return Result.FAIL;
            }

            if (!TMDevice.CheckSlitValveClose(ModuleHelper.Converter(LoadLockDevice.Module)))
            {
                EV.PostAlarmLog(Module, $"LoadLock Can not servo to pressure, slit valve is open");
                return Result.FAIL;
            }

            //if (Math.Abs(_targetPressure - LoadLockDevice.ChamberPressure) < _balancePressureDiff && LoadLockDevice.CheckVacuum())
            //{
            //    return Result.DONE;
            //}

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                TimeDelay((int)RoutineStep.TimeDelay1, 1);

                SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _slowPumpTimeout);

                FastPump((int)RoutineStep.FastPump, _pumpBasePressure, _fastPumpTimeout);

                TimeDelay((int)RoutineStep.TimeDelay2, _pumpDelayTime);

                CloseFastPumpValve((int)RoutineStep.CloseFastValve);

                CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);

                TimeDelay((int)RoutineStep.TimeDelay3, 1);

                SlowVent((int)RoutineStep.SlowVent, _targetPressure - 5, _slowVentTimeout);

                CloseVentValve((int)RoutineStep.CloseSlowVentValve);

                // 稍微等一下，确保DI状态更新
                TimeDelay((int)RoutineStep.ClosePumpValveDelay, 2);
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

        public override void Abort()
        {
            LoadLockDevice.SetSlowPumpValve(false, out _);
            LoadLockDevice.SetFastPumpValve(false, out _);
        }
    }
}
