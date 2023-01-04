using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadLockVentRoutine : LoadLockBaseRoutine
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

        private IoInterLock _tmIoInterLock;
        private bool _useSettingValue;

        private Stopwatch _swTimer = new Stopwatch();

        public LoadLockVentRoutine(ModuleName module)
        {
            Module = module.ToString();
            Name = "Vent";
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
        }

        public void Init()
        {
            _useSettingValue = false;
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

            if (LoadLockDevice.CheckAtm())
            {
                EV.PostInfoLog(Module, $"{LoadLockDevice.Module} in atm, vent skipped");
                return Result.DONE;
            }
            
            if (!TMDevice.CheckSlitValveClose(ModuleHelper.Converter(LoadLockDevice.Module)))
            {
                EV.PostAlarmLog(Module, $"can not vent, slit valve is open");
                return Result.FAIL;
            }
            if (!LoadLockDevice.CheckLidClose())
            {
                EV.PostAlarmLog(Module, $"can not vent, lid is open");
                return Result.FAIL;
            }            
            if (!LoadLockDevice.SetFastPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"can not vent, TM fast pump value can not close");
                return Result.FAIL;
            }

            if (!LoadLockDevice.SetFastPumpValve(false, out reason)
                || !LoadLockDevice.SetSlowPumpValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }
            if (!TMDevice.SetTmToLLVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not vent,can not close v85!");
            }
            if (!_tmIoInterLock.SetLLVentRoutineRunning(true, out reason))
            {
                EV.PostAlarmLog(Module, $"can not vent,{reason}");
                return Result.FAIL;
            }

            _slowVentTimeout = SC.GetValue<int>("LoadLock.Vent.SlowVentTimeout");
            if (!_useSettingValue)
            {
                _ventBasePressure = SC.GetValue<double>("LoadLock.Vent.VentBasePressure");
                _ventDelayTime = SC.GetValue<int>("LoadLock.Vent.VentDelayTime");
            }
            _routineTimeOut = SC.GetValue<int>("LoadLock.Vent.RoutineTimeOut");

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

                CloseVentValve((int)RoutineStep.CloseSlowVentValve);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                LoadLockDevice.SetSlowVentValve(false, out _);
                LoadLockDevice.SetFastVentValve(false, out _);
                return Result.FAIL;
            }

            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _tmIoInterLock.DoLLVentUpRoutineRunning = false;

            return Result.DONE;
        }

        public override void Abort()
        {
            _tmIoInterLock.DoLLVentUpRoutineRunning = false;
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
