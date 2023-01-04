using System.Diagnostics;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadVentRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            SlowVent,
            FastVent,
            VentDelay,
            CloseSlowVentValve,
            CloseFastVentValve,
            CloseValveDelay,
        }

        private double _ventBasePressure;
        private int _slowVentTimeout;
        private int _ventDelayTime;
        private int _routineTimeOut;
        private bool _useSettingValue;

        private Stopwatch _swTimer = new Stopwatch();

        public UnLoadVentRoutine()
        {
            Module = ModuleName.UnLoad.ToString();
            Name = "Vent";
        }

        public void Init(double basePressure, int ventDelayTime)
        {
            _ventBasePressure = basePressure;
            _ventDelayTime = ventDelayTime;
            _useSettingValue = true;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            string reason;

            if (UnLoadDevice.CheckAtm())
            {
                EV.PostInfoLog(Module, $"{UnLoadDevice.Module} in atm, vent skipped");
                return Result.DONE;
            }

            if (!TMDevice.CheckSlitValveClose(ModuleName.UnLoad))
            {
                EV.PostAlarmLog(Module, $"can not vent, slit valve is open");
                return Result.FAIL;
            }
            if (!UnLoadDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"can not vent, lid is open");
                return Result.FAIL;
            }
           
            if (!TMDevice.SetFastPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"can not vent, TM fast pump value can not close");
                return Result.FAIL;
            }

            if (!UnLoadDevice.SetFastPumpValve(false, out reason)
                || !UnLoadDevice.SetSlowPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }
            if (!TMDevice.SetTmToLLVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not vent,can not close v85!");
            }

            _slowVentTimeout = SC.GetValue<int>("UnLoad.Vent.SlowVentTimeout");
            if (!_useSettingValue)
            {
                _ventBasePressure = SC.GetValue<double>("UnLoad.Vent.VentBasePressure");
                _ventDelayTime = SC.GetValue<int>("UnLoad.Vent.VentDelayTime");
            }
            _routineTimeOut = SC.GetValue<int>("UnLoad.Vent.RoutineTimeOut");


            if (!TmIoInterLock.SetUnloadVentRoutineRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not vent,{reason}");
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

                SlowVent((int)RoutineStep.SlowVent, _ventBasePressure, _slowVentTimeout);

                TimeDelay((int)RoutineStep.VentDelay, _ventDelayTime);

                CloseVentValve((int)RoutineStep.CloseFastVentValve);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                UnLoadDevice.SetSlowVentValve(false, out _);
                UnLoadDevice.SetFastVentValve(false, out _);
                return Result.FAIL;
            }

            TmIoInterLock.SetUnloadVentRoutineRunning(false, out _);
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
