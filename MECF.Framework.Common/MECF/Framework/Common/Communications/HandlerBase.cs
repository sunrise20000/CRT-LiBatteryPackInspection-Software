using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MECF.Framework.Common.Communications
{
	public abstract class HandlerBase
	{
		private EnumHandlerState _state;

		protected Stopwatch _timerAck = new Stopwatch();

		protected Stopwatch _timerComplete = new Stopwatch();

		public bool IsComplete => _state == EnumHandlerState.Completed;

		public bool IsAcked => _state == EnumHandlerState.Acked;

		public string MessageType { get; set; }

		public string Name { get; set; }

		public int MutexId { get; set; }

		public TimeSpan AckTimeout { get; set; }

		public TimeSpan CompleteTimeout { get; set; }

		public MessageBase ResponseMessage { get; set; }

		public string SendText { get; set; }

		public byte[] SendBinary { get; set; }

		public List<string> SendOids { get; set; }

		protected HandlerBase(string text)
		{
			SendText = text;
			SendBinary = Encoding.ASCII.GetBytes(text);
			AckTimeout = TimeSpan.FromSeconds(10.0);
			CompleteTimeout = TimeSpan.FromSeconds(60.0);
			_state = EnumHandlerState.Create;
		}

		protected HandlerBase(byte[] buffer)
		{
			SendText = "";
			SendBinary = buffer;
			AckTimeout = TimeSpan.FromSeconds(60.0);
			CompleteTimeout = TimeSpan.FromSeconds(90.0);
			_state = EnumHandlerState.Create;
		}

		protected HandlerBase(List<string> oids)
		{
			SendText = "";
			SendOids = oids;
			AckTimeout = TimeSpan.FromSeconds(60.0);
			CompleteTimeout = TimeSpan.FromSeconds(90.0);
			_state = EnumHandlerState.Create;
		}

		public abstract bool HandleMessage(MessageBase msg, out bool transactionComplete);

		public void OnSent()
		{
			SetState(EnumHandlerState.Sent);
		}

		public void OnAcked()
		{
			SetState(EnumHandlerState.Acked);
		}

		public void OnComplete()
		{
			SetState(EnumHandlerState.Completed);
		}

		public void SetState(EnumHandlerState state)
		{
			_state = state;
			if (_state == EnumHandlerState.Sent)
			{
				_timerAck.Restart();
				_timerComplete.Restart();
			}
			if (_state == EnumHandlerState.Acked)
			{
				_timerAck.Stop();
			}
			if (_state == EnumHandlerState.Completed)
			{
				_timerAck.Stop();
				_timerComplete.Stop();
			}
		}

		public bool CheckTimeout()
		{
			if (_state == EnumHandlerState.Sent && _timerAck.IsRunning && _timerAck.Elapsed > AckTimeout)
			{
				_timerAck.Stop();
				_state = EnumHandlerState.Completed;
				return true;
			}
			if ((_state == EnumHandlerState.Sent || _state == EnumHandlerState.Acked) && _timerComplete.IsRunning && _timerComplete.Elapsed > CompleteTimeout)
			{
				_timerAck.Stop();
				_timerComplete.Stop();
				_state = EnumHandlerState.Completed;
				return true;
			}
			return false;
		}

		public virtual bool MatchMessage(MessageBase msg)
		{
			return false;
		}
	}
}
