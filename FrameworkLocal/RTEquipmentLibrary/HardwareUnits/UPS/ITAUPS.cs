using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS
{
   public class ITAUPS : BaseDevice, IConnection,IDevice
    {
        private ITAUPSConnection _connection;
        private bool _activeMonitorStatus;
        private int _errorCode;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigPowerAlarm = new R_TRIG();
        private R_TRIG _trigLowerBatteryAlarm = new R_TRIG();
        private PeriodicJob _thread;
        private int tempCount = 1;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;

        private string _systemStatus;
        private float _inputVoltage;
        private float _batteryVoltage;
        private float _batteryRemainsTime;
        public string SystemStatus
        {
            get { return _systemStatus; }
            set { _systemStatus = value; }
        }
        public float InputVoltage
        {
            get { return _inputVoltage; }
            set { _inputVoltage = value; }
        }
        public float BatteryVoltage
        {
            get { return _batteryVoltage; }
            set { _batteryVoltage = value; }
        }
        public float BatteryRemainsTime
        {
            get { return _batteryRemainsTime; }
            set { _batteryRemainsTime = value; }
        }
        public bool UPSPowerAlarm { get; set; }
        public bool UPSLowerBatteryAlarm { get; set; }

        public string Address
        {
            get; set;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected;
            }
        }


        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        //public DOAccessor _batteryLowVoltage;
        //public DOAccessor _upsEnable;
        public ITAUPS(string module, string name, string scRoot, DOAccessor BatteryLowVoltage,DOAccessor UpsEnable) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
            //_batteryLowVoltage = BatteryLowVoltage;
            //_upsEnable = UpsEnable;
        }

        ~ITAUPS()
        {
            _connection.Disconnect();

        }


        public void QueryOids()
        {
            //UPS二代
            foreach(var oid in Oids)
            {
                if (oid.Key != null)
                {
                    _lstHandler.AddLast(new ITAUPSGetHandler(this, oid.Key, "Get", oid.Value));
                }
            }
            if (SystemStatus == "Critical")
            {
                _lstHandler.AddLast(new ITAUPSGetBulkHandler(this, "AlarmTrap", "GetBulk", "1.3.6.1.4.1.13400.2.54.3.1.6"));
            }
           
        }

        public Dictionary<string, string> Oids = new Dictionary<string, string>()
        {
            { "SystemStatus",".1.3.6.1.4.1.13400.2.54.2.1.1.0"},
            { "InputVoltage",".1.3.6.1.4.1.13400.2.54.2.2.1.0"},
            { "BatteryVoltage",".1.3.6.1.4.1.13400.2.54.2.5.1.0"},
            { "BatteryRemainsTime",".1.3.6.1.4.1.13400.2.54.2.5.7.0"},
            { "upsOutputSource",".1.3.6.1.4.1.13400.2.54.2.1.2.0"},
        };

        public void ResetDevice()
        {

        }

        public void QueryError()
        {
            EV.PostInfoLog(Module, "Query error");
        }

        public bool Initialize()
        {
            //string portName = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            string portName = SC.GetStringValue($"{Name}.Address");
            Address = portName;
            int address = SC.GetValue<int>($"{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");
            _connection = new ITAUPSConnection(portName);
            _connection.EnableLog(_enableLog);

            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;
            int retry = 0;
            do
            {
                _connection.Disconnect();
                Thread.Sleep(sleep * 1000);
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Name} connected");
                    break;
                }

                if (count > 0 && retry++ > count)
                {
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }

                Thread.Sleep(sleep * 1000);
                LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

            } while (true);

            _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);
          
            DATA.Subscribe($"{Module}.{Name}.SystemStatus", () => _systemStatus);
            DATA.Subscribe($"{Module}.{Name}.InputVoltage", () => _inputVoltage/10);
            DATA.Subscribe($"{Module}.{Name}.BatteryVoltage", () => _batteryVoltage/10);
            DATA.Subscribe($"{Module}.{Name}.BatteryRemainsTime", () => _batteryRemainsTime/10);

            DATA.Subscribe($"{Module}.{Name}.UtilityPowerFailure", () => _trigUtilityPowerFailure.CLK);
            DATA.Subscribe($"{Module}.{Name}.BatteryUnderVoltage", () => _trigBatteryUnderVoltage.CLK);


            _systemStatus = "Normal";
            //_inputVoltage = 11;
            _batteryVoltage = 200;            //_batteryRemainsTime = 33;
            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }
        public int _connectTimes { get; set; }
        private bool OnTimer()
        {
            try
            {
             
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {

                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    //UPSPowerAlarm = true;
                    //UPSLowerBatteryAlarm = true;



                    //_trigRetryConnect.CLK = !_connection.IsConnected;
                    //if (_trigRetryConnect.Q)
                    //{
                    //    //_connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                    //   // _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                    //    if (!_connection.Connect())
                    //    {
                    //        EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                    //    }
                    //}
                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //UPSPowerAlarm = false ;
                            //UPSLowerBatteryAlarm = false ;

                            EV.PostAlarmLog(Module, $"Connected with {_connection.Address}, {Module}.{Name}");
                        }
                    }
                    if (_connectTimes++ < 3)
                        _connection.ForceClear();
                    else _connectTimes = 4;
                    return true;

                }

                HandlerBase handler = null;
                //if (!_connection.IsBusy)
                //{
                lock (_locker)
                {
                    if (_lstHandler.Count == 0)
                        QueryOids();
                    if (_lstHandler.Count > 0 && !_connection.IsBusy)
                    {
                        handler = _lstHandler.First.Value;
                        _lstHandler.RemoveFirst();
                        if (handler != null)
                        {
                            _connection.Execute(handler);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        internal void NoteError()
        {

        }

        public void Monitor()
        {
            try
            {

                if(BatteryVoltage>=200)
                {
                    UPSLowerBatteryAlarm = false;
                }
                else
                {
                    if (_connection.IsConnected && !_connection.IsCommunicationError)
                    {
                        UPSLowerBatteryAlarm = true;
                    }
                }
                _trigLowerBatteryAlarm.CLK = UPSLowerBatteryAlarm;
                if(_trigLowerBatteryAlarm.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} LowerBattery Alarm.");
                }
                _trigPowerAlarm.CLK = UPSPowerAlarm;
                if(_trigPowerAlarm.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Power Alarm.");
                }
                //
                _connection.EnableLog(_enableLog);
                if (_connectTimes < 4) return;
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    //UPSPowerAlarm = true;
                    //UPSLowerBatteryAlarm = true;

                    _inputVoltage = 0;
                    _batteryVoltage = 0;
                    _batteryRemainsTime = 0;
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Reset()
        {
            _connectTimes = 0;
            _connection.SetCommunicationError(false, "");           
                        
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");
            _trigRetryConnect.RST = true;
            _trigUtilityPowerFailure.RST = true;
            _trigBatteryUnderVoltage.RST = true;
            _trigCommunicationError.RST = true;
            _trigPowerAlarm.RST = true;
            _trigLowerBatteryAlarm.RST = true;
            //
            

        }
        public void PraseSystemStatus(string value)
        {
            
            if (value == "0")
            {
                SystemStatus = "Normal";
                //if (UPSLowerBatteryAlarm) 
                //    UPSLowerBatteryAlarm = false;
                //if (UPSPowerAlarm) 
                //    UPSPowerAlarm = false;
            }
            else if (value == "1")
            {
                SystemStatus = "Warning";
                //if (UPSLowerBatteryAlarm) 
                //    UPSLowerBatteryAlarm = false;
                //if (UPSPowerAlarm)
                //    UPSPowerAlarm = false;
            }
            else if (value == "2")
            {
                if (UPSLowerBatteryAlarm)
                {
                    SystemStatus = "LowerBattery";
                }
                else if (UPSPowerAlarm)
                {
                    SystemStatus = "Power";
                }
                else
                {
                    SystemStatus = "Critical";
                }
            }
        }
        public void ParseOutputSource(string value)
        {

            if (value == "0") //UPS No Output 
            {
                UPSPowerAlarm = true;
            }
            else if (value == "1") // UPS On Main
            {
                UPSPowerAlarm = false;
            }
            else if (value == "2") //UPS On Battery
            {
                UPSPowerAlarm = true;
            }
        }


        public R_TRIG _trigUtilityPowerFailure = new R_TRIG();
        public R_TRIG _trigBatteryUnderVoltage = new R_TRIG();
        public void UtilityPowerAlarm(bool alarm)
        {
            UPSPowerAlarm = alarm;
           // _trigUtilityPowerFailure.CLK = alarm;
           //if (_trigUtilityPowerFailure.Q)
           //{
           //     //_upsEnable.SetValue(false, out _);
           //     EV.PostAlarmLog(Name, $"UPS Utility Power Failure.");
           //}
        }
        public void BatteryUnderVoltage(bool alarm)
        {
            UPSLowerBatteryAlarm = alarm;
            //_trigBatteryUnderVoltage.CLK = alarm;
            //if (_trigBatteryUnderVoltage.Q)
            //{
            //    _batteryLowVoltage.SetValue(false, out _);
            //    EV.PostAlarmLog(Name, $"UPS Battery Under Voltage.");
            //}
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }

        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }

        public void Terminate()
        {
            _connection.Disconnect();
        }

    }
}

