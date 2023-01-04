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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.P116PIDTC;

namespace Mainframe.TMs.Routines
{
    public class TMVentRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            CloseTurboPumpValves, //Fast pump, backing valve, iso valve
            CloseValveDelay,

            TurboPumpOff,
            
            Vent,
            VentDelay,

            CloseVentValves,
            CloseVentValveDelay,
            TimeDelay,
            OpenV80,
            CloseV80,
            CloseV81,
            CloseV82,
            SetMFC1,
            SetMFC2,
            OpenSlowVent,
            OpenFastVent,
            TimeStayATM,
            CloseSlowVent
        }

        private double _ventBasePressure;
        private double _slowFastVentSwitchPressure;
        private int _slowVentTimeout;
        private int _fastVentTime;
        private int _ventDelayTime;
        private bool _useSettingValue;
        private int _routineTimeOut;

        private double _bufferMaxTemp;

        private SicTM _tm;
        private IoSensor _bufferLid;
        private IoSensor _tmLid;
        private Devices.IoPump _pumpType;
        private LoadLock _ll;
        private IoSensor _preHeatLid;
        private SicPM.Devices.IoMFC _mfc60;
        private IoInterLock _tmIoInterLock;
        private P116PIDTC _p116PIDTC;

        private double _mfc60Default1;
        private double _mfc60Default2;

        private Stopwatch _swTimer = new Stopwatch();
        public TMVentRoutine( )
        {
            Module = ModuleName.TM.ToString();
            Name = "Vent";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ Module}");
            _ll = DEVICE.GetDevice<SicLoadLock>($"LoadLock.LoadLock");
            _bufferLid = DEVICE.GetDevice<IoSensor>($"Buffer.BufferLidClosed");
            _tmLid = DEVICE.GetDevice<IoSensor>($"TM.TMLidClosed");
            _pumpType = DEVICE.GetDevice<Devices.IoPump>($"TM.TMPump1");
            _preHeatLid = DEVICE.GetDevice<IoSensor>($"TM.PreHeatStationLidClosed");
            _mfc60 = DEVICE.GetDevice<SicPM.Devices.IoMFC>($"TM.Mfc60");
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
            _p116PIDTC = DEVICE.GetDevice<P116PIDTC>("TM.P116PIDTC");
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


        public bool Initalize()
        {
            return true;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            string reason;
            if (!_tm.SetSlowPumpValve(false, out reason) || !_tm.SetFastPumpValve(false, out reason) )
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            if (_tm.CheckAtm())
            {
                EV.PostInfoLog(Module, $"{_tm.Module} in atm, vent skipped");
                return Result.DONE;
            }


            ////Buffer温度超过设定值TM不能Vent
            //_bufferMaxTemp = SC.GetValue<double>("TM.Vent.BufferMaxTemp");
            //if(_p116PIDTC.fPVInValue >= _bufferMaxTemp)
            //{
            //    EV.PostWarningLog(Module, $"Can not vent, Buffer Temp more than {_bufferMaxTemp}");
            //    return Result.FAIL;
            //}


            ModuleName[] modules = new ModuleName[] { ModuleName.LoadLock, ModuleName.PM1 };
            foreach (var moduleName in modules)
            {
                if (!_tm.CheckSlitValveClose(moduleName))
                {
                    EV.PostAlarmLog(Module, $"Can not vent, {moduleName} slit valve not closed");
                    return Result.FAIL;
                }
            }

            _routineTimeOut = SC.GetValue<int>("TM.Vent.RoutineTimeOut");
            _mfc60Default1 = SC.GetValue<double>("TM.Vent.Mfc60Default1");
            _mfc60Default2 = SC.GetValue<double>("TM.Vent.Mfc60Default2");
            _slowFastVentSwitchPressure = SC.GetValue<double>("TM.Vent.SlowFastVentSwitchPressure");
            _slowVentTimeout = SC.GetValue<int>("TM.Vent.SlowVentTimeout");
            _fastVentTime = SC.GetValue<int>("TM.Vent.FastVentTimeout");           
            if (!_useSettingValue)
            {
                _ventBasePressure = SC.GetValue<double>("TM.Vent.VentBasePressure"); 
                _ventDelayTime = SC.GetValue<int>("TM.Vent.VentDelayTime");
            }

            if (!_bufferLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not vent,Buffer lid is not open");
                return Result.FAIL;
            }
            if (!_tmLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not vent,TM lid is not open");
                return Result.FAIL;
            }
            if (_pumpType.IsAlarm)
            {
                EV.PostAlarmLog(Module, $"can not vent,TM pump alarm");
                return Result.FAIL;
            }
            if (!_pumpType.IsRunning)
            {
                EV.PostAlarmLog(Module, $"can not vent,TM pump is not running");
                return Result.FAIL;
            }
            if (!_tm.SetTmToLLVent(false, out _))
            {
                EV.PostAlarmLog(Module, $"can not vent,can not close v85!");
            }
            if (!_tmIoInterLock.SetTMVentRoutineRunning(true, out reason))
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

                //关闭V81
                CloseFastPump((int)RoutineStep.CloseV81);

                //关闭V82
                CloseSlowPump((int)RoutineStep.CloseV82);

                //设定MFC60慢充流量
                SetMFCToSetPoint((int)RoutineStep.SetMFC1, _mfc60, _mfc60Default1);

                //打开V77,等待压力大于200mbar
                OpenSlowVent((int)RoutineStep.OpenSlowVent, _tm, _slowFastVentSwitchPressure, _slowVentTimeout);

                //打开V80
                //OpenBufferVent((int)RoutineStep.OpenV80);

                //设定MFC60快充流量
                SetMFCToSetPoint((int)RoutineStep.SetMFC2, _mfc60, _mfc60Default2);

                //等待压力大于1020mbar
                OpenFastVent((int)RoutineStep.OpenFastVent, _tm, _ventBasePressure, _fastVentTime);

                //等待5s
                TimeDelay((int)RoutineStep.TimeStayATM, _ventDelayTime);

                //关闭V77
                CloseSlowVentValve((int)RoutineStep.CloseSlowVent, _tm);

                //关闭V80
                //CloseBufferVent((int)RoutineStep.CloseV80);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                _tm.SetFastVentValve(false, out _);
                _tm.SetSlowVentValve(false, out _);
                return Result.FAIL;
            }


            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _tmIoInterLock.DoTmVentUpRoutineRunning = false;

            return Result.DONE;
        }

        

        public override void Abort()
        {
            _tmIoInterLock.DoTmVentUpRoutineRunning = false;
            //_tm.SetFastVentValve(false, out _);
            //_tm.SetSlowVentValve(false, out _);

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
