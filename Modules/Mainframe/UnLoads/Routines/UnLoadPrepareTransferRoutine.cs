using System;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadPrepareTransferRoutine : UnLoadBaseRoutine
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
            StartLoop,
            EndLoop,

            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
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

        public UnLoadPrepareTransferRoutine()
        {
            Module = ModuleName.UnLoad.ToString();
            Name = "PrepareTransfer";
        }

        public override Result Start(params object[] objs)
        {
            // See methods PrepareTransfer() of SicRT\Equipments\Schedulers\SchedulerUnLoad.cs for details.
            // objs[0]: ModuleName robot
            // objs[1]: Hand blade
            // objs[2]: targetSlot
            // objs[3]: EnumTransferType transferType

            // Validate the arguments.
            //if (objs == null || objs.Length != 4 || (objs[0] is ModuleName moduleName) == false || (objs[3] is EnumTransferType transferType) == false)
            //{
            //    EV.PostAlarmLog(Module, $"Arguments of routine {nameof(UnLoadPrepareTransferRoutine)} are incorrect.");
            //    return Result.FAIL;
            //}

            Reset();
            _targetPressure = SC.GetValue<double>("TM.PressureBalance.BalancePressure");
            _llVacPressure = SC.GetValue<double>("UnLoad.VacuumPressureBase");

            _pumpBasePressure = SC.GetValue<double>("UnLoad.Purge.PumpBasePressure");
            _balancePressureDiff = SC.GetValue<double>("TM.PressureBalance.BalanceMaxDiffPressure");
            _pumpSwitchPressure = SC.GetValue<double>("UnLoad.Pump.SlowFastPumpSwitchPressure");
            _slowPumpTimeout = SC.GetValue<int>("UnLoad.Pump.SlowPumpTimeout");
            _fastPumpTimeout = SC.GetValue<int>("UnLoad.Pump.FastPumpTimeout");
            _slowVentTimeout = SC.GetValue<int>("UnLoad.Vent.SlowVentTimeout");
            _pumpDelayTime = SC.GetValue<int>("UnLoad.Purge.PumpDelayTime");

            //PrepareTrasfer需要把压力调到VacuumBasePressure以下
            if (_targetPressure > _llVacPressure)
            {
                _targetPressure = _llVacPressure;
            }

            string reason;
            
            if (!UnLoadDevice.SetFastVentValve(false, out reason) || !UnLoadDevice.SetSlowVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }
            if (!UnLoadDevice.SetFastPumpValve(false, out reason) || !UnLoadDevice.SetSlowPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            bool isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            if (isAtmMode)
            {
                EV.PostInfoLog(Module, $"system in atm mode, {UnLoadDevice.Module} pump skipped");
                return Result.DONE;
            }

            if (!UnLoadDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"Can not pump, door is open");
                return Result.FAIL;
            }

            if (!TMDevice.CheckSlitValveClose(ModuleHelper.Converter(UnLoadDevice.Module)))
            {
                EV.PostAlarmLog(Module, $"LoadLock Can not servo to pressure, slit valve is open");
                return Result.FAIL;
            }

            //if (Math.Abs(_targetPressure - UnLoadDevice.ChamberPressure) < _balancePressureDiff && UnLoadDevice.CheckVacuum())
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
                //if (_purgeCount > 0)
                //{
                //    Loop((int)RoutineStep.StartLoop, _purgeCount);

                TimeDelay((int)RoutineStep.TimeDelay1, 1);

                SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _slowPumpTimeout);

                FastPump((int)RoutineStep.FastPump, _pumpBasePressure, _fastPumpTimeout);

                TimeDelay((int)RoutineStep.TimeDelay2, _pumpDelayTime);

                CloseFastPumpValve((int)RoutineStep.CloseFastValve);

                CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);

                // 模拟器运行时由于以上动作刷新DI不及时导致触发Interlock，增加一些延时
                //TODO IoValue类中检查相关DI状态。
                TimeDelay((int)RoutineStep.TimeDelay3, 2);
                
                SlowVent((int)RoutineStep.SlowVent, _targetPressure - 5, _slowVentTimeout);

                CloseVentValve((int)RoutineStep.CloseSlowVentValve);
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
            UnLoadDevice.SetSlowPumpValve(false, out _);
            UnLoadDevice.SetFastPumpValve(false, out _);
        }
    }
}
