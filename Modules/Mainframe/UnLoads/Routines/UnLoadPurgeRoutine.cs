﻿using System;
using System.Diagnostics;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadPurgeRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            SlowPump,
            FastPump,
            PumpDelay,
            CloseFastValve,
            CloseSlowValve,
            SlowVent,
            FastVent,
            VentDelay,
            CloseFastVentValve,
            CloseSlowVentValve,
            StartLoop,
            LoopPump,
            LoopVent,
            StopLoop,
            TimeDelay1,
            TimeDelay2
        }

        private int _purgeCount;
        private int _routineTimeOut;
        private double _pumpSwitchPressure;
        private double _pumpBasePressure;
        private int _pumpDelayTime;
        private int _pumpTimeOut;
        private double _ventBasePressure;
        private int _ventDelayTime;
        private int _ventTimeOut;


        private Stopwatch _swTimer = new Stopwatch();

        public UnLoadPurgeRoutine()
        {
            Module = ModuleName.UnLoad.ToString();
            Name = "Purge";
        }

        public void Init()
        {

        }

        public override Result Start(params object[] objs)
        {
            Reset();
            if (objs.Length == 2 && int.TryParse(objs[0].ToString(), out int purgeCount)&& int.TryParse(objs[1].ToString(), out int pumpDelayTime))
            {
                _purgeCount = purgeCount;
                _pumpDelayTime = pumpDelayTime;
            }
            else
            {
                _purgeCount = SC.GetValue<int>("UnLoad.Purge.CyclePurgeCount");
                _pumpDelayTime = SC.GetValue<int>("UnLoad.Purge.PumpDelayTime");
            }

            _routineTimeOut = SC.GetValue<int>("UnLoad.Purge.RoutineTimeOut");
            _pumpSwitchPressure = SC.GetValue<double>("UnLoad.Pump.SlowFastPumpSwitchPressure");
            _pumpBasePressure = SC.GetValue<double>("UnLoad.Purge.PumpBasePressure");
            _pumpTimeOut = SC.GetValue<int>("UnLoad.Purge.PumpTimeOut");
            _ventBasePressure = SC.GetValue<double>("UnLoad.Purge.VentBasePressure");
            _ventDelayTime = SC.GetValue<int>("UnLoad.Purge.VentDelayTime");
            _ventTimeOut = _pumpTimeOut;
            
            if (!UnLoadDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"can not purge, lid is open");
                return Result.FAIL;
            }
            if (!TMDevice.SetFastPumpValve(false, out var reason))
            {
                EV.PostAlarmLog(Module, $"can not purge, TM fast pump value can not close");
                return Result.FAIL;
            }
            if (!TMDevice.CheckSlitValveClose(ModuleHelper.Converter(UnLoadDevice.Module)))
            {
                EV.PostAlarmLog(Module, $"Can not purge, slit valve is open");
                return Result.FAIL;
            }
            if (!TMDevice.SetTmToLLVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not vent,can not close v85!");
            }

            if (!TmIoInterLock.SetUnloadPurgeRoutineRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not purge,{reason}");
                return Result.FAIL;
            }
            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                return Result.DONE;
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

                if (_purgeCount > 0)
                {
                    Loop((int)RoutineStep.StartLoop, _purgeCount);

                    SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _pumpTimeOut);
                    FastPump((int)RoutineStep.FastPump, _pumpBasePressure, _pumpTimeOut);
                    TimeDelay((int)RoutineStep.PumpDelay, _pumpDelayTime);
                    CloseFastPumpValve((int)RoutineStep.CloseFastValve);
                    CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);

                    TimeDelay((int)RoutineStep.TimeDelay1, 2);

                    SlowVent((int)RoutineStep.SlowVent, _ventBasePressure, _ventTimeOut);
                    CloseVentValve((int)RoutineStep.CloseSlowVentValve);

                    TimeDelay((int)RoutineStep.VentDelay, _ventDelayTime);
                    EndLoop((int)RoutineStep.StopLoop);
                }

                TimeDelay((int)RoutineStep.TimeDelay2, 3);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            var result = Result.DONE;
            if (TmIoInterLock.SetUnloadPurgeRoutineRunning(false, out var reason) == false)
            {
                EV.PostWarningLog(Module, $"Unable to perform {nameof(TmIoInterLock.SetUnloadPurgeRoutineRunning)}, {reason}");
                result = Result.FAIL;
            }
                
            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            return result;
        }

        public override void Abort()
        {
            TmIoInterLock.SetUnloadPurgeRoutineRunning(false, out _);
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

    }
}
