using System;
using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Communications
{
	public abstract class SerialPortConnectionBase : IConnection
	{
		private AsyncSerialPort _port;

		protected HandlerBase _activeHandler;

		private object _lockerActiveHandler = new object();

		private string _address;

		private bool _isAsciiMode;

		public int retryTime = 0;

		private PeriodicJob _thread;

		private object _locker = new object();

		private LinkedList<string> _lstAsciiMsgs = new LinkedList<string>();

		private LinkedList<byte[]> _lstBinsMsgs = new LinkedList<byte[]>();

		private string _newLine;

		public string Address => _address;

		public bool IsConnected => _port.IsOpen();

		public bool IsBusy => _activeHandler != null;

		public bool IsCommunicationError { get; private set; }

		public string LastCommunicationError { get; private set; }

		public bool Connect()
		{
			return _port.Open();
		}

		public bool Disconnect()
		{
			_port.Close();
			return true;
		}

		public void TerminateCom()
		{
			_port.Dispose();
		}

		public SerialPortConnectionBase(string port, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline = "\r", bool isAsciiMode = true, int interval = 2)
		{
			_address = port;
			_isAsciiMode = isAsciiMode;
			_newLine = newline;
			_port = new AsyncSerialPort(port, baudRate, dataBits, parity, stopBits, newline, isAsciiMode);
			_port.OnDataChanged += _port_OnAsciiDataReceived;
			_port.OnBinaryDataChanged += _port_OnBinaryDataChanged;
			_port.OnErrorHappened += _port_OnErrorHappened;
			_thread = new PeriodicJob(interval, OnTimer, port + ".MonitorHandler", isStartNow: true);
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
						_port_HandleAsciiData(value);
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

		public void SetPortAddress(string portName)
		{
			_port.PortName = portName;
		}

		private void _port_OnErrorHappened(string obj)
		{
			LOG.Error(obj);
		}

		public virtual bool SendMessage(string message)
		{
			if (_port != null && _port.IsOpen())
			{
				return _port.Write(message);
			}
			LOG.Error("No connection writing message " + message);
			return false;
		}

		public virtual bool SendMessage(byte[] message)
		{
			if (_port != null && _port.IsOpen())
			{
				return _port.Write(message);
			}
			LOG.Error("No connection writing message " + string.Join(" ", Array.ConvertAll(message, (byte x) => x.ToString("X2"))));
			return false;
		}

		public void ForceClear()
		{
			lock (_lockerActiveHandler)
			{
				IsCommunicationError = false;
				_activeHandler = null;
			}
		}

		public void Execute(HandlerBase handler)
		{
			if (_activeHandler != null || handler == null || !_port.IsOpen())
			{
				return;
			}
			lock (_lockerActiveHandler)
			{
				retryTime = 0;
				_activeHandler = handler;
				_activeHandler.SetState(EnumHandlerState.Sent);
			}
			if ((!_isAsciiMode && handler.SendBinary == null) || (_isAsciiMode ? SendMessage(handler.SendText) : SendMessage(handler.SendBinary)))
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
					if (_isAsciiMode || (!_isAsciiMode && _activeHandler.SendBinary != null))
					{
						_activeHandler = null;
					}
					else
					{
						_activeHandler.SetState(EnumHandlerState.Sent);
					}
				}
			}
		}

		public void EnableLog(bool enable)
		{
			_port.EnableLog = enable;
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
			if (_activeHandler != null && _port.IsOpen())
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
