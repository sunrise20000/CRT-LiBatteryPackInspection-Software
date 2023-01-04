using System.Collections.Generic;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Communications
{
	public class SNMPBase
	{
		private SNMPFactory _snmp;

		protected HandlerBase _activeHandler;

		private object _lockerActiveHandler = new object();

		private string _address;

		public int retryTime = 0;

		private PeriodicJob _thread;

		private object _locker = new object();

		private LinkedList<string> _lstMsgs = new LinkedList<string>();

		private string _newLine;

		private Dictionary<string, string> errorcode = new Dictionary<string, string>
		{
			{ "0", "noerror:all going well" },
			{ "1", "toolBig:The agent was unable to load the response into an SNMP packet" },
			{ "2", "noSuchName:The operation indicates a non-existent variable" },
			{ "3", "badValue:A set operation indicates an invalid value or invalid syntax" },
			{ "4", "readOnly:The admin process attempted to modify a read-only variable" },
			{ "5", "genErr:Some other mistake" }
		};

		public string Address => _address;

		public bool IsConnected => _snmp.IsOpen();

		public bool IsBusy => _activeHandler != null;

		public bool IsCommunicationError { get; private set; }

		public string LastCommunicationError { get; private set; }

		public bool Connect()
		{
			return _snmp.Open();
		}

		public bool Disconnect()
		{
			_snmp.Close();
			return true;
		}

		public void TerminateCom()
		{
		}

		public SNMPBase(string port, string community, int VersionNo, string newline = "\r")
		{
			_address = port;
			_newLine = newline;
			_snmp = new SNMPFactory(port, community, VersionNo, newline);
			_snmp.OnDataChanged += _snmp_OnDataChanged;
			_snmp.OnErrorHappened += _snmp_OnErrorHappened;
			_snmp.OnDataDicChanged += _snmp_OnDataDicChanged;
			_snmp.OnDataWriteChanged += _snmp_OnDataWriteChanged;
			_thread = new PeriodicJob(2, OnTimer, port + ".MonitorHandler", isStartNow: true);
		}

		private void _snmp_OnDataWriteChanged(string obj)
		{
		}

		private void _snmp_OnDataDicChanged(Dictionary<string, string> obj)
		{
			MessageBase msg = ParseResponse(obj);
			ProceedTransactionMessage(msg);
		}

		private void _snmp_OnErrorHappened(string arg1, string arg2)
		{
		}

		private void _snmp_OnDataChanged(string arg1, string arg2)
		{
			MessageBase msg = ParseResponse(arg1, arg2);
			ProceedTransactionMessage(msg);
		}

		private bool OnTimer()
		{
			lock (_locker)
			{
				while (_lstMsgs.Count > 0)
				{
					string value = _lstMsgs.First.Value;
					_port_HandleAsciiData(value);
					_lstMsgs.RemoveFirst();
				}
			}
			return true;
		}

		private void _port_OnErrorHappened(string obj)
		{
			LOG.Error(obj);
		}

		public virtual bool SendMessage(string message)
		{
			if (_snmp != null && _snmp.IsOpen())
			{
				return _snmp.SnmpGet(message);
			}
			LOG.Error("No connection writing message " + message);
			return false;
		}

		public virtual bool SendNextMessage(string message)
		{
			if (_snmp != null && _snmp.IsOpen())
			{
				return _snmp.SnmpGetNext(message);
			}
			LOG.Error("No connection writing message " + message);
			return false;
		}

		public virtual bool SendBulk(string message)
		{
			if (_snmp != null && _snmp.IsOpen())
			{
				return _snmp.SnmpWalk(message);
			}
			LOG.Error("No connection writing message " + message);
			return false;
		}

		public virtual bool SendMessage(List<string> message)
		{
			if (_snmp != null && _snmp.IsOpen())
			{
				return _snmp.SnmpGetList(message);
			}
			LOG.Error($"No connection writing message {message}");
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
			if (_activeHandler != null || handler == null || !_snmp.IsOpen())
			{
				return;
			}
			lock (_lockerActiveHandler)
			{
				retryTime = 0;
				_activeHandler = handler;
				_activeHandler.SetState(EnumHandlerState.Sent);
			}
			bool flag = false;
			switch (handler.MessageType)
			{
			case "Get":
				flag = SendMessage(handler.SendText);
				break;
			case "GetNext":
				flag = SendNextMessage(handler.SendText);
				break;
			case "Set":
				flag = SendMessage(handler.SendText);
				break;
			case "GetList":
				flag = SendMessage(handler.SendOids);
				break;
			case "GetBulk":
				flag = SendBulk(handler.SendText);
				break;
			}
			if (flag)
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

		protected virtual MessageBase ParseResponse(string Oid, string value)
		{
			return null;
		}

		protected virtual MessageBase ParseResponse(Dictionary<string, string> rawMessage)
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
			_snmp.EnableLog = enable;
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

		private void _port_OnAsciiDataReceived(string oneLineMessage)
		{
			lock (_locker)
			{
				if (string.IsNullOrEmpty(_newLine))
				{
					_lstMsgs.AddLast(oneLineMessage);
					return;
				}
				string[] array = oneLineMessage.Split(_newLine.ToCharArray());
				foreach (string text in array)
				{
					if (!string.IsNullOrEmpty(text))
					{
						_lstMsgs.AddLast(text + _newLine);
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
					result = _activeHandler;
					_activeHandler = null;
				}
			}
			return result;
		}

		public void Retry()
		{
			if (_activeHandler != null && _snmp.IsOpen())
			{
				_activeHandler.SetState(EnumHandlerState.Sent);
				if (!SendMessage(_activeHandler.SendText))
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
