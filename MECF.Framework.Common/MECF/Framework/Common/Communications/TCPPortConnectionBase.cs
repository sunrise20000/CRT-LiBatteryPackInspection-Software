using System;
using System.Collections.Generic;
using System.Threading;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Communications
{
	public abstract class TCPPortConnectionBase : IConnection
	{
		private AsynSocketClient _socket;

		protected HandlerBase _activeHandler;

		protected object _lockerActiveHandler = new object();

		private string _address;

		private bool _isAsciiMode;

		public int retryTime = 0;

		private PeriodicJob _thread;

		private object _locker = new object();

		private LinkedList<string> _lstAsciiMsgs = new LinkedList<string>();

		private LinkedList<byte[]> _lstBinsMsgs = new LinkedList<byte[]>();

		private string _newLine;

		public string Address => _address;

		public bool IsConnected => _socket.IsConnected;

		public bool IsBusy => _activeHandler != null;

		public bool IsCommunicationError { get; private set; }

		public string LastCommunicationError { get; private set; }

		public bool Connect()
		{
			_socket.Connect();
			int num = 0;
			while (!IsConnected && num < 25)
			{
				Thread.Sleep(200);
				num++;
			}
			if (IsConnected)
			{
				return true;
			}
			Disconnect();
			return false;
		}

		public bool Disconnect()
		{
			_socket.Dispose();
			return true;
		}

		public TCPPortConnectionBase(string address, string newline = "\r", bool isAsciiMode = true)
		{
			_address = address;
			_newLine = newline;
			_isAsciiMode = isAsciiMode;
			_socket = new AsynSocketClient(address, isAsciiMode, newline);
			_socket.OnDataChanged += _port_OnAsciiDataReceived;
			_socket.OnBinaryDataChanged += _port_OnBinaryDataChanged;
			_socket.OnErrorHappened += _port_OnErrorHappened;
			_thread = new PeriodicJob(1, OnTimer, address + ".MonitorHandler", isStartNow: true);
		}

		private bool OnTimer()
		{
			lock (_locker)
			{
				if (_isAsciiMode)
				{
					while (_lstAsciiMsgs.Count > 0)
					{
						string value = _lstAsciiMsgs.First.Value;
						if (!string.IsNullOrEmpty(value))
						{
							_port_HandleAsciiData(value);
						}
						_lstAsciiMsgs.RemoveFirst();
					}
				}
				else
				{
					while (_lstBinsMsgs.Count > 0)
					{
						byte[] value2 = _lstBinsMsgs.First.Value;
						_port_HandleBinarayData(value2);
						_lstBinsMsgs.RemoveFirst();
					}
				}
			}
			return true;
		}

		public bool Poll()
		{
			return _socket.Poll();
		}

		public void Close()
		{
			_socket.Close();
		}

		private void _port_OnErrorHappened(TCPErrorEventArgs obj)
		{
			LOG.Error(obj.Reason);
		}

		public virtual bool SendMessage(string message)
		{
			if (_socket != null && _socket.IsConnected)
			{
				return _socket.Write(message);
			}
			LOG.Error("No connection writing message " + message);
			return false;
		}

		public virtual bool SendMessage(byte[] message)
		{
			if (_socket != null && _socket.IsConnected)
			{
				return _socket.Write(message);
			}
			LOG.Error("No connection writing message " + string.Join(" ", Array.ConvertAll(message, (byte x) => x.ToString("X2"))));
			return false;
		}

		public void ForceClear()
		{
			lock (_lockerActiveHandler)
			{
				_activeHandler = null;
				IsCommunicationError = false;
			}
		}

		public void Execute(HandlerBase handler)
		{
			if (_activeHandler != null || handler == null || !_socket.IsConnected)
			{
				return;
			}
			lock (_lockerActiveHandler)
			{
				retryTime = 0;
				_activeHandler = handler;
				_activeHandler.SetState(EnumHandlerState.Sent);
			}
			if (_isAsciiMode ? SendMessage(handler.SendText) : SendMessage(handler.SendBinary))
			{
				return;
			}
			lock (_lockerActiveHandler)
			{
				_activeHandler = null;
			}
		}

		protected virtual MessageBase ParseResponse(string rawMessage)
		{
			return null;
		}

		protected virtual MessageBase ParseResponse(byte[] rawMessage)
		{
			return null;
		}

		protected virtual void OnEventArrived(MessageBase msg)
		{
		}

		protected virtual void ActiveHandlerProceedMessage(MessageBase msg)
		{
			lock (_lockerActiveHandler)
			{
				if (_activeHandler != null && (msg.IsFormatError || (_activeHandler.HandleMessage(msg, out var transactionComplete) && transactionComplete)))
				{
					_activeHandler = null;
				}
			}
		}

		public void EnableLog(bool enable)
		{
			_socket.NeedLog = enable;
		}

		private void ProceedTransactionMessage(MessageBase msg)
		{
			if (msg == null || msg.IsFormatError)
			{
				SetCommunicationError(isError: true, "received invalid response message.");
				return;
			}
			if (msg.IsEvent)
			{
				OnEventArrived(msg);
			}
			ActiveHandlerProceedMessage(msg);
		}

		private void _port_OnBinaryDataChanged(byte[] binaryData)
		{
			lock (_locker)
			{
				_lstBinsMsgs.AddLast(binaryData);
			}
		}

		private void _port_HandleBinarayData(byte[] binaryData)
		{
			MessageBase msg = ParseResponse(binaryData);
			ProceedTransactionMessage(msg);
		}

		private void _port_OnAsciiDataReceived(string oneLineMessage)
		{
			lock (_locker)
			{
				if (string.IsNullOrEmpty(_newLine))
				{
					_lstAsciiMsgs.AddLast(oneLineMessage);
					return;
				}
				string[] array = oneLineMessage.Split(_newLine.ToCharArray());
				foreach (string text in array)
				{
					if (!string.IsNullOrEmpty(text))
					{
						_lstAsciiMsgs.AddLast(text + _newLine);
					}
				}
			}
		}

		private void _port_HandleAsciiData(string oneLineMessage)
		{
			MessageBase msg = ParseResponse(oneLineMessage);
			ProceedTransactionMessage(msg);
		}

		public HandlerBase MonitorTimeout()
		{
			HandlerBase result = null;
			lock (_lockerActiveHandler)
			{
				if (_activeHandler != null && _activeHandler.CheckTimeout())
				{
					EV.PostWarningLog("System", Address + " receive " + _activeHandler.Name + " timeout");
					result = _activeHandler;
					_activeHandler = null;
					SetCommunicationError(isError: true, "receive response timeout");
				}
			}
			return result;
		}

		public void Retry()
		{
			if (_activeHandler != null && _socket.IsConnected)
			{
				_activeHandler.SetState(EnumHandlerState.Sent);
				if (!(_isAsciiMode ? SendMessage(_activeHandler.SendText) : SendMessage(_activeHandler.SendBinary)))
				{
					_activeHandler = null;
				}
			}
		}

		public void SetCommunicationError(bool isError, string reason)
		{
			IsCommunicationError = isError;
			LastCommunicationError = reason;
		}
	}
}
