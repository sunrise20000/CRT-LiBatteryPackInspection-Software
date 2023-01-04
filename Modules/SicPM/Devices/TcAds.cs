using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.PLC;
using MECF.Framework.RT.Core.IoProviders.Siemens;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes;
using TwinCAT.Ads;

namespace SicPM.Devices
{
    public class TcAds : BaseDevice, IConnectable, IConnectionContext, IDevice, IConnection, IAdsPlc
    {
        #region IConnectionContext

        public bool IsEnabled => true;

        public int RetryConnectIntervalMs => 1000;
        public int MaxRetryConnectCount => 5;
        public bool EnableCheckConnection => true;
        public virtual string Address { get; }
        public bool IsAscii { get; }
        public string NewLine { get; }
        public bool EnableLog { get; }

        #endregion

        #region IConnectable
        public event Action<string> OnCommunicationError;
        public event Action<string> OnAsciiDataReceived;
        public event Action<byte[]> OnBinaryDataReceived;

        #endregion

        public event Action OnDisconnected;

        bool IAdsPlc.CheckIsConnected()
        {
            return Connection.IsConnected;
        }

        bool IConnection.IsConnected
        {
            get
            {
                return Connection.IsConnected;
            }
        }

        bool IConnection.Connect()
        {
            Connect(out _);
            return true;
        }

        bool IConnection.Disconnect()
        {
            Disconnect(out _);
            return true;
        }

        public FsmConnection Connection { get; set; } = new FsmConnection();


        //public bool IsCommunicationError
        //{
        //    get
        //    {
        //        return _isCommunicationError;
        //    }
        //}

        //private bool _isCommunicationError;

        private OperateResult connect;
        public SiemensS7Net siemensTcpNet = null;
        private SiemensPLCS siemensPLCSelected = SiemensPLCS.S1500;
        private string _plcType;

        private Dictionary<string, int> _nameHandleMap = new Dictionary<string, int>();

        private bool _isTargetConnected;

        public AlarmEventItem AlarmConnectFailed { get; set; }
        public AlarmEventItem AlarmCommunicationError { get; set; }
        public event Action OnConnected;

        public TcAds(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            Name = node.GetAttribute("id");

            Address = SC.GetStringValue($"PM.{Module}.SiemensIP");
            _plcType = SC.GetStringValue($"PM.{Module}.SiemensType");
        }

        public bool Initialize()
        {
            AlarmConnectFailed = SubscribeAlarm($"{Module}.{Name}.ConnectionError", $"Can not connect with {Address}", null);
            AlarmCommunicationError = SubscribeAlarm($"{Module}.{Name}.CommunicationError", $"Can not Communication {Address}", null);

            Connection = new FsmConnection();
            Connection.Initialize(100, this, this);

            Connection.OnConnected += Connection_OnConnected;
            Connection.OnDisconnected += Connection_OnDisconnected;
            Connection.OnError += Connection_OnError;

            ConnectionManager.Instance.Subscribe($"{Module}.{Name}", this);

            return true;
        }

        bool IConnectable.CheckIsConnected()
        {
            return siemensTcpNet != null && siemensTcpNet.IsConnected;
        }

