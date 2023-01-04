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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.VAT
{
    public class VATS651  : SerialPortDevice, IConnection
    {
        public string Address { get; }
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

        private VATS651Connection _connection;
        public VATS651Connection Connection
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
        private string _scRoot;
        public VATS651(string module, string name, string scRoot, string portName) : base(module, name)
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

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            _connection = new VATS651Connection(PortName);
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
 
            _connection = new VATS651Connection(portName, bautRate, dataBits, parity, stopBits);
 
            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
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
                            //_lstHandler.AddLast(new VATS651QueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new VATS651SetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651RawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651RawCommandHandler(this, command));
            }
        }

        public void CloseValve()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "C:"));
            }
        }
        public void OpenValve()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "O:"));
            }
        }
        public void Hold()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "H:"));
            }
        }
        public void SetPosition(string position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "R:", position));
            }
        }
        public void SetPressure(string pressure)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "S:", pressure));
            }
        }
        public void SetAcessMode(string mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "c:01", mode));
            }
        }
        public void SetValveConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:04", setVal));
            }
        }
        public void SetSensorConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:01", setVal));
            }
        }
        public void SetSensorScale(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:05", setVal));
            }
        }
        public void SetSensor1Linearization(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:17", setVal));
            }
        }
        public void SetSensor2Linearization(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:18", setVal));
            }
        }
        public void SetSensorAverage(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:19", setVal));
            }
        }
        public void SetCommRangeConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:21", setVal));
            }
        }
        public void SetInterfaceConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:20", setVal));
            }
        }
        public void SetValveSpeed(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "V:", setVal));
            }
        }
        public void SetPressureControllerMode(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:02Z00", setVal));
            }
        }
        public void SetPressureControllerConfig(string pressureController, string parameterNumber, string parameterValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:02", pressureController+ parameterNumber+ parameterValue));
            }
        }
        public void ResetError(string reset)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "c:82", reset));
            }
        }

        internal void NoteActionCompleted(string command)
        {
            if (command == "C:")
            {
                CloseValveCompleted = true;
            }
            else if (command == "O:")
            {
                OpenValveCompleted = true;
            }
            else if (command == "R:")
            {
                SetPositonCompleted = true;
            }
            else if (command == "S:")
            {
                SetPressureCompleted = true;
            }
            else if (command == "c:01")
            {
                SetAcessModeCompleted = true;
            }
            else if (command == "s:04")
            {
                SetValveConfigCompleted = true;
            }
            else if (command == "s:01")
            {
                SetSensorConfigCompleted = true;
            }
            else if (command == "s:05")
            {
                SetSensorScaleCompleted = true;
            }
            else if (command == "s:17")
            {
                SetSensor1LinearizationCompleted = true;
            }
            else if (command == "s:18")
            {
                SetSensor2LinearizationCompleted = true;
            }
            else if (command == "s:19")
            {
                SetSensorAverageCompleted = true;
            }
            else if (command == "s:21")
            {
                SetCommRangeConfigCompleted = true;
            }
            else if (command == "s:20")
            {
                SetInterfaceConfigCompleted = true;
            }
            else if (command == "V:")
            {
                SetValveSpeedCompleted = true;
            }
            else if (command == "c:82")
            {
                ResetErrorCompleted = true;
            }
            else if (command == "s:02Z00")
            {
                SetPressureControllerModeCompleted = true;
            }
            else if (command == "s:02")
            {
                SetPressureControllerConfigCompleted = true;
            }
        }


        internal void NoteQueryResult(string command, string queryResult)
        {
            if (command == "A:")
            {
                Position = queryResult;
            }
            else if (command == "P:")
            {
                Pressure = queryResult;
            }
            else if (command == "i:60")
            {
                Sensor1Offset = queryResult;
            }
            else if (command == "i:61")
            {
                Sensor2Offset = queryResult;
            }
            else if (command == "i:64")
            {
                Sensor1Reading = queryResult;
            }
            else if (command == "i:65")
            {
                Sensor2Reading = queryResult;
            }
            else if (command == "i:30")
            {
                DeviceStatus = queryResult;
            }
            else if (command == "i:50")
            {
                VATError = queryResult;
            }
            else if (command == "i:51")
            {
                VATWarning = queryResult;
            }
        }

        internal void NoteSetCompleted(string command, string parameter)
        {
            //if(command == "SWAA")
            //{
            //    AlignAngleSet = double.Parse(parameter);
            //}
            //else if(command == "SWS")
            //{
            //    WaferSizeSet = double.Parse(parameter);
            //}
        }

        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(VATS651RawCommandHandler) && ((VATS651Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new VATS651RawCommandHandler(this, command, comandArgument));
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
        public void MonitorRawCommand(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(VATS651RawCommandHandler) && ((VATS651Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new VATS651RawCommandHandler(this, command));
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


        public void RequestPositoin(bool isSelected)
        {
            SimpleRequest(isSelected, "A:");
        }
        public void RequestPressure(bool isSelected)
        {
            SimpleRequest(isSelected, "P:");
        }
        public void RequestSensor1Offset(bool isSelected)
        {
            SimpleRequest(isSelected, "i:60");
        }
        public void RequestSensor2Offset(bool isSelected)
        {
            SimpleRequest(isSelected, "i:61");
        }

        public void RequestSensor1Reading(bool isSelected)
        {
            SimpleRequest(isSelected, "i:64");
        }
        public void RequestSensor2Reading(bool isSelected)
        {
            SimpleRequest(isSelected, "i:65");
        }
        public void RequestDeviceStatus(bool isSelected)
        {
            SimpleRequest(isSelected, "i:30");
        }

        public void RequestError(bool isSelected)
        {
            SimpleRequest(isSelected, "i:50");
        }
        public void RequestWarnings(bool isSelected)
        {
            SimpleRequest(isSelected, "i:51");
        }

        private void SimpleRequest(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(VATS651SimpleQueryHandler) && ((VATS651SimpleQueryHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new VATS651SimpleQueryHandler(this, command));
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
        public bool SetPositonCompleted { get; private set; }
        public bool SetPressureCompleted { get; private set; }
        public bool SetAcessModeCompleted { get; private set; }
        public bool ResetErrorCompleted { get; private set; }
        public string Position { get; private set; }
        public bool CloseValveCompleted { get; private set; }
        public bool OpenValveCompleted { get; private set; }
        public string Pressure { get; private set; }
        public string Sensor1Offset { get; private set; }
        public string Sensor2Offset { get; private set; }
        public string Sensor1Reading { get; private set; }
        public string Sensor2Reading { get; private set; }
        public string DeviceStatus { get; private set; }
        public string VATError { get; private set; }
        public string VATWarning { get; private set; }
        public bool SetValveConfigCompleted { get; private set; }
        public bool SetSensorConfigCompleted { get; private set; }
        public bool SetPressureControllerModeCompleted { get; private set; }
        public bool SetPressureControllerConfigCompleted { get; private set; }
        public bool SetSensorScaleCompleted { get; private set; }
        public bool SetSensor1LinearizationCompleted { get; private set; }
        public bool SetSensor2LinearizationCompleted { get; private set; }
        public bool SetSensorAverageCompleted { get; private set; }
        public bool SetCommRangeConfigCompleted { get; private set; }
        public bool SetInterfaceConfigCompleted { get; private set; }
        public bool SetValveSpeedCompleted { get; private set; }

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
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion
    }



}
