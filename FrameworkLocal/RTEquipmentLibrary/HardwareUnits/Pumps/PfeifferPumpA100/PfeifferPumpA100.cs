using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA100
{
    public class PfeifferPumpA100  : SerialPortDevice, IConnection
    {
        public string Address { get { return _address; }}
        public bool IsConnected { get; } 
        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public string PortStatus { get; set; } = "Closed";

        private PfeifferPumpA100Connection _connection;
        public PfeifferPumpA100Connection Connection
        {
            get { return _connection; }
        }
 
        private R_TRIG _trigError = new R_TRIG();
 
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        private bool _enableLog;
        private string _address;
        private string _scRoot;
        public PfeifferPumpA100(string module, string name, string scRoot, string portName) : base(module, name)
        {
            _scRoot = scRoot;
            PortName = portName;
        }
        private void ResetPropertiesAndResponses()
        {

            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        public override bool Initialize(string portName)
        {
            base.Initialize(portName);

            ResetPropertiesAndResponses();

            if (_connection != null && _connection.IsConnected && PortName == portName)
                return true;

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            PortName = portName;

            _address = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            _connection = new PfeifferPumpA100Connection(PortName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
 
            return true;
        }

        public bool InitConnection(string portName, int bautRate, int dataBits, Parity parity, StopBits stopBits)
        {
 
            _connection = new PfeifferPumpA100Connection(portName, bautRate, dataBits, parity, stopBits);
 
            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        internal void NoteSetParaCompleted()
        {
            SetParaCompleted = true;
        }

        private bool OnTimer()
        {
            try
            {
                //_connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new PfeifferPumpA100QueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new PfeifferPumpA100SetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }

                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }


        public override void Monitor()
        {
            try
            {
                //_connection.EnableLog(_enableLog);
 
 
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            
        }


        public override void Reset()
        {
            _trigError.RST = true;
 
            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        public override bool Home(out string reason)
        {
            return base.Home(out reason);
        }


        #region Command Functions

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100RawCommandHandler(this, command, null));
            }
        }
        public void PerformRawCommand(string command, string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100RawCommandHandler(this, command, parameter));
            }
        }


        public void EchoOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100SimpleSwitchHandler(this, "ECH", parameter));
            }
        }
        public void PumpOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100SimpleSwitchHandler(this, "SYS", parameter));
            }
        }
        public void SetPumpParameter(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpSetPumpParameterHandler(this, parameter));
            }
        }
        public void SetFullOverhaullAlert(string alertValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpSetPumpParameterHandler(this, "070" + alertValue));
            }
        }
        public void SetFullOverhaullAlarm(string alarmValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpSetPumpParameterHandler(this, "071" + alarmValue));
            }
        }


        internal void NoteSwitchCompleted(string command, bool value)
        {
            if(command== "ECH")
                SetEchoOnOff = value;
            else if (command == "SYS")
                SetPumpOnOff = value;
        }


        public void MonitorRawCommand(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(PfeifferPumpA100RawCommandHandler) && ((PfeifferPumpA100Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new PfeifferPumpA100RawCommandHandler(this, command, null));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }


        public void ReadPumpStatus(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(PfeifferPumpA100ReadPumpStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new PfeifferPumpA100ReadPumpStatusHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }
        public void ReadSetState(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(PfeifferPumpA100ReadSetStateHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new PfeifferPumpA100ReadSetStateHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }

        #endregion

        #region Properties
        public string Error { get; private set; }

        public bool SetEchoOnOff { get; private set; }
        public bool SetPumpOnOff { get; private set; }
        public bool SetParaCompleted { get; private set; }

        public PumpRunStatus PumpRunStatus { get; private set; }
        public ControlMode ManageMode { get; private set; }
        public DefectResult FrequencyControlDefect { get; private set; }
        public DefectResult MotorTempDefect { get; private set; }
        public DefectResult MaintenanceDefect { get; private set; }
        public string SFullOverhaull { get; private set; }

        internal void NotePumpStatus(string data)
        {
            try
            {
                var statusArray = data.ToArray();
                PumpRunStatus = (PumpRunStatus)int.Parse(statusArray[0].ToString());
                var management = int.Parse(statusArray[10].ToString());
                int manageModeValue = management & 1;
                ManageMode = (ControlMode)manageModeValue;

                FrequencyControlDefect = (DefectResult)int.Parse(statusArray[48].ToString());
                MotorTempDefect = (DefectResult)int.Parse(statusArray[51].ToString());
                MaintenanceDefect = (DefectResult)int.Parse(statusArray[55].ToString());
            }
            catch (Exception ex)
            {
                NoteError("Invalid Pump Status");
                LOG.Error($"Invalid Pump Status: {data}, Exception info: {ex}");
            }

        }

        internal void NoteReadSetState(string data)
        {
            try
            {
                SFullOverhaull = data.Substring(65, 5);
            }
            catch (Exception ex)
            {
                NoteError("Invalid Pump Status");
                LOG.Error($"Invalid Pump Status: {data}, Exception info: {ex}");
            }

        }
        

        private int Str2Int(string sValue)
        {
            if (string.IsNullOrEmpty(sValue))
                throw new Exception("empty int value");
            if(sValue.Length > 1)
            {
                sValue = sValue.TrimStart('0');
            }
            return int.Parse(sValue);
        }


        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();



        public void NoteError(string reason)
        {
            if (reason != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
            }
            else
            {
                Error = null;
            }
        }


        internal void NoteRawCommandInfo(string command, string data)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommand == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion
    }

    public enum PumpRunStatus
    {
        Stopped,
        Running
    }
    public enum ValveStatus
    {
        Closed,
        Opened
    }
    public enum PumpLLStatus
    {
        Off,
        On
    }
    public enum ControlMode
    {
        Local,
        Remote
    }
    public enum DefectResult
    {
        OK,
        Warning,
        Hazard
    }


}
