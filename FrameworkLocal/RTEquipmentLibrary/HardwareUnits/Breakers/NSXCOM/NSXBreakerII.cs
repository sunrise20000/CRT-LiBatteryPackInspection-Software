using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Breakers.NSXCOM
{
    public class NSXBreakerInfo
    {
        public int Index;
        public string Name;
        public float Value;
    }
    public class NSXBreakerII : BaseDevice, IConnection
    {
        private NSXBreakerIIConnection _connection;
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
      
        public List<NSXBreakerInfo> nsxbreakerInfo = new List<NSXBreakerInfo>();
       
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

        public NSXBreakerII(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        ~NSXBreakerII()
        {
            _connection.Disconnect();

        }

        public NSXBreakerII(string module, string name, string scRoot,int count) : base(module, name, name, name)
        {
            tempCount = count;
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        public void QueryPressure()
        {
            
            _lstHandler.AddLast(new NSXBreakerIIQueryHandler(this, "Query Electricity", 0x2, 0x3,0xF7, 0x0, 0x18));
           
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

            ADDNSXBreakerInfo();

            //string portName = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            string portName = SC.GetStringValue($"{Name}.Address");
            Address = portName;
            int address = SC.GetValue<int>($"{Name}.DeviceAddress");

            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new NSXBreakerIIConnection(Address);
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

            foreach (var nsx in nsxbreakerInfo)
            {
                DATA.Subscribe($"{Module}.{Name}." + nsx.Name, () => nsx.Value);
            }

            //DATA.Subscribe($"{Name}.DiffPressure1", () => _actualTempValue);
            //DATA.Subscribe($"{Name}.DiffPressure2", () => _settingTempValue);


            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }
        public void ADDNSXBreakerInfo()
        {
            nsxbreakerInfo.Clear();

            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 0, Name = "APhaseCurrent", Value = 0 }); 
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 1, Name = "BPhaseCurrent", Value = 0 });
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 2, Name = "CPhaseCurrent", Value = 0 });
            
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 18, Name = "AActivePower", Value = 0 }); ;
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 19, Name = "BActivePower", Value = 0 });
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 20, Name = "CActivePower", Value = 0 });

            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 21, Name = "AReactivePower", Value = 0 }); ;
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 22, Name = "BReactivePower", Value = 0 });
            nsxbreakerInfo.Add(new NSXBreakerInfo() { Index = 23, Name = "CReactivePower", Value = 0 });
        }


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

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        // _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                        _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }

                    _connection.ForceClear();
                    return true;

                }

                HandlerBase handler = null;
                //if (!_connection.IsBusy)
                //{
                lock (_locker)
                {
                    if (_lstHandler.Count == 0)
                        QueryPressure();
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


                //}
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

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

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

    }
}
