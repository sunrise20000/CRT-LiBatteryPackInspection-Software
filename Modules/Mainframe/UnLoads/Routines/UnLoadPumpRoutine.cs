using System.Diagnostics;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadPumpRoutine : UnLoadBaseRoutine
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
            TimeDelay1,
        }

        private double _pumpBasePressure = 0;
        private double _pumpSwitchPressure = 100;

        private int _slowPumpTimeout;
        private int _fastPumpTimeout;
        private int _pumpDelayTime= 5;
        private int _routineTimeOut;


        private bool _useSettingValue;

        private Stopwatch _swTimer = new Stopwatch();

        public UnLoadPumpRoutine()
        {
            Module = ModuleName.UnLoad.ToString();
            Name = "Pump";
        }

        public void Init()
        {
            _useSettingValue = false;
        }

        public void Init(double basePressure, int pumpDelayTime)
        {
            _pumpBasePressure = basePressure;
            _pumpDelayTime = pumpDelayTime;
            _useSettingValue = true;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            string reason;
            if (!UnLoadDevice.SetFastVentValve(false, out reason) || !UnLoadDevice.SetSlowVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            bool isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            _pumpSwitchPressure = SC.GetValue<double>("UnLoad.Pump.SlowFastPumpSwitchPressure");           
            
            _slowPumpTimeout = SC.GetValue<int>("UnLoad.Pump.SlowPumpTimeout");
            _fastPumpTimeout = SC.GetValue<int>("UnLoad.Pump.FastPumpTimeout");
            _routineTimeOut = SC.GetValue<int>("UnLoad.Pump.RoutineTimeOut");

            if (!_useSettingValue)
            {
                _pumpBasePressure = SC.GetValue<double>("UnLoad.Pump.PumpBasePressure");
                _pumpDelayTime = SC.GetValue<int>("UnLoad.Pump.PumpDelayTime");
            }
            
            if (!TMDevice.SetFastPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"can not pump, TM fast pump value can not close");
                return Result.FAIL;
            }

            if (isAtmMode)
            {
                EV.PostInfoLog(Module, $"system in atm mode, {Module} pump skipped");
                return Result.DONE;
            }
            if (SC.ContainsItem("UnLoad.RunPumpRoutineEventBelowBasePressure") && !SC.GetValue<bool>("UnLoad.RunPumpRoutineEventBelowBasePressure"))
            {
                if (UnLoadDevice.ChamberPressure < _pumpBasePressure)
                {
                    EV.PostInfoLog(Module, $"{Module} already under pump base pressure");
                    return Result.DONE;
                }
            }

            if (!UnLoadDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"Can not pump, lid is open");
                return Result.FAIL;
            }

            if (!TMDevice.CheckSlitValveClose(ModuleName.UnLoad))
            {
                EV.PostAlarmLog(Module, $"Can not pump, slit valve is open");
                return Result.FAIL;
            }
            if (!TMDevice.SetTmToUnLoadVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not pump,can not close v85!");
            }
            if (!EFEMDevice.CheckSlitValveClose(ModuleName.UnLoad,ModuleName.WaferRobot))
            {
                EV.PostAlarmLog(Module, $"Can not pump, slit valve is open");
                return Result.FAIL;
            }

            if (!TmIoInterLock.SetUnloadPumpRoutineRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not pump,{reason}");
                return Result.FAIL;
            }

            _swTimer.Restart();
            Notify("Start");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CheckRoutineTimeOut();

                SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _slowPumpTimeout);

                FastPump((int)RoutineStep.FastPump, _pumpBasePressure, _fastPumpTimeout);

                TimeDelay((int)RoutineStep.PumpDelay, _pumpDelayTime);

                CloseFastPumpValve((int)RoutineStep.CloseFastValve);

                CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                UnLoadDevice.SetSlowPumpValve(false, out _);
                UnLoadDevice.SetFastPumpValve(false, out _);
                return Result.FAIL;
            }
            
            TmIoInterLock.SetUnloadPumpRoutineRunning(false, out _);

            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
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

    }
}