        public bool Connect(out string reason)
        {
            var ipAddr = ParseAdsIPAddr(Address);
            siemensPLCSelected = (SiemensPLCS)Enum.Parse(typeof(SiemensPLCS), _plcType);
            siemensTcpNet = new SiemensS7Net(siemensPLCSelected);
            siemensTcpNet.IpAddress = ipAddr.Item1;// _ipAddress;
            siemensTcpNet.Port = ipAddr.Item2;   // int.Parse(_port);
            siemensTcpNet.ConnectTimeOut = 1;
            siemensTcpNet.ReceiveTimeOut = 1000;

            try
            {
                if (siemensPLCSelected != SiemensPLCS.S200Smart &&
                    siemensPLCSelected != SiemensPLCS.S200)
                {
                    siemensTcpNet.Rack = 0;
                    siemensTcpNet.Slot = 0;

                    siemensTcpNet.ConnectionType = 1;
                    siemensTcpNet.LocalTSAP = 258;
                }

                connect = siemensTcpNet.ConnectServer();
                if (!siemensTcpNet.IsConnected)
                {
                    //_isCommunicationError = true;
                    //IsConnect = true;
                    //EV.PostInfoLog(Module, "PLC Connection Successful！");
                }
                else
                {
                    //_isCommunicationError = false;
                }

                //return connect.IsSuccess;
            }
            catch (Exception ex)
            {
                _isTargetConnected = false;
                //_isCommunicationError = true;
                reason = ex.Message;
                return false;
            }

            _isTargetConnected = true;
            reason = string.Empty;
            return true;
        }

        public bool Disconnect(out string reason)
        {
            reason = string.Empty;
            siemensTcpNet.ConnectClose();
            siemensTcpNet = null;
            connect = null;
            return false;
        }

        private void Connection_OnError(string obj)
        {
            AlarmConnectFailed.Set();
        }

        private void Connection_OnDisconnected()
        {
            EV.PostInfoLog(Module, $"Disconnect from PLC {Address}");

            ResetAlarm();
        }

        private void Connection_OnConnected()
        {
            EV.PostInfoLog(Module, $"Connected with PLC {Address}");

            ResetAlarm();
        }

        public void Monitor()
        {

        }

        public void Terminate()
        {
            Connection.Terminate();
        }

        private Tuple<string, int> ParseAdsIPAddr(string addr)
        {
            if (addr == null)
                throw (new Exception("PM.SiemensIP is not exist in sc.data!"));

            Regex reg = new Regex(@"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?):(\d{1,5})$");
            if (!reg.IsMatch(addr))
                throw (new Exception($@"{Module}.SiemensIP '{addr}' is invalid!"));

            var item = addr.Split(':');
            return Tuple.Create(item[0], int.Parse(item[1]));
        }

        public void Reset()
        {
            ResetAlarm();

            Connection.InvokeReset();
        }

        public bool Read(string variableName, out object data, string type, int length, out string reason)
        {
            data = null;
            reason = string.Empty;
            return true;
        }

        public bool WriteArrayElement(string variableName, int index, object data, out string reason)
        {
            reason = "";
            return true;
        }

        public bool SendBinaryData(byte[] data)
        {
            return true;
        }

        public bool SendAsciiData(string data)
        {
            return true;
        }

        public bool BulkReadRenderResult(string Adrress, ushort length, out byte[] data)
        {
            try
            {
                if (siemensTcpNet == null)
                {
                    data = null;
                    return false;
                }
                OperateResult<byte[]> read = siemensTcpNet.Read(Adrress, length);// ushort.Parse(lengthTextBox.Text));
                if (read.IsSuccess)
                {
                    data = read.Content;
                    return true;
                }
                else
                {
                    //if (!_isCommunicationError)
                    //    EV.PostAlarmLog("PLC", $"Communication error with Siemens PLC , {read.Message}");
                    //_isCommunicationError = true;
                    data = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                // EV.PostAlarmLog("", "Read Failed：" + ex.Message);
            }

            data = null;
            return false;
        }

        public bool BulkWriteByteRenderResult(string Adrress, byte[] data)
        {
            try
            {
                if (siemensTcpNet == null)
                {
                    data = null;
                    return false;
                }
                OperateResult write = siemensTcpNet.Write(Adrress, data);
                if (!write.IsSuccess)
                {
                    //if (!_isCommunicationError)
                    EV.PostAlarmLog("PLC", $"Communication error with Siemens PLC , {write.Message}");
                    //_isCommunicationError = true;
                }
                return write.IsSuccess;
            }
            catch (Exception ex)
            {
                // EV.PostAlarmLog("", "Read Failed：" + ex.Message);
            }
            return false;
        }
    }
}
