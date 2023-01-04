using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadLockPumpRoutine : LoadLockBaseRoutine
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
            ClosePumpValveDelay
        }

        private double _pumpBasePressure=0;
        private double _forelineBasePressure;
        private double _pumpSwitchPressure;

        private int _waitForelineTimeout;
        private int _slowPumpTimeout;
        private int _fastPumpTimeout;
        private int _pumpDelayTime = 5;
        private int _routineTimeOut;

        private IoInterLock _tmIoInterLock;

        private bool _useSettingValue;

        private Stopwatch _swTimer = new Stopwatch();

        public LoadLockPumpRoutine(ModuleName module )
        {
            Module = module.ToString();  
            Name   = "Pump";

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

            string reason;
            
            if (!LoadLockDevice.SetFastVentValve(false, out reason) || !LoadLockDevice.SetSlowVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            bool isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            _forelineBasePressure = SC.GetValue<double>("LoadLock.ForelinePressureBase");
            _pumpSwitchPressure = SC.GetValue<double>("LoadLock.Pump.SlowFastPumpSwitchPressure");            
            _waitForelineTimeout = SC.GetValue<int>("LoadLock.WaitForelinePressureTimeout");
            _slowPumpTimeout = SC.GetValue<int>("LoadLock.Pump.SlowPumpTimeout");
            _fastPumpTimeout = SC.GetValue<int>("LoadLock.Pump.FastPumpTimeout");
            _routineTimeOut = SC.GetValue<int>("LoadLock.Pump.RoutineTimeOut");

            if (!_useSettingValue)
            {
                _pumpBasePressure = SC.GetValue<double>("LoadLock.Pump.PumpBasePressure");
                _pumpDelayTime = SC.GetValue<int>("LoadLock.Pump.PumpDelayTime");
            }
            

            if (!TMDevice.SetFastPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"can not pump, TM fast pump value can not close");
                return Result.FAIL;
            }

            if (isAtmMode)
            {
                EV.PostInfoLog(Module, $"system in atm mode, {LoadLockDevice.Module} pump skipped");
                return Result.DONE;
            }
            if (SC.ContainsItem("LoadLock.RunPumpRoutineEventBelowBasePressure") && !SC.GetValue<bool>("LoadLock.RunPumpRoutineEventBelowBasePressure"))
            {
                if (LoadLockDevice.ChamberPressure < _pumpBasePressure)
                {
                    EV.PostInfoLog(Module, $"{LoadLockDevice.Module} already under pump base pressure");
                    return Result.DONE;
                }
            }

            if (!LoadLockDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"Can not pump, lid is open");
                return Result.FAIL;
            }

            if (!TMDevice.CheckSlitValveClose(ModuleHelper.Converter(LoadLockDevice.Module)))
            {
                EV.PostAlarmLog(Module, $"Can not pump, slit valve is open");
                return Result.FAIL;
            }
            if (!TMDevice.SetTmToLLVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not pump,can not close v85!");
            }

            if (!_tmIoInterLock.SetLLPumpRoutineRunning(true, out reason))
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

                TimeDelay((int)RoutineStep.RequestPumpDelay, 1);

                SlowPump((int)RoutineStep.SlowPump, _pumpSwitchPressure, _slowPumpTimeout);

                FastPump((int)RoutineStep.FastPump, _pumpBasePressure, _fastPumpTimeout); 

                TimeDelay((int)RoutineStep.PumpDelay, _pumpDelayTime);

                CloseFastPumpValve((int)RoutineStep.CloseFastValve);

                CloseSlowPumpValve((int)RoutineStep.CloseSlowValve);
                
                // 稍微等一下，确保DI状态更新
                TimeDelay((int)RoutineStep.ClosePumpValveDelay, 2);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                LoadLockDevice.SetSlowPumpValve(false, out _);
                LoadLockDevice.SetFastPumpValve(false, out _);
                return Result.FAIL;
            }
            
            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _tmIoInterLock.DoLLPumpDownRoutineRunning = false;

            return Result.DONE;
        }

        public override void Abort()
        {
            _tmIoInterLock.DoLLPumpDownRoutineRunning = false;
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
