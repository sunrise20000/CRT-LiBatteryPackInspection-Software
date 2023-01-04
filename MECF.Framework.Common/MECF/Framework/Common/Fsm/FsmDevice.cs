#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.Fsm
{
	public class FsmDevice : BaseDevice, IDevice
	{
		private Thread _thread = null;

		private IStateMachine _fsm = null;

		private Dictionary<int, string> _fsmStateMap = new Dictionary<int, string>();

		private Dictionary<int, string> _fsmMessageMap = new Dictionary<int, string>();

		public int FsmState => _fsm.State;

		public int FsmPreviousState => _fsm.PrevState;

		public string StringFsmStatus => _fsmStateMap.ContainsKey(FsmState) ? _fsmStateMap[FsmState] : FsmState.ToString();

		public void MapState(int state, string stringState)
		{
			_fsmStateMap[state] = stringState;
		}

		public void MapMessage(int msg, string stringMessage)
		{
			_fsmMessageMap[msg] = stringMessage;
		}

		public void EnableFsm(int fsmInterval, object initState)
		{
			EnableFsm(fsmInterval, (int)initState);
		}

		public void EnableFsm(int fsmInterval, int initState)
		{
			_fsm = new StateMachine(base.Module + " " + base.Name + " FSM", initState, fsmInterval);
			_fsm.Start();
			_thread = new Thread(_fsm.Loop);
			_thread.Name = _fsm.Name;
			_thread.Start();
			while (!_thread.IsAlive)
			{
				Thread.Sleep(1);
			}
		}

		public virtual bool Initialize()
		{
			return true;
		}

		public virtual void Monitor()
		{
		}

		public virtual void Terminate()
		{
			if (_fsm != null)
			{
				_fsm.Stop();
			}
			if (_thread == null || !_thread.IsAlive)
			{
				return;
			}
			Thread.Sleep(100);
			if (_thread.IsAlive)
			{
				try
				{
					_thread.Abort();
				}
				catch (Exception ex)
				{
					LOG.Error($"Entity terminate has exception.", ex);
				}
			}
		}

		public virtual void Reset()
		{
		}

		protected void Transition<T, V>(T state, V msg, FsmFunc func, T next)
		{
			Debug.Assert(typeof(T).IsEnum && typeof(V).IsEnum);
			int state2 = Convert.ToInt32(state);
			int next2 = Convert.ToInt32(next);
			int msg2 = Convert.ToInt32(msg);
			Transition(state2, msg2, func, next2);
		}

		protected void Transition(int state, int msg, FsmFunc func, int next)
		{
			if (_fsm != null)
			{
				_fsm.Transition(state, msg, func, next);
			}
		}

		protected void AnyStateTransition(int msg, FsmFunc func, int next)
		{
			if (_fsm != null)
			{
				_fsm.AnyStateTransition(msg, func, next);
			}
		}

		protected void AnyStateTransition<T, V>(V msg, FsmFunc func, T next)
		{
			Debug.Assert(typeof(T).IsEnum && typeof(V).IsEnum);
			int next2 = Convert.ToInt32(next);
			int msg2 = Convert.ToInt32(msg);
			AnyStateTransition(msg2, func, next2);
		}

		protected void EnterExitTransition<T, V>(T state, FsmFunc enter, V? msg, FsmFunc exit) where V : struct
		{
			Debug.Assert(typeof(T).IsEnum && (!msg.HasValue || typeof(V).IsEnum));
			int state2 = Convert.ToInt32(state);
			int msg2 = ((!msg.HasValue) ? 268435441 : Convert.ToInt32(msg));
			EnterExitTransition(state2, enter, msg2, exit);
		}

		protected void EnterExitTransition(int state, FsmFunc enter, int msg, FsmFunc exit)
		{
			if (_fsm != null)
			{
				_fsm.EnterExitTransition(state, enter, msg, exit);
			}
		}

		public void PostMsg<T>(T msg, params object[] args) where T : struct
		{
			Debug.Assert(typeof(T).IsEnum);
			int msg2 = Convert.ToInt32(msg);
			PostMsg(msg2, args);
		}

		public void PostMsg(int msg, params object[] args)
		{
			if (_fsm == null)
			{
				LOG.Error($"fsm is null, post msg {msg}");
			}
			else
			{
				_fsm.PostMsg(msg, args);
			}
		}

		public bool CheckAllMessageProcessed()
		{
			return _fsm.CheckExecuted();
		}

		public bool CheckToPostMessage<T>(T msg, params object[] args)
		{
			return CheckToPostMessage(Convert.ToInt32(msg));
		}

		public bool CheckToPostMessage(int msg, params object[] args)
		{
			if (!_fsm.FindTransition(_fsm.State, msg))
			{
				string empty = string.Empty;
				empty = ((!_fsmMessageMap.ContainsKey(msg)) ? msg.ToString() : _fsmMessageMap[msg]);
				EV.PostWarningLog(base.Module, base.Name + " is in " + StringFsmStatus + " state，can not do " + empty);
				return false;
			}
			_fsm.PostMsg(msg, args);
			return true;
		}
	}
}
