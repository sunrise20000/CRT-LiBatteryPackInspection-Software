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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Vacuometer
{
    public class Vacuometer : BaseDevice, IConnection,IDevice
    {
        private VacuometerConnection _connection;
        private bool _activeMonitorStatus;
        private int _errorCode;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        private int tempCount = 1;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;

        public double Pressure { get; set; }

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

        public Vacuometer(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        ~Vacuometer()
        {
            _connection.Disconnect();

        }

        //public TempOmron(string module, string name, string scRoot) : base(module, name, name, name)
        //{
        //    //tempCount = count;
        //    _scRoot = scRoot;
        //    _activeMonitorStatus = true;
        //}

        public void QueryTemp()
        {

            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Actual1 Temp", 0x1, 0x2, 0x0, 0x0, 0x1));
            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Setting1 Temp", 0x1, 0x2, 0x40, 0x0, 0x1));

            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Actual2 Temp", 0x1, 0x04, 0x00, 0x00, 0x01));
            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Setting2 Temp", 0x1, 0x40, 0x40, 0x00, 0x01));

            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Actual3 Temp", 0x1, 0x60, 0x00, 0x00, 0x1));
            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Setting3 Temp", 0x1, 0x60, 0x40, 0x00, 0x1));

            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Actual4 Temp", 0x1, 0x80, 0x00, 0x00, 0x01));
            //_lstHandler.AddLast(new VacuometerQueryHandler(this, "Query Setting4 Temp", 0x1, 0x80, 0x40, 0x00, 0x01));

        }


        public void ResetDevice()
        {

        }

        public void QueryError()
        {
            EV.PostInfoLog(Module, "Query error");
        }

        public bool Initialize()
        {
            // string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            string portName = SC.GetStringValue($"{Name}.Address");
            Address = portName;
            //int address = SC.GetValue<int>($"{_scRoot}.{Name}.DeviceAddress");

            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new VacuometerConnection(portName);
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
           // _connection.Execute(new VacuometerReciveHandler(this, "Recive Data"));
           
            DATA.Subscribe($"{Module}.{Name}.PressureValue", () => Pressure);

            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }

        public double A { get; set; }
        public int B { get; set; }
        public void ReadData(byte[] buffer)
        {
            if (buffer[1] == 2) B = 32767;
            else  B = 32000;
            bool bit4  = ((Convert.ToInt16(buffer[2]) >> 4) & 0x01)==0;
            bool bit5 = ((Convert.ToInt16(buffer[2]) >> 5) & 0x01)==0;
            if (bit4 && bit5) A = 1.00;
            else if (!bit4 && bit5) A = 1.3332;
            else if(bit4&&!bit5) A = 133.32;
            var AnalogValue = BitConverter.ToInt16(new byte[] { buffer[5], buffer[4]},0);
            Pressure =((AnalogValue* 1.3332)/B) * 1.0 * 1000;
        }
        public R_TRIG _trigCreateReciveHandler = new R_TRIG();
        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _trigCreateReciveHandler.RST = true;
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        //_connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                        //_connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }
                    //_connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                    //if (_trigRetryConnect.CLK)
                    //{
                    //    if (!_connection.Connect())
                    //    {
                    //        if (_trigRetryConnect.Q)
                    //        {

                    //            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        EV.PostAlarmLog(Module, $"Retry connect succee with {_connection.Address}, {Module}.{Name}");
                    //        _trigRetryConnect.CLK = false;
                    //    }
                    //    Thread.Sleep(3000);
                    //}
                    //_connection.ForceClear();
                    //if(_connection.IsConnected && !_connection.IsCommunicationError)
                    //_connection.Execute(new VacuometerReciveHandler(this, "Recive Data"));
                    return true;

                }
                _trigCreateReciveHandler.CLK = true;
                if (_trigCreateReciveHandler.Q)
                    _connection.Execute(new VacuometerReciveHandler(this, "ReciveDataHandler"));
                HandlerBase handler = null;
                //if (!_connection.IsBusy)
                //{
                lock (_locker)
                {
                    if (_lstHandler.Count == 0)
                       // QueryTemp();
                    if ((_lstHandler.Count > 0 && !_connection.IsBusy))
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
                _connection.EnableLog(_enableLog);
                
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    Pressure = 0.0;
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
            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            //_connection.ForceClear();
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");
            _trigRetryConnect.RST = true;
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
        public Dictionary<string, double> dicUnit = new Dictionary<string, double>()
        {
            {"Torr",1.00},
            {"mbar",1.3332},
            {"Pa",133.32}
        };
       
    }
    //public class SensorType
    //{
    //    public string type;
    //    public int 
    //}
     
}
