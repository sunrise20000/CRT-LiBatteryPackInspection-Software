using System;
using System.Diagnostics;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadLeakCheckRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            SlowPump,
            CloseSlowValve,
            FastPump,
            PumpDelay,
            CloseFastValve,
            CheckDoor,
            RunPumpRoutine,
            ContinuePump,
            ClosePumpValve,
            DoLeakCheck,
            CalcLeakCheck,
            StartLoop,
            LoopVent,
            LoopPump,
            StopLoop,
            SlowPumpP, FastPumpP, PumpDelayP, CloseFastValveP, CloseSlowValveP, SlowVentV, CloseFastVentValveV, VentDelayV,

            TimeDelay1,
            TimeDelay2
        }

        private int _paramContinuePumpTime;
        private int _paramLeakCheckTime;
        private double _beginPressure;
        private double _pumpBasePressure;
        private double _pumpSwitchPressure;
        private int _pumpTimeout;
        private int _ventTimeout;
        private int _routineTimeOut;
        private double _leakSpec;
        private int _purgeCount;

        private double _pumpBasePressure1;
        private double _ventBasePressure1;
        private int _pumpDelayTime;
        private int _ventDelayTime;

        private double _beginTime;
        private int _calTimes;              //第几次计算LeakCheck的值（每60秒计算一次）

        private Stopwatch _swTimer = new Stopwatch();
        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }


        public UnLoadLeakCheckRoutine()
        {
            Module = ModuleName.UnLoad.ToString();
            Name = "LeakCheck";
        }

        internal void Init(int pumpTime, int leakCheckTime)
        {
            _paramContinuePumpTime = pumpTime;
            _paramLeakCheckTime = leakCheckTime;
        }

        internal void Init()
        {
            _paramContinuePumpTime = SC.GetValue<int>("UnLoad.LeakCheck.PumpDelayTime");
            _paramLeakCheckTime = SC.GetValue<int>("UnLoad.LeakCheck.LeakCheckDelayTime");
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _swTimer.Restart();


            string reason;
            if (!UnLoadDevice.SetFastVentValve(false, out reason) || !UnLoadDevice.SetSlowVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }
            bool isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            _pumpSwitchPressure = SC.GetValue<double>("UnLoad.Pump.SlowFastPumpSwitchPressure");            
            _pumpBasePressure = SC.GetValue<double>("UnLoad.Pump.PumpBasePressure");
            _routineTimeOut = SC.GetValue<int>("UnLoad.LeakCheck.RoutineTimeOut");
            _leakSpec = SC.GetValue<double>("UnLoad.LeakCheck.LeakSpec");
            _purgeCount = SC.GetValue<int>("UnLoad.LeakCheck.CyclePurgeCount");

            _pumpTimeout = SC.GetValue<int>("UnLoad.LeakCheck.PumpTimeout");
            _ventTimeout = SC.GetValue<int>("UnLoad.LeakCheck.VentTimeout");
            _pumpDelayTime = SC.GetValue<int>("UnLoad.LeakCheck.LoopPumpDelayTime");
            _ventDelayTime = SC.GetValue<int>("UnLoad.LeakCheck.LoopVentDelayTime");

            _pumpBasePressure1 = SC.GetValue<double>("UnLoad.Pump.PumpBasePressure");
            _ventBasePressure1 = SC.GetValue<double>("UnLoad.Vent.VentBasePressure");


            if (isAtmMode)
            {
                EV.PostInfoLog(Module, $"system in atm mode, {Module} pump skipped");
                return Result.DONE;
            }
            if (!UnLoadDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"Can not leakCheck, lid is open");
                return Result.FAIL;
            }
            if (!TMDevice.SetFastPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"can not leakCheck, TM fast pump value can not close");
                return Result.FAIL;
            }
            if (!TMDevice.CheckSlitValveClose(ModuleName.LoadLock))
            {
                EV.PostAlarmLog(Module, $"Can not leakCheck, slit valve is open");
                return Result.FAIL;
            }
            if (!TmIoInterLock.SetUnloadLeakCheckRoutineRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not LeakCheck,{reason}");
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

                Loop((int)RoutineStep.StartLoop, _purgeCount); 

                SlowPump((int)RoutineStep.SlowPumpP, _pumpSwitchPressure, _pumpTimeout);
                FastPump((int)RoutineStep.FastPumpP, _pumpBasePressure1, _pumpTimeout);
                TimeDelay((int)RoutineStep.PumpDelayP, _pumpDelayTime);
                CloseFastPumpValve((int)RoutineStep.CloseFastValveP);
                CloseSlowPumpValve((int)RoutineStep.CloseSlowValveP);

                TimeDelay((int)RoutineStep.TimeDelay1, 1);
                SlowVent((int)RoutineStep.SlowVentV, _ventBasePressure1, _ventTimeout);
                TimeDelay((int)RoutineStep.VentDelayV, _ventDelayTime);
                CloseVentValve((int)RoutineStep.CloseFastVentValveV);
                TimeDelay((int)RoutineStep.TimeDelay2, 1);

                EndLoop((int)RoutineStep.StopLoop);

                SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _pumpTimeout);
                FastPump((int)RoutineStep.FastPump, _pumpBasePressure, _pumpTimeout);
                TimeDelay((int)RoutineStep.PumpDelay, _paramContinuePumpTime);
                CloseFastPumpValve((int)RoutineStep.CloseFastValve);
                CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);
                DoLeakCheck((int)RoutineStep.DoLeakCheck, _paramLeakCheckTime);
                CalcLeackCheckPerMinute(_paramLeakCheckTime);

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
            TmIoInterLock.SetUnloadLeakCheckRoutineRunning(false, out _);

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

        public void DoLeakCheck(int id, double time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Keep pressure for {time} seconds");

                _beginTime = _swTimer.ElapsedMilliseconds;
                _beginPressure = UnLoadDevice.ChamberPressure;
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
                double endPressure = UnLoadDevice.ChamberPressure;
                double leakSpan = endPressure - _beginPressure;
                double leakRate = (endPressure - _beginPressure) / (leakSecond / 60.0);
                if (leakRate > _leakSpec)
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.FAIL.ToString(), Module, Module);
                    EV.PostInfoLog(Module, $"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");

                }
                else
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.Succeed.ToString(), Module, Module);
                    EV.PostInfoLog(Module, $"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");

                }
            }
            else
            {
                if ((int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000) >= 60 * _calTimes)
                {
                    _calTimes++;
                    int leakSecond = (int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000);
                    double endPressure = UnLoadDevice.ChamberPressure;
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
                double endPressure = UnLoadDevice.ChamberPressure; // 
                double leakSpan = endPressure - _beginPressure;
                double leakRate = (endPressure - _beginPressure) / (time / 60.0);
                if (leakRate > _leakSpec)
                {
                    LeakCheckDataRecorder.Add(time, _beginPressure, endPressure, leakRate, Result.FAIL.ToString(), "", Module);
                    EV.PostInfoLog(Module, $"{Module} leakcheck result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2} mbar, end: {endPressure:F2} mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    return false;
                }
                else
                {
                    LeakCheckDataRecorder.Add(time, _beginPressure, endPressure, leakRate, Result.Succeed.ToString(), "", Module);
                    EV.PostInfoLog(Module, $"{Module} leakcheck result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2} mbar, end: {endPressure:F2} mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    return true;
                }
            });
        }
    }
}
