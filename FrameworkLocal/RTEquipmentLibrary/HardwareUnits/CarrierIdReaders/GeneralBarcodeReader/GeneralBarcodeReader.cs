using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronV640
{
    public class GeneralBarcodeReader : CIDReaderBaseDevice, IConnection
    { 
        private string _scRoot;
        private string _portName;
        private bool _enableLog;
        private int _baudRate;
        private int _dataBits;
        private Parity _parity;
        private int _stopBits;
        private AsyncSerialPort _port;
        private string _readCmd;
        private string _endCmd;
        private string _receivedMsg = string.Empty;
        private object _locker = new object();
        DateTime _scanStartTimer;
        public GeneralBarcodeReader(string module, string name, string scRoot, string readcmd, LoadPortBaseDevice lp = null,string endcmd="") : base(module, name, lp)
        {
            _scRoot = scRoot;
            _readCmd = readcmd;
            _endCmd = endcmd;
            _portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _baudRate = SC.GetValue<int>($"{_scRoot}.{Name}.BaudRate");
            _dataBits = SC.GetValue<int>($"{_scRoot}.{Name}.DataBits");
            _parity = (Parity)Enum.Parse(typeof(Parity), SC.GetStringValue($"{_scRoot}.{Name}.Parity"));
            _stopBits = SC.GetValue<int>($"{_scRoot}.{Name}.StopBits");
            _port = new AsyncSerialPort(_portName, _baudRate, 8);//, Parity.Even, StopBits.One, "\r", true);
            _port.EnableLog = _enableLog;
            _port.OnDataChanged += _port_OnDataChanged;
            _port.OnErrorHappened += _port_OnErrorHappened;
            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;

            int retry = 0;
            do
            {
                if (_port.Open())
                {
                    EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                    break;
                }
                else
                {
                    EV.PostInfoLog(Module, $"Can't connected with {Module}.{Name},retry time {retry}.");
                    _port.Close();
                    Thread.Sleep(sleep * 1000);
                }
                if (count > 0 && retry++ > count)
                {
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }
            } while (true);
        }

        private void _port_OnErrorHappened(string obj)
        {
            OnError();
        }

        private void _port_OnDataChanged(string package)
        {
            try
            {
                lock (_locker)
                {
                    if (DeviceState == CIDReaderStateEnum.ReadCarrierID)
                    {
                        CarrierIDBeRead = CarrierIDBeRead + package;
                        if (!CarrierIDBeRead.Contains("\r")) return;
                        CarrierIDBeRead = CarrierIDBeRead.Split('\r')[0].Replace("\n", "");
                        if (string.IsNullOrEmpty(CarrierIDBeRead) || CarrierIDBeRead == "BR")
                        {
                            OnCarrierIDReadFailed("Scan Failed.");
                            return;
                        }
                        OnCarrierIDRead(CarrierIDBeRead.Replace("\r","").Replace("\n",""));
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }

        public string Address { get; set; }

        public bool IsConnected => _port.IsOpen();

        public bool Connect()
        {
            return _port.Open();
        }


        public bool Disconnect()
        {
            return _port.Close();
        }

        protected override bool fStartWriteCarrierID(object[] param)
        {
            return false;
        }
        protected override bool fStartReadCarrierID(object[] param)
        {
            try
            {
                lock (_locker)
                {
                    if (!_port.IsOpen())
                    {
                        EV.PostWarningLog(Module, _portName + " not open, can not scan");
                        return false;
                    }
                    CarrierIDBeRead = string.Empty;
                    _port.Write(_readCmd);
                    _scanStartTimer = DateTime.Now;
                    return true;
                }
            }

            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fMonitorReadCarrierID(object[] param)
        {
            if (DateTime.Now - _scanStartTimer > TimeSpan.FromSeconds(60))
            {
                EV.PostWarningLog(Module, _portName + " error, timeout");
                OnCarrierIDReadFailed("Timeout");
                if(!string.IsNullOrEmpty(_endCmd))
                    _port.Write(_endCmd);
                return true;
            }
            return false;
        }      
    }
}
