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
    public class TMPurgeRoutine  : TMBaseRoutine
    {
        enum RoutineStep
        {
            StartLoop,
            LoopPump,
            LoopVent,
            StopLoop,

            //Pump
            CloseV77,
            CloseV80,
            SlowPump,
            FastPump,
            VaccumDelay,
            CloseValves,
            CLoseSlowValue,

            //Vent
            CloseV81,
            CloseV82,
            Delay1,
            SetMFC,
            OpenSlowVent,
            OpenFastVent,
            TimeStayATM,
            CloseSlowVent,
            DelayFinal
        }


        //private TMPumpRoutine _pumpRoutine;
        //private TMVentRoutine _ventRoutine;
        private TM _tm;
        private IoSensor _bufferLid;
        private IoSensor _tmLid;
        private Devices.IoPump _pumpType;
        private LoadLock _ll;
        private IoInterLock _tmIoInterLock;
        private Stopwatch _swTimer = new Stopwatch();

        private int _routineTimeOut;
        private int _purgeCount;

        private double _slowFastPumpSwitchPressure;
        private double _pumpDelayTime;
        private double _pumpBasePressure;
        private int _pumpTimeOut;

        private double _ventBasePressure;
        private double _slowFastVentSwitchPressure;
        private int _slowVentTimeout;
        private int _fastVentTime;


        private double _mfc60Default1;
        private double _mfc60Default2;

        private SicPM.Devices.IoMFC _mfc60;

        private double _ventDelayTime;

        private bool _useSettingValue = false;

        public TMPurgeRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "Purge";

            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ Module}");
            _ll = DEVICE.GetDevice<SicLoadLock>($"LoadLock.LoadLock");
            _bufferLid = DEVICE.GetDevice<IoSensor>($"Buffer.BufferLidClosed");
            _tmLid = DEVICE.GetDevice<IoSensor>($"TM.TMLidClosed");
            _pumpType = DEVICE.GetDevice<Devices.IoPump>($"TM.TMPump1");
            _tmIoInterLock = DEVICE.GetDevice<IoInterLock>("TM.IoInterLock");
            _mfc60 = DEVICE.GetDevice<SicPM.Devices.IoMFC>($"TM.Mfc60");

            //_pumpRoutine = new TMPumpRoutine();
            //_ventRoutine = new TMVentRoutine();
        }

        public void Init(int purgeCount, double pumpPressure, int pumpDelayTime, double ventPressure, int ventDelayTime)
        {
            _purgeCount = purgeCount;
            _pumpBasePressure = pumpPressure;
            _pumpDelayTime = pumpDelayTime;
            _ventBasePressure = ventPressure;
            _ventDelayTime = ventDelayTime;

            _useSettingValue = true;
        }
        
        public void Init(int purgeCount, int pumpDelayTime)
        {
            _purgeCount = purgeCount;
            _pumpDelayTime = pumpDelayTime;

            _pumpBasePressure = SC.GetValue<double>("TM.Purge.PumpBasePressure");
            _ventBasePressure = SC.GetValue<double>("TM.Purge.VentBasePressure");
            _ventDelayTime = SC.GetValue<int>("TM.Purge.PumpDelayTime");
            
            _useSettingValue = true;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (!_useSettingValue)
            {
                _purgeCount = SC.GetValue<int>("TM.Purge.CyclePurgeCount");
                _pumpBasePressure = SC.GetValue<double>("TM.Purge.PumpBasePressure");
                _pumpDelayTime = SC.GetValue<int>("TM.Purge.PumpDelayTime");
                _ventBasePressure = SC.GetValue<double>("TM.Purge.VentBasePressure");
                _ventDelayTime = SC.GetValue<int>("TM.Purge.PumpDelayTime");
            }

            _routineTimeOut = SC.GetValue<int>("TM.Purge.RoutineTimeOut");

            _slowFastPumpSwitchPressure = SC.GetValue<double>("TM.Purge.SlowFastPumpSwitchPressure");
            _pumpTimeOut = SC.GetValue<int>("TM.Purge.PumpTimeOut");

            _mfc60Default1 = SC.GetValue<double>("TM.Purge.Mfc60Default1");
            _mfc60Default2 = SC.GetValue<double>("TM.Purge.Mfc60Default2");

            _slowFastVentSwitchPressure = SC.GetValue<double>("TM.Purge.SlowFastVentSwitchPressure");
            _slowVentTimeout = SC.GetValue<int>("TM.Purge.SlowVentTimeout");
            _fastVentTime = SC.GetValue<int>("TM.Purge.FastVentTimeout");

            ModuleName[] modules = new ModuleName[] { ModuleName.LoadLock, ModuleName.PM1 };
            foreach (var moduleName in modules)
            {
                if (!_tm.CheckSlitValveClose(moduleName))
                {
                    EV.PostAlarmLog(Module, $"Can not Purge, {moduleName} slit valve not closed");
                    return Result.FAIL;
                }
            }

            if (!_bufferLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not Purge,Buffer lid is not open");
                return Result.FAIL;
            }
            if (!_tmLid.Value)
            {
                EV.PostAlarmLog(Module, $"Can not Purge,TM lid is not open");
                return Result.FAIL;
            }
            if (_pumpType.IsAlarm)
            {
                EV.PostAlarmLog(Module, $"can not Purge,TM pump alarm");
                return Result.FAIL;
            }
            if (!_pumpType.IsRunning)
            {
                EV.PostAlarmLog(Module, $"can not Purge,TM pump is not running");
                return Result.FAIL;
            }
            if (!_tmIoInterLock.SetTMPurgeRoutineRunning(true, out var reason))
            {
                EV.PostAlarmLog(Module, $"can not Purge,{reason}");
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

                Loop((int)RoutineStep.StartLoop, _purgeCount);
                {
                    //关闭V77
                    CloseTMVent((int)RoutineStep.CloseV77);

                    //关闭V80
                    //CloseBufferVent((int)RoutineStep.CloseV80);

                    //打开V82,等待压力低于200mbar(可配置)
                    OpenSlowPump((int)RoutineStep.SlowPump, _tm, _slowFastPumpSwitchPressure, _pumpTimeOut);

                    //打开V81,压力达到0mbar(可配置)
                    OpenFastPump((int)RoutineStep.FastPump, _tm, _pumpBasePressure, _pumpTimeOut);

                    //等待5s(可配置)
                    TimeDelay((int)RoutineStep.VaccumDelay, _pumpDelayTime);

                    //关闭
                    CloseFastPump((int)RoutineStep.CloseValves, _tm);
                    
                    //关闭V82
                    CloseSlowPump((int)RoutineStep.CloseV82);

                    TimeDelay((int)RoutineStep.Delay1, 2);

                    //设定MFC60慢充流量
                    SetMFCToSetPoint((int)RoutineStep.SetMFC, _mfc60, _mfc60Default1);

                    //打开V77,等待压力大于200mbar
                    OpenSlowVent((int)RoutineStep.OpenSlowVent, _tm, _slowFastVentSwitchPressure, _slowVentTimeout);

                    //设定MFC60快充流量
                    SetMFCToSetPoint((int)RoutineStep.SetMFC, _mfc60, _mfc60Default2);

                    OpenFastVent((int)RoutineStep.OpenFastVent, _tm, _ventBasePressure, _fastVentTime);
                    
                    //关闭V77
                    CloseSlowVentValve((int)RoutineStep.CloseSlowVent, _tm);
                }

                EndLoop((int)RoutineStep.StopLoop);

                TimeDelay((int)RoutineStep.DelayFinal, 3);
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

            _tmIoInterLock.DoTmCyclePurgeRoutineRunning = false;

            return Result.DONE;
        }

        public override void Abort()
        {
            _tmIoInterLock.DoTmCyclePurgeRoutineRunning = false;
            //_pumpRoutine.Abort();
            //_ventRoutine.Abort();

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
