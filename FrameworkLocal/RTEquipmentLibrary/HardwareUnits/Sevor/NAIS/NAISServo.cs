using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Servo.NAIS
{
    public class NAISServo : BaseDevice, IConnection, IDevice
    {
        private NAISServoConnection _connection;

        private bool _isOn;

        private bool _activeMonitorStatus;

        private int _errorCode;

        private RD_TRIG _trigPumpOnOff = new RD_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigOverTemp = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();
        private string _lastError = string.Empty;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;
        public bool Status { get; set; }

        private string[] StatusStringList = new string[] { "normal", "running fault", "stop fault" };

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
        public bool AlarmStatus { get; set; }
        public bool PositionComplete { get; set; }
        public bool MotorBusy { get; set; }

        public bool IsBusy { get; set; }
        public bool IsStbOff { get; set; } //StbOff消息是否发送

        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public NAISServo(string module, string scRoot, string name) : base(module, name, name, name)
        {

            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        public void Query()
        { 
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoQueryHandler(this, "QueryAlarm", 0x01, 0x01, 0x00, 0xA1, 0x00, 0x01));
                _lstHandler.AddLast(new NAISServoQueryHandler(this, "QueryPosition", 0x01, 0x01, 0x00, 0xA2, 0x00, 0x01));
                _lstHandler.AddLast(new NAISServoQueryHandler(this, "QueryMotorStatus", 0x01, 0x01, 0x01, 0x40, 0x00, 0x01));
            }
        }
        public void QueryPosition()
        {
            _lstHandler.AddLast(new NAISServoSetHandler(this, "QueryPosition", 0x01, 0x01, 0x00, 0xA2, 0x00, 0x01));
        }
        public void QueryMotorStatus()
        {
            _lstHandler.AddLast(new NAISServoSetHandler(this, "QueryMotorStatus", 0x01, 0x01, 0x01,0x40, 0x00, 0x01));
        }
        public void SetServoOn()
        {
            IsBusy = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoSetHandler(this, "Set Servo On", 0x01, 0x05, 0x00, 0x60, 0xff, 0x00));
            }
        }
        public void SetBlockHome()
        {
            IsBusy = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoSetHandler(this, "Set BlockNo High", 0x01, 0x06, 0x44, 0x14, 0x00, 0x00));
            }
        }
        public void SetBlockNoHigh()
        {
            IsStbOff = false;
            IsBusy = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoSetHandler(this, "Set BlockNo High", 0x01, 0x06, 0x44, 0x14, 0x00, 0x01));
            }
        }
        public void SetBlockNoLow()
        {
            IsStbOff = false;
            IsBusy = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoSetHandler(this, "Set BlockNo Low", 0x01, 0x06, 0x44, 0x14, 0x00, 0x02));
            }
        }
        public void SetStbOn()
        {
            IsStbOff = false;
            IsBusy = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoSetHandler(this, "Set Stb On", 0x01, 0x05, 0x01, 0x20, 0xff, 0x00));
            }
        }
        public void SetStbOff()
        {
            IsStbOff = true;
            IsBusy = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                _lstHandler.AddLast(new NAISServoSetHandler(this, "Set Stb Off", 0x01, 0x05, 0x01, 0x20, 0x00, 0x00));
            }
        }

        public void ClearOff()
        {
            _lstHandler.AddLast(new NAISServoSetHandler(this, "Clear Error Off", 0x01, 0x05, 0x00, 0x61, 0x00, 0x00));
        }

        public void ClearOn()
        {
            _lstHandler.AddLast(new NAISServoSetHandler(this, "Clear Error On", 0x01, 0x05, 0x00, 0x61, 0xFF, 0x00));
        }


        protected bool fAbort(object[] param)
        {
            lock (_locker)
            {
                IsBusy = false;
                _connection.ForceClear();
                _lstHandler.Clear();
            }
            return true;
        }
        public bool Initialize()
        {
            // string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            string portName = SC.GetStringValue($"{Name}.Address");
            Address = portName;
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new NAISServoConnection(portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            
            _thread = new PeriodicJob(1000, OnTimer, $"{Name} MonitorHandler", true);

            OP.Subscribe($"{Module}.{Name}.SetServoOn", (cmd, param) =>
            {
                Set("SetServoOn");
                EV.PostInfoLog(Module, $"{Name}Set Servo On.");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetBlockHome", (cmd, param) =>
            {
                Set("SetBlockHome");
                EV.PostInfoLog(Module, $"{Name}Set Block Home.");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetBlockUp", (cmd, param) =>
            {
                Set("SetBlockUp");
                EV.PostInfoLog(Module, $"{Name}Set Block No High.");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetBlockLow", (cmd, param) =>
            {
                Set("SetBlockLow");
                EV.PostInfoLog(Module, $"{Name}Set Block No Low.");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetStbOn", (cmd, param) =>
            {
                Set("SetStbOn");
                EV.PostInfoLog(Module, $"{Name}Set Stb On.");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetStbOff", (cmd, param) =>
            {
                Set("SetStbOff");
                EV.PostInfoLog(Module, $"{Name}Set Stb Off.");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.ClearOn", (cmd, param) =>
            {
                ClearOn();
                //Set("SetStbOff");
                EV.PostInfoLog(Module, $"{Name}Clear Error On.");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetAbort", (cmd, param) =>
            {
                fAbort(param);
                return true;
            });

            DATA.Subscribe($"{Module}.{Name}.AlarmStatus", () => AlarmStatus);
            DATA.Subscribe($"{Module}.{Name}.PositionComplete", () => PositionComplete);
            DATA.Subscribe($"{Module}.{Name}.MotorBusy", () => MotorBusy);
            ConnectionManager.Instance.Subscribe($"{Name}", this);
            //SetServoOn();
            return true;
        }
        public R_TRIG _trigSevroOn = new R_TRIG();
        public int _connecteTimes { get; set; }
        private bool OnTimer()
        {
            try
            {          
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _trigSevroOn.RST = true;
                        IsBusy = false;
                        _lstHandler.Clear();  
                    }
                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    //_connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                    // _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        //_connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }
                    if (_connecteTimes++ < 3)
                        _connection.ForceClear();
                    else _connecteTimes = 4;
                   // if(_connection.IsConnected && !_connection.IsCommunicationError) SetServoOn();
                    return true;
                }
                _trigSevroOn.CLK = true;
                if (_trigSevroOn.Q) 
                    SetServoOn();
                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0 && _activeMonitorStatus &&!IsBusy)
                        {
                            Query();
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
                return true;

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
                _connection.EnableLog(_enableLog);
                if (_connecteTimes < 4) return;
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

        public void Terminate()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Disconnect();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Reset()
        {
            _connecteTimes = 0;
            _trigError.RST = true;
            _trigOverTemp.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;
            
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }


        internal void NoteOnOff(bool isOn)
        {
            _isOn = isOn;
        }
        public R_TRIG _trigAlarm = new R_TRIG();
        public void PraseData(string name, byte[] buffer)
        {
            switch (name)
            {

                case "QueryAlarm":
                    {
                        AlarmStatus = buffer[3] == 0 ? false : true;
                        _trigAlarm.CLK = AlarmStatus;
                        if (_trigAlarm.Q)
                        {
                            EV.PostAlarmLog("PM","Servo Is Error State.");
                        }
                    }
                    break;
                case "QueryPosition":
                    {
                        PositionComplete = buffer[3] == 0 ? false : true;
                    }
                    break;
                case "QueryMotorStatus":
                    {
                        MotorBusy = buffer[3] == 0 ? false : true;
                    }
                    break;

            }
        }

        public void Set(string name)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if (_connection.IsBusy) _connection.ForceClear();
                List<byte> cmddata=null;
                switch (name)
                {
                    case "SetServoOn":
                            cmddata = new List<byte>() { 0x01, 0x05, 0x00, 0x60, 0xff, 0x00};    
                        break;
                    case "SetBlockHome":
                            cmddata = new List<byte>() { 0x01, 0x06, 0x44, 0x14, 0x00, 0x00 };
                        break;
                    case "SetBlockUp":
                            cmddata = new List<byte>() { 0x01, 0x06, 0x44, 0x14, 0x00, 0x01 };   
                        break;                   
                    case "SetBlockLow":
                            cmddata = new List<byte>() { 0x01, 0x06, 0x44, 0x14, 0x00, 0x02 };
                        break;
        
                    case "SetStbOn":
                            cmddata = new List<byte>() { 0x01, 0x05, 0x01, 0x20, 0xff, 0x00 };
                        break;
                    case "SetStbOff":
                            cmddata = new List<byte>() { 0x01, 0x05, 0x01, 0x20, 0x00, 0x00 }; 
                        break;
                }
               
                byte[] checksum = ModRTU_CRC(cmddata.ToArray());
                cmddata.Add(checksum[0]);
                cmddata.Add(checksum[1]);

                _connection.SendMessage(cmddata.ToArray());
            }
        }
        

        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }


        public void SetError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
            }
        }
        private static byte[] ModRTU_CRC(byte[] buffer)
        {
            ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;

            for (var pos = 0; pos < len; pos++)
            {
                crc ^= buf[pos]; // XOR byte into least sig. byte of crc

                for (var i = 8; i != 0; i--)
                    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {
                        // If the LSB is set
                        crc >>= 1; // Shift right and XOR 0xA001
                        crc ^= 0xA001;
                    }
                    else // Else LSB is not set
                    {
                        crc >>= 1; // Just shift right
                    }
            }

            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return BitConverter.GetBytes(crc);

        }


        private bool setDown = true;
        private void SetServoUpOrDown()
        { 
            
        }
    }
}
