using System;
using System.Threading;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Fsm;

namespace MECF.Framework.Common.Communications
{
	public class FsmConnection : ActiveFsm
	{
		private enum ConnectionState
		{
			Init = 1000,
			NotConnect = 1001,
			Connecting = 1002,
			Connected = 1003,
			Error = 1004
		}

		private enum MSG
		{
			Connect = 1000,
			Disconnect = 1001,
			ConnectFailed = 1002,
			Connected = 1003,
			Reset = 1004,
			CommunicationError = 1005,
			ReceiveBinaryData = 1006,
			SendBinaryData = 1007,
			ReceiveAsciiData = 1008,
			SendAsciiData = 1009,
			ConfirmConnection = 1010
		}

		private IConnectable _connectable;

		private IConnectionContext _config;

		private string _lastError;

		public bool IsConnected => base.FsmState == 1003;

		public bool IsError => base.FsmState == 1004;

		public string Name { get; set; }

		public event Action<string> OnError;

		public event Action OnConnected;

		public event Action OnDisconnected;

		public virtual void Initialize(int fsmInterval, IConnectable connectable, IConnectionContext config)
		{
			_connectable = connectable;
			_config = config;
			_connectable.OnBinaryDataReceived += _connectable_OnBinaryDataChanged;
			_connectable.OnAsciiDataReceived += _connectable_OnAsciiDataChanged;
			_connectable.OnCommunicationError += ConnectableOnCommunicationError;
			EnumLoop<ConnectionState>.ForEach(delegate(ConnectionState item)
			{
				MapState((int)item, item.ToString());
			});
			EnumLoop<MSG>.ForEach(delegate(MSG item)
			{
				MapMessage((int)item, item.ToString());
			});
			EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Init, FsmEnterInit, FSM_MSG.NONE, null);
			Transition(ConnectionState.Init, MSG.Disconnect, null, ConnectionState.NotConnect);
			Transition(ConnectionState.Init, MSG.Connect, null, ConnectionState.Connecting);
			EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Connecting, FsmEnterConnecting, FSM_MSG.NONE, null);
			Transition(ConnectionState.Connecting, MSG.ConnectFailed, null, ConnectionState.Error);
			Transition(ConnectionState.Connecting, MSG.Connected, null, ConnectionState.Connected);
			Transition(ConnectionState.Connecting, MSG.Disconnect, FsmDisconnect, ConnectionState.NotConnect);
			Transition(ConnectionState.Connecting, FSM_MSG.TIMER, FsmMonitorConnecting, ConnectionState.Connecting);
			EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Connected, FsmEnterConnected, FSM_MSG.NONE, FsmExitConnected);
			Transition(ConnectionState.Connected, MSG.Disconnect, FsmDisconnect, ConnectionState.NotConnect);
			Transition(ConnectionState.Connected, MSG.CommunicationError, null, ConnectionState.Error);
			Transition(ConnectionState.Connected, MSG.ConnectFailed, null, ConnectionState.Error);
			Transition(ConnectionState.Connected, FSM_MSG.TIMER, FsmMonitorConnected, ConnectionState.Connected);
			Transition(ConnectionState.Connected, MSG.ConfirmConnection, FsmMonitorConnected, ConnectionState.Connected);
			Transition(ConnectionState.Connected, MSG.ReceiveBinaryData, FsmReceiveBinaryData, ConnectionState.Connected);
			Transition(ConnectionState.Connected, MSG.SendBinaryData, FsmSendBinaryData, ConnectionState.Connected);
			Transition(ConnectionState.Connected, MSG.ReceiveAsciiData, FsmReceiveAsciiData, ConnectionState.Connected);
			Transition(ConnectionState.Connected, MSG.SendAsciiData, FsmSendAsciiData, ConnectionState.Connected);
			Transition(ConnectionState.NotConnect, MSG.Connect, null, ConnectionState.Connecting);
			EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Error, FsmEnterError, FSM_MSG.NONE, null);
			Transition(ConnectionState.Error, MSG.Disconnect, null, ConnectionState.NotConnect);
			Transition(ConnectionState.Error, MSG.Reset, null, ConnectionState.Connecting);
			StartFsm(Name, fsmInterval, 1000);
		}

		public virtual void Terminate()
		{
			StopFsm();
			try
			{
				_connectable.Disconnect(out var _);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		protected virtual void OnReceiveData(string message)
		{
		}

		protected virtual void OnReceiveData(byte[] message)
		{
		}

		protected virtual void OnConnect()
		{
		}

		protected virtual void OnDisconnect()
		{
		}

		protected virtual void OnConnectMonitor()
		{
		}

		private bool FsmSendAsciiData(object[] param)
		{
			if (!_connectable.SendAsciiData((string)param[0]))
			{
				PostMsg(MSG.ConfirmConnection);
			}
			return true;
		}

		private bool FsmReceiveAsciiData(object[] param)
		{
			try
			{
				OnReceiveData((string)param[0]);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		private bool FsmSendBinaryData(object[] param)
		{
			if (!_connectable.SendBinaryData((byte[])param[0]))
			{
				PostMsg(MSG.ConfirmConnection);
			}
			return true;
		}

		private bool FsmReceiveBinaryData(object[] param)
		{
			try
			{
				OnReceiveData((byte[])param[0]);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		private void ConnectableOnCommunicationError(string obj)
		{
			if (!CheckToPostMessage(1005, out var reason, obj))
			{
				LOG.Write(reason);
			}
		}

		private void _connectable_OnAsciiDataChanged(string obj)
		{
			if (!CheckToPostMessage(1008, out var reason, obj))
			{
				LOG.Write(reason);
			}
		}

		private void _connectable_OnBinaryDataChanged(byte[] obj)
		{
			if (!CheckToPostMessage(1006, out var reason, obj))
			{
				LOG.Write(reason);
			}
		}

		private bool FsmDisconnect(object[] param)
		{
			if (!_connectable.Disconnect(out var reason))
			{
				LOG.Write(reason);
				return false;
			}
			return true;
		}

		private bool FsmExitConnected(object[] param)
		{
			if (this.OnDisconnected != null)
			{
				this.OnDisconnected();
			}
			OnDisconnect();
			return true;
		}

		private bool FsmEnterError(object[] param)
		{
			if (this.OnError != null)
			{
				this.OnError(_lastError);
			}
			return true;
		}

		private bool FsmEnterConnecting(object[] param)
		{
			if (!_connectable.Connect(out var reason))
			{
				_lastError = reason;
				PostMsg(MSG.ConnectFailed);
			}
			return true;
		}

		private bool FsmMonitorConnecting(object[] param)
		{
			if (_connectable.CheckIsConnected())
			{
				PostMsg(MSG.Connected);
				return true;
			}
			if (!PeformConnect())
			{
				_lastError = "Can not connect with " + Name;
				PostMsg(MSG.ConnectFailed);
				return true;
			}
			return true;
		}

		private bool FsmMonitorConnected(object[] param)
		{
			if (_config.EnableCheckConnection && !_connectable.CheckIsConnected() && !PeformConnect())
			{
				_lastError = "Can not connect with " + Name;
				PostMsg(MSG.ConnectFailed);
				return false;
			}
			OnConnectMonitor();
			return true;
		}

		private bool PeformConnect()
		{
			int num = 0;
			do
			{
				if (!_connectable.Connect(out var reason))
				{
					_lastError = reason;
					return false;
				}
				Thread.Sleep(10);
				if (_connectable.CheckIsConnected())
				{
					return true;
				}
				if (num < _config.MaxRetryConnectCount)
				{
					num++;
					LOG.Write($"{Name} retry connect {num} /{_config.MaxRetryConnectCount} time");
					Thread.Sleep(_config.RetryConnectIntervalMs);
				}
			}
			while (num < _config.MaxRetryConnectCount);
			if (!_connectable.CheckIsConnected())
			{
				LOG.Error("Can not connect with " + Name);
				return false;
			}
			return true;
		}

		private bool FsmEnterConnected(object[] param)
		{
			if (this.OnConnected != null)
			{
				this.OnConnected();
			}
			OnConnect();
			return true;
		}

		private bool FsmEnterInit(object[] param)
		{
			if (_config.IsEnabled)
			{
				PostMsg(MSG.Connect);
			}
			else
			{
				PostMsg(MSG.Disconnect);
			}
			return true;
		}

		public void InvokeSendData<T>(T data)
		{
			if (!CheckToPostMessage(1009, out var reason, data))
			{
				LOG.Write(reason);
			}
		}

		public void InvokeConnect()
		{
			if (!CheckToPostMessage(1000, out var reason))
			{
				LOG.Write(reason);
			}
		}

		public void InvokeDisconnect()
		{
			if (!CheckToPostMessage(1001, out var reason))
			{
				LOG.Write(reason);
			}
		}

		public void InvokeError()
		{
			if (!CheckToPostMessage(1005, out var reason))
			{
				LOG.Write(reason);
			}
		}

		public void InvokeReset()
		{
			if (!CheckToPostMessage(1004, out var reason))
			{
				LOG.Write(reason);
			}
		}
	}
}
