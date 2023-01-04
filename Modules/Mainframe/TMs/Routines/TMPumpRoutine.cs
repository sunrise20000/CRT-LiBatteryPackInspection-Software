using System;
using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using Mainframe.LLs;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;

namespace Mainframe.TMs.Routines
{
    public class TMPumpRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            //CheckForelinePressure,
            //RequestPump,
            //StartDelay,
            //RequestPumpDelay,
            //Pump,
            //PumpDelay,
            //Delay,

            CloseV77,
            CloseV80,
            SlowPump,
            FastPump,
            VaccumDelay,
            CloseValves,
            CLoseSlowValue,
            ClosePumpValveDelay,
        }

        //private SicTM _tm;
        private IoSensor _bufferLid;
        private IoSensor _tmLid;
        private Devices.IoPump _pumpType;
        private LoadLock _ll;
        private IoInterLock _tmIoInterLock;

        private double _forelineBasePressure;
        private double _pumpBasePressure;
        private double _slowFastPumpSwitchPressure;
        private int _waitForelineTimeout;
        private int _slowPumpTimeout;
        private int _fastPumpTime;
        private int _pumpDelayTime;
        private int _routineTimeOut;


        private bool _useSettingValue;

        private Stopwatch _swTimer = new Stopwatch();

        public TMPumpRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "Pump";
            //_tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ Module}");

            _ll = DEVICE.GetDevice<SicLoadLock>($"LoadLock.LoadLock");
            _bufferLid = DEVICE.GetDevice<IoSensor>($"Buffer.BufferLidClosed");
            _tmLid = DEVICE.GetDevice<IoSensor>($"TM.TMLidClosed");
            _pumpType = DEVICE.GetDevice<Devices.IoPump>($"TM.TMPump1");
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
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

            _forelineBasePressure = SC.GetValue<double>("TM.ForelinePressureBase");
            _slowFastPumpSwitchPressure = SC.GetValue<double>("TM.Pump.SlowFastPumpSwitchPressure");
            _waitForelineTimeout = SC.GetValue<int>("TM.WaitForelinePressureTimeout");
            _slowPumpTimeout = SC.GetValue<int>("TM.Pump.PumpSlowTimeout");
            _fastPumpTime = SC.GetValue<int>("TM.Pump.FastPumpTimeout");
            _routineTimeOut = SC.GetValue<int>("TM.Pump.RoutineTimeOut");

            if (!_useSettingValue)
            {
                _pumpBasePressure = SC.GetValue<double>("TM.Pump.PumpBasePressure");
                _pumpDelayTime = SC.GetValue<int>("TM.Pump.PumpDelayTime");
            }

            string reason;
            if (!TMDevice.SetFastPumpValve(false, out reason) || !TMDevice.SetFastVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }
            
            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                EV.PostInfoLog(Module, $"system in atm mode, {TMDevice.Module} pump skipped");
                return Result.DONE;
            }
            if (SC.ContainsItem("TM.RunPumpRoutineEventBelowBasePressure") && !SC.GetValue<bool>("TM.RunPumpRoutineEventBelowBasePressure"))
            {
                if (TMDevice.ChamberPressure < _pumpBasePressure)
                {
                    EV.PostInfoLog(Module, $"{TMDevice.Module} already under pump base pressure");
                    return Result.DONE;
                }
            }           

            ModuleName[] modules = new ModuleName[] { ModuleName.LoadLock, ModuleName.PM1 };
            foreach (var moduleName in modules)
            {
                if (!TMDevice.CheckSlitValveClose(moduleName))
                {
                    EV.PostAlarmLog(Module, $"Can not pump, {moduleName} slit valve not closed");
                    return Result.FAIL;
                }
            }


            if (!_bufferLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not pump,Buffer lid is not closed");
                return Result.FAIL;
            }
            if (!_tmLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not pump,TM lid is not closed");
                return Result.FAIL;
            }
            if (_pumpType.IsAlarm)
            {
                EV.PostAlarmLog(Module, $"can not pump,TM pump alarm");
                return Result.FAIL;
            }
            if (!_pumpType.IsRunning)
            {
                EV.PostAlarmLog(Module, $"can not pump,TM pump is not running");
                return Result.FAIL;
            }
            if (!TMDevice.SetTmToLLVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not pump,can not close v85!");
            }
            if (!_tmIoInterLock.SetTMPumpRoutineRunning(true, out reason))
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

                //CheckForelinePressure((int)RoutineStep.CheckForelinePressure, _tm, _forelineBasePressure, _waitForelineTimeout);

                //关闭V77
                CloseTMVent((int)RoutineStep.CloseV77);

                //关闭V80
                //CloseBufferVent((int)RoutineStep.CloseV80);

                //打开V82,等待压力低于200mbar(可配置)
                OpenSlowPump((int)RoutineStep.SlowPump, TMDevice, _slowFastPumpSwitchPressure, _slowPumpTimeout);

                //打开V81,压力达到0mbar(可配置)
                OpenFastPump((int)RoutineStep.FastPump, TMDevice, _pumpBasePressure, _fastPumpTime);

                //等待5s(可配置)
                TimeDelay((int)RoutineStep.VaccumDelay, _pumpDelayTime);

                //关闭
                CloseFastPump((int)RoutineStep.CloseValves, TMDevice);

                CloseSlowPump((int)RoutineStep.CLoseSlowValue, TMDevice);
                
                // 稍微等一下，确保DI状态更新
                TimeDelay((int)RoutineStep.ClosePumpValveDelay, 2);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                TMDevice.SetFastPumpValve(false, out _);
                TMDevice.SetSlowPumpValve(false, out _);
                return Result.FAIL;
            }


            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _tmIoInterLock.DoTmPumpDownRoutineRunning = false;

            return Result.DONE;
        }

        

        public override void Abort()
        {
            _tmIoInterLock.DoTmPumpDownRoutineRunning = false;
            //_tm.SetFastPumpValve(false, out _);
            //_tm.SetSlowPumpValve(false, out _);

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


        public void CheckPressure(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check {tm.Name}  pressure ");

                return true;
            }, () =>
            {
                return tm.ForelinePressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{tm.Name}  pressure can not lower than {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CheckForelinePressure(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Check {tm.Name} foreline pressure ");

                return true;
            }, () =>
            {
                return tm.ForelinePressure <= basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{tm.Name} foreline pressure can not lower than {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

       
    }


}
