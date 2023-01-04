#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.Fsm
{
	public class ActiveFsm
	{
		private Dictionary<int, string> _fsmStateMap = new Dictionary<int, string>();

		private Dictionary<int, string> _fsmMessageMap = new Dictionary<int, string>();

		private Thread _thread = null;

		private IStateMachine _fsm = null;

		public int FsmState => _fsm.State;

		public string StringFsmStatus => _fsmStateMap.ContainsKey(FsmState) ? _fsmStateMap[FsmState] : FsmState.ToString();

		public ActiveFsm()
		{
			_fsm = new StateMachine();
		}

		public ActiveFsm(string name)
		{
			_fsm = new StateMachine();
		}

		public void MapState(int state, string stringState)
		{
			_fsmStateMap[state] = stringState;
		}

		public void MapMessage(int msg, string stringMessage)
		{
			_fsmMessageMap[msg] = stringMessage;
		}

		public string GetStringMessage(int msg)
		{
			return _fsmMessageMap.ContainsKey(msg) ? _fsmMessageMap[msg] : msg.ToString();
		}

		protected void StartFsm(string name, int fsmInterval, int initState)
		{
			_fsm.Init(initState, fsmInterval);
			_fsm.Name = name;
			_fsm.Start();
			_thread = new Thread(_fsm.Loop);
			_thread.Name = _fsm.Name;
			_thread.Start();
			while (!_thread.IsAlive)
			{
				Thread.Sleep(1);
			}
		}

		protected void StopFsm()
		{
			if (_fsm == null)
			{
				return;
			}
			_fsm.Stop();
			if (_thread == null || !_thread.IsAlive)
			{
				return;
			}
			Thread.Sleep(100);
			if (!_thread.IsAlive)
			{
				return;
			}
			try
			{
				_thread.Abort();
			}
			catch (Exception ex)
			{
				LOG.Error(" " + _fsm.Name + " FSM terminated, " + ex.Message);
			}
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
				_fsm.PostMsgWithoutLock(msg, args);
			}
		}

		public bool CheckToPostMessage(int msg, out string reason, params object[] args)
		{
			if (!_fsm.FindTransition(_fsm.State, msg))
			{
				reason = _fsm.Name + " is in " + StringFsmStatus + " state，can not do " + GetStringMessage(msg);
				return false;
			}
			_fsm.PostMsgWithoutLock(msg, args);
			reason = string.Empty;
			return true;
		}
	}
}
