using System;
using System.Xml;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Event;

namespace MECF.Framework.Common.PLC
{
	public class WcfPlc : BaseDevice, IConnection, IConnectable, IAdsPlc, IDevice
	{
		private WcfPlcServiceClient _plcClient;

		private FsmConnection _connection;

		private int _heartbeat = 1;

		public AlarmEventItem AlarmConnectFailed { get; set; }

		public AlarmEventItem AlarmCommunicationError { get; set; }

		public string Address { get; }

		public bool IsConnected { get; set; }

		public event Action OnConnected;

		public event Action OnDisconnected;

		public event Action<string> OnCommunicationError;

		public event Action<string> OnAsciiDataReceived;

		public event Action<byte[]> OnBinaryDataReceived;

		public WcfPlc(string module, XmlElement node, string ioModule = "")
		{
			string attribute = node.GetAttribute("module");
			base.Module = (string.IsNullOrEmpty(attribute) ? module : attribute);
			base.Name = node.GetAttribute("id");
		}

		public bool Initialize()
		{
			_plcClient = new WcfPlcServiceClient();
			AlarmConnectFailed = SubscribeAlarm(base.Module + "." + base.Name + ".ConnectionError", "Can not connect with " + Address, null);
			AlarmCommunicationError = SubscribeAlarm(base.Module + "." + base.Name + ".CommunicationError", "Can not Communication " + Address, null);
			_connection = new FsmConnection();
			IConnectionContext config = new StaticConnectionContext
			{
				IsEnabled = true,
				Address = "WCF PLC",
				EnableCheckConnection = false,
				EnableLog = false,
				MaxRetryConnectCount = 3,
				IsAscii = false,
				NewLine = string.Empty,
				RetryConnectIntervalMs = 1000
			};
			_connection.OnConnected += _connection_OnConnected;
			_connection.OnDisconnected += _connection_OnDisconnected;
			_connection.OnError += _connection_OnError;
			_connection.Initialize(50, this, config);
			return true;
		}

		private void _connection_OnError(string error)
		{
			AlarmConnectFailed.Set(error);
		}

		private void _connection_OnDisconnected()
		{
			if (this.OnDisconnected != null)
			{
				this.OnDisconnected();
			}
		}

		private void _connection_OnConnected()
		{
			if (this.OnConnected != null)
			{
				this.OnConnected();
			}
		}

		public void Monitor()
		{
		}

		public void Terminate()
		{
		}

		public void Reset()
		{
			ResetAlarm();
			_connection.InvokeReset();
		}

		public bool Connect()
		{
			_connection.InvokeConnect();
			return true;
		}

		public bool Disconnect()
		{
			_connection.InvokeDisconnect();
			return true;
		}

		public bool Connect(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public bool Disconnect(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public bool CheckIsConnected()
		{
			if (_plcClient == null)
			{
				return false;
			}
			_heartbeat++;
			if (_plcClient.Heartbeat(_heartbeat) > 0 && !_plcClient.ActionFailed)
			{
				return true;
			}
			return false;
		}

		public bool SendBinaryData(byte[] data)
		{
			return true;
		}

		public bool SendAsciiData(string data)
		{
			return true;
		}

		public bool Read(string variable, out object data, string type, int length, out string reason)
		{
			if (!_connection.IsConnected)
			{
				reason = "Not connected with PLC";
				data = null;
				return false;
			}
			if (base.HasAlarm)
			{
				reason = "Has alarm";
				data = null;
				return false;
			}
			bool result = _plcClient.Read(variable, out data, type, length, out reason);
			if (data == null || _plcClient.ActionFailed)
			{
				AlarmCommunicationError.Description = "Failed read PLC data";
				AlarmCommunicationError.Set();
				return false;
			}
			return result;
		}

		public bool WriteArrayElement(string variable, int index, object value, out string reason)
		{
			if (!_connection.IsConnected)
			{
				reason = "Not connected with PLC";
				return false;
			}
			if (base.HasAlarm)
			{
				reason = "Has alarm";
				return false;
			}
			bool result = _plcClient.WriteArrayElement(variable, index, value, out reason);
			if (_plcClient.ActionFailed)
			{
				AlarmCommunicationError.Description = "Failed read PLC data";
				AlarmCommunicationError.Set();
				return false;
			}
			return result;
		}

		public bool BulkReadRenderResult(string Adrress, ushort length, out byte[] data)
		{
			throw new NotImplementedException();
		}

		public bool BulkWriteFloatRenderResult(string Adrress, float data)
		{
			throw new NotImplementedException();
		}

		public bool BulkWriteByteRenderResult(string Adrress, byte[] data)
		{
			throw new NotImplementedException();
		}
	}
}
