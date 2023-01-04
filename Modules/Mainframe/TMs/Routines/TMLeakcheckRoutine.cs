using System;
using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using Mainframe.LLs;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;

namespace Mainframe.TMs.Routines
{
    public class TMLeakCheckRoutine : ModuleRoutine, IRoutine
    {
        enum RoutineStep
        {
            SlowPump,
            FastPump,
            VaccumDelay,
            PumpDelay,
            CloseValves,
            CLoseSlowValue,
            Delay,
            CheckSlitValves,
            RunPumpRoutine,
            ContinuePump,
            ClosePumpValve,
            DoLeakCheck,
            CalcLeakCheck,
            StartLoop,
            LoopVent,
            LoopPump,
            StopLoop
        }

        private TMPumpRoutine _pumpRoutine;
        private TMVentRoutine _ventRoutine;

        private int _paramContinuePumpTime;
        private int _paramLeakCheckTime;
        private double _beginPressure;
        private double _pumpBasePressure;
        private double _slowFastPumpSwitchPressure;
        private int _slowPumpTimeout;
        private int _fastPumpTime;
        private int _routineTimeOut;
        private double _leakSpec;
        private int _purgeCount;

        private double _beginTime;
        private int _calTimes;              //第几次计算LeakCheck的值（每60秒计算一次）

        private TM _tm;
        private IoSensor _bufferLid;
        private IoSensor _tmLid;
        private Devices.IoPump _pumpType;
        private LoadLock _ll;
        private IoSensor _preHeatLid;
        private IoInterLock _tmIoInterLock;
        private Stopwatch _swTimer = new Stopwatch();

        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }
        public TMLeakCheckRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "Leak Check";
            _tm = DEVICE.GetDevice<TM>($"{ ModuleName.System.ToString()}.{ Module}");
            _ll = DEVICE.GetDevice<SicLoadLock>($"LoadLock.LoadLock");
            _bufferLid = DEVICE.GetDevice<IoSensor>($"Buffer.BufferLidClosed");
            _tmLid = DEVICE.GetDevice<IoSensor>($"TM.TMLidClosed");
            _pumpType = DEVICE.GetDevice<Devices.IoPump>($"TM.TMPump1");
            _preHeatLid = DEVICE.GetDevice<IoSensor>($"TM.PreHeatStationLidClosed");
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");

