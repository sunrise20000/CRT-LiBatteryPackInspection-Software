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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Comet
{
    public class CometRFMatch : SerialPortDevice, IConnection
    {
        public string Address { get { return _address; } }

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

        private CometRFMatchConnection _connection;
        public CometRFMatchConnection Connection
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
        public CometRFMatch(string module, string name, string scRoot, string portName) : base(module, name)
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
            _connection = new CometRFMatchConnection(PortName);
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
 
            _connection = new CometRFMatchConnection(portName, bautRate, dataBits, parity, stopBits);
 
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
                            //_lstHandler.AddLast(new CommetRFMatchQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new CommetRFMatchSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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

        internal void NoteNAK(ResponseNAK nak)
        {
            ResponseNAK = nak;
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

        public void PerformRawCommand(string command, string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchRawCommandHandler(this, command, completeEvent, comandArgument));
            }
        }


        public void TriggerFullRefRun()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "10,00", "10,01", "FullRefRunCompleted"));
            }
        }
        public void TriggerGotoCap()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "20,00", "20,01", "GotoCapCompleted"));
            }
        }

        public void TriggerGotoFullStepPos()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "21,00", "21,01", "GotoFullStepPosCompleted"));
            }
        }
        public void TriggerMoveFullSteps()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "22,00", "22,01", "MoveFullStepsCompleted"));
            }
        }
        public void TriggerGotoCmin()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "23,00", "23,01", "GotoCminCompleted"));
            }
        }
        public void TriggerGotoCmax()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "24,00", "24,01", "GotoCmaxCompleted"));
            }
        }
        public void TriggerGotoMicroStepPos()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "25,00", "25,01", "GotoMicroStepPosCompleted"));
            }
        }
        public void TriggerMoveMicroSteps()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "26,00", "26,01", "MoveMicroStepsCompleted"));
            }
        }
        public void TriggerGotoStoredPos()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerHandler(this, "27,00", "27,01", "GotoStoredPosCompleted"));
            }
        }

        public void SetAccelerationSpeedIndex(byte acceleration, byte speed)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "43,00", acceleration.ToString("X2") + "," + speed.ToString("X2")));
            }
        }
        public void SetAccelerationSpeedIndex(string acceleration, string speed)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "43,00", acceleration + "," + speed));
            }
        }

        public void SetCurrent(byte idleCurrent, byte driveCurrent)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "45,00", idleCurrent.ToString("X2") + "," + driveCurrent.ToString("X2")));
            }
        }
        public void SetCurrent(string idleCurrent, string driveCurrent)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "45,00", idleCurrent + "," + driveCurrent));
            }
        }

        public void SetLowerCustomerLimit(short lowerLimit)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "72,01", lowerLimit));
            }
        }
        public void SetLowerCustomerLimit(string lowerLimit)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "72,01", short.Parse(lowerLimit)));
            }
        }

        public void SetUpperCustomerLimit(short upperLimit)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "72,02", upperLimit));
            }
        }
        public void SetUpperCustomerLimit(string upperLimit)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "72,02", short.Parse(upperLimit)));
            }
        }

        public void StoreIndexedPosition(byte index, short positon)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "75,00", index, positon));
            }
        }
        public void StoreIndexedPosition(string index, string positon)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetHandler(this, "75,00", byte.Parse(index), short.Parse(positon)));
            }
        }


        public double ActualCap { get; set; }
        public void GetActualCap(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ActualCap";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetDoubleValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetDoubleValueHandler(this, "40,01", "41,01", propName, 0.1));
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

        public short ActualFullStepPos { get; set; }
        public void GetActualFullStepPos(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ActualFullStepPos";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetShortValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetShortValueHandler(this, "40,02", "41,02", propName));
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

        public double MinimumCap { get; set; }
        public void GetMinimumCap(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "MinimumCap";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetDoubleValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetDoubleValueHandler(this, "40,10", "41,10", propName, 0.1));
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
        public double MaximumCap { get; set; }
        public void GetMaximumCap(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "MaximumCap";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetDoubleValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetDoubleValueHandler(this, "40,11", "41,11", propName, 0.1));
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

        public short MinimumFullStepPos { get; set; }
        public void GetMinimumFullStepPos(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "MinimumFullStepPos";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetShortValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetShortValueHandler(this, "40,12", "41,12", propName));
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
        public short MaximumFullStepPos { get; set; }
        public void GetMaximumFullStepPos(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "MaximumFullStepPos";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetShortValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetShortValueHandler(this, "40,13", "41,13", propName));
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

        //parse
        public short AccelerationSpeed { get; set; }
        public void GetAccelerationSpeed(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "AccelerationSpeed";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetShortValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetShortValueHandler(this, "40,21", "41,21", propName));
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
        //parse
        public int Status { get; set; }
        public void GetStatus(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "Status";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetDoubleValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetShortValueHandler(this, "40,22", "41,22", propName));
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
        //need special handler ??
        public short CcurveIndexedFullStepCap { get; set; }
        public void GetCcurveIndexedFullStepCap(bool isSelected, int tableIndex)
        {
            lock (_locker)
            {
                string propName = "CcurveIndexedFullStepCap";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetShortValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetShortValueHandler(this, "40,30", "41,30", propName));
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
        public byte ControllerTemp { get; set; }
        public void GetControllerTemp(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ControllerTemp";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetByteValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetByteValueHandler(this, "40,32", "41,32", propName));
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
        public uint MicroStepPos { get; set; }
        public void GetMicroStepPos(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "MicroStepPos";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetIntValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetIntValueHandler(this, "40,36", "41,36", propName));
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
        public double CminNom { get; set; }
        public void GetCminNom(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "CminNom";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetDoubleValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetDoubleValueHandler(this, "40,70", "41,70", propName, 0.1));
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
        public double CmaxNom { get; set; }
        public void GetCmaxNom(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "CmaxNom";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetDoubleValueHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetDoubleValueHandler(this, "40,11", "41,11", propName, 0.1));
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


        public double LowerCustomerLimit { get; set; }
        public void GetLowerCustomerLimit(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "LowerCustomerLimit";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetLimitHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetLimitHandler(this, "40,72", "41,72", propName,"01"));
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
        public double UpperCustomerLimit { get; set; }
        public void GetUpperCustomerLimit(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "UpperCustomerLimit";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetLimitHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetLimitHandler(this, "40,72", "41,72", propName, "02"));
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
        public double LowerFactoryLimit { get; set; }
        public void GetLowerFactoryLimit(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "LowerFactoryLimit";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetLimitHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetLimitHandler(this, "40,72", "41,72", propName, "03"));
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
        public double UpperFactoryLimit { get; set; }
        public void GetUpperFactoryLimit(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "UpperFactoryLimit";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetLimitHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetLimitHandler(this, "40,72", "41,72", propName, "04"));
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

        public double PresetCap { get; set; }
        public void GetPresetCap(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "PresetCap";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetLimitHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetLimitHandler(this, "40,73", "41,73", propName, "00"));
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

        public short PresetFullStepPos { get; set; }
        public void GetPresetFullStepPos(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "PresetFullStepPos";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGet3ByteHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGet3ByteHandler(this, "40,73", "41,73", propName, "01"));
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


        public short StoredFullStepPos { get; set; }
        public void GetStoredFullStepPos(bool isSelected, int index)
        {
            lock (_locker)
            {
                string propName = "PresetFullStepPos";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGet3ByteHandler) && ((CometRFMatchHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGet3ByteHandler(this, "40,75", "41,75", propName, index.ToString("X2")));
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

        public void MonitorRawCommand(bool isSelected, string command, string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchRawCommandHandler) && ((CometRFMatchHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchRawCommandHandler(this, command, completeEvent, comandArgument));
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


        public ResponseNAK ResponseNAK { get; private set; }
        public bool FullRefRunCompleted { get; private set; }
        public bool GotoCapCompleted { get; private set; }
        public bool GotoFullStepPosCompleted { get; private set; }
        public bool MoveFullStepsCompleted { get; private set; }
        public bool GotoCminCompleted { get; private set; }
        public bool GotoCmaxCompleted { get; private set; }
        public bool GotoMicroStepPosCompleted { get; private set; }
        public bool MoveMicroStepsCompleted { get; private set; }
        public bool GotoStoredPosCompleted { get; private set; }

        public byte Acceleration { get; private set; }
        public byte SpeedIndex { get; private set; }
        public bool SetCommandCompleted { get; private set; }


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
        internal void NoteSetComplted()
        {
            SetCommandCompleted = true;
        }


        #endregion
    }

    public enum ResponseNAK
    { 
        NotNak,
        CommandNotDefined,
        CheckSumFailed,
        PositionOverLimit,
        IndexOutOfRange
    }




}