            _pumpRoutine = new TMPumpRoutine();
            _ventRoutine = new TMVentRoutine();

        }

        internal void Init(int pumpTime, int leakCheckTime)
        {
            _paramContinuePumpTime = pumpTime;
            _paramLeakCheckTime = leakCheckTime;
        }

        internal void Init()
        {
            _paramContinuePumpTime = SC.GetValue<int>("TM.LeakCheck.PumpDelayTime"); ;
            _paramLeakCheckTime = SC.GetValue<int>("TM.LeakCheck.LeakCheckDelayTime"); ;
        }


        public Result Start(params object[] objs)
        {
            Reset();
            _swTimer.Restart();

            _pumpRoutine.Init(SC.GetValue<double>("TM.Purge.PumpBasePressure"), SC.GetValue<int>("TM.Purge.PumpDelayTime"));
            _ventRoutine.Init(SC.GetValue<double>("TM.Purge.VentBasePressure"), SC.GetValue<int>("TM.Purge.VentDelayTime"));

            string reason;
            if (!_tm.SetFastPumpValve(false, out reason) || !_tm.SetFastVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                EV.PostInfoLog(Module, $"system in atm mode, {_tm.Module} pump skipped");
                return Result.DONE;
            }
            if (SC.ContainsItem("TM.RunPumpRoutineEventBelowBasePressure") && !SC.ContainsItem("TM.RunPumpRoutineEventBelowBasePressure"))
            {
                if (_tm.ChamberPressure < _pumpBasePressure)
                {
                    EV.PostInfoLog(Module, $"{_tm.Module} already under pump base pressure");
                    return Result.DONE;
                }
            }

            ModuleName[] modules = new ModuleName[] { ModuleName.LoadLock, ModuleName.PM1 };
            foreach (var moduleName in modules)
            {
                if (!_tm.CheckSlitValveClose(moduleName))
                {
                    EV.PostAlarmLog(Module, $"Can not leakCheck, {moduleName} slit valve not closed");
                    return Result.FAIL;
                }
            }

            _slowFastPumpSwitchPressure = SC.GetValue<double>("TM.Pump.SlowFastPumpSwitchPressure");
            _slowPumpTimeout = SC.GetValue<int>("TM.Pump.PumpSlowTimeout");
            _fastPumpTime = SC.GetValue<int>("TM.Pump.FastPumpTimeout");
            _pumpBasePressure = SC.GetValue<double>("TM.Pump.PumpBasePressure");

            _routineTimeOut = SC.GetValue<int>("TM.LeakCheck.RoutineTimeOut");
            _leakSpec = SC.GetValue<double>("TM.LeakCheck.LeakSpec");
            _purgeCount = SC.GetValue<int>("TM.LeakCheck.CyclePurgeCount");

            if (!_ll.SetFastPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not leakCheck,LL fastPump can not close");
                return Result.FAIL;
            }
            if (!_bufferLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not leakCheck,Buffer lid is not open");
                return Result.FAIL;
            }
            if (!_tmLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not leakCheck,TM lid is not open");
                return Result.FAIL;
            }
            if (_pumpType.IsAlarm)
            {
                EV.PostAlarmLog(Module, $"can not leakCheck,TM pump alarm");
                return Result.FAIL;
            }
            if (!_pumpType.IsRunning)
            {
                EV.PostAlarmLog(Module, $"can not leakCheck,TM pump is not running");
                return Result.FAIL;
            }
            if (!_tmIoInterLock.SetTMLeakCheckRoutineRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not leakCheck,{reason}");
                return Result.FAIL;
            }

            Notify("Start");
            return Result.RUN;
        }

        public Result Monitor()
        {
            try
            {
                CheckRoutineTimeOut();

                Loop((int)RoutineStep.StartLoop, _purgeCount);
                ExecuteRoutine((int)RoutineStep.LoopPump, _pumpRoutine);
                ExecuteRoutine((int)RoutineStep.LoopVent, _ventRoutine);
                EndLoop((int)RoutineStep.StopLoop);


                OpenSlowPump((int)RoutineStep.SlowPump, _tm, _slowFastPumpSwitchPressure, _slowPumpTimeout);
                OpenFastPump((int)RoutineStep.FastPump, _tm, _pumpBasePressure, _fastPumpTime);
                TimeDelay((int)RoutineStep.VaccumDelay, _paramContinuePumpTime);
                CloseFastPump((int)RoutineStep.CloseValves, _tm);
                CloseSlowPump((int)RoutineStep.CLoseSlowValue, _tm);

                DoLeakCheck((int)RoutineStep.DoLeakCheck, _paramLeakCheckTime);
                CalcLeackCheckPerMinute(_paramLeakCheckTime);
                //CalcLeakCheck((int)RoutineStep.CalcLeakCheck, _paramLeakCheckTime);
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
            _tmIoInterLock.DoTmLeakCheckRoutineRunning = false;
            _swTimer.Stop();
            return Result.DONE;
        }

        public void Abort()
        {
            Stop("Leak check aborted");
            _tmIoInterLock.DoTmLeakCheckRoutineRunning = false;
            _pumpRoutine.Abort();
            _ventRoutine.Abort();

            _swTimer.Stop();
            LeakCheckDataRecorder.Add((int)_swTimer.ElapsedMilliseconds / 1000, _beginPressure, _tm.ChamberPressure, 0, Result.FAIL.ToString(), "", Module);
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

        public void CheckSlitValves(int id, TM tm)
        {
            Tuple<bool, Result> ret = Check(id, () =>
            {
                Notify("Check slit valve is all closed");

                ModuleName[] modules = new ModuleName[]
                    {ModuleName.LLA, ModuleName.LLB, ModuleName.PM1, ModuleName.PM2, ModuleName.PM3, ModuleName.PM4};

                foreach (var m in modules)
                {
                    if (!tm.CheckSlitValveClose(m))
                    {
                        Stop($"{m} slit valve not closed.");
                        return false;
                    }
                }

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
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
                _beginPressure = _tm.ChamberPressure;
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

            //Tuple<bool, Result> ret = Delay(id, () =>
            //    {
            //        Notify($"Keep pressure for {time} seconds");

            //        _beginPressure = _tm.ChamberPressure;

            //        return true;
            //    }, time * 1000);

            //if (ret.Item1)
            //{
            //    if (ret.Item2 == Result.RUN)
            //    {
            //        throw (new RoutineBreakException());
            //    }
            //}
        }

        public void CalcLeackCheckPerMinute(int time)
        {
            //大于LeakCheck时间,
            if ((int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000) >= time)
            {
                int leakSecond = (int)((_swTimer.ElapsedMilliseconds - _beginTime) / 1000);
                double endPressure = _tm.ChamberPressure;
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
                    double endPressure = _tm.ChamberPressure;
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
                double endPressure = _tm.ChamberPressure;
                double leakSpan = endPressure - _beginPressure;
                double leakRate = (endPressure - _beginPressure) / (time / 60.0);

                if (leakRate > _leakSpec)
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.FAIL.ToString(), Module, Module);
                    EV.PostInfoLog(Module, $"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    return false;
                }
                else
                {
                    LeakCheckDataRecorder.Add(time, (int)_beginPressure, (int)endPressure, leakRate, Result.Succeed.ToString(), Module, Module);
                    EV.PostInfoLog(Module, $"Leak check result: end at {DateTime.Now.ToString("HH:mm:ss")}, start: {_beginPressure:F2}mbar, end: {endPressure:F2}mbar, using {time} seconds, leak rate: {leakRate:F2}");
                    return true;
                }
            });
        }

        public void OpenSlowPump(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} slow pump valve");

                if (!tm.SetSlowPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.ChamberPressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastPumpValve(false, out string _);

                    Stop($"{tm.Name} pressure can not pump to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowPump(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} pump valves");

                if (!_tm.SetSlowPumpValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

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


        public void OpenFastPump(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} pump valve");

                if (!tm.SetFastPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.ChamberPressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastPumpValve(false, out string _);

                    Stop($"{tm.Name} pressure can not pump to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void CloseFastPump(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} pump valves");

                if (!_tm.SetFastPumpValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

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


    }

}
