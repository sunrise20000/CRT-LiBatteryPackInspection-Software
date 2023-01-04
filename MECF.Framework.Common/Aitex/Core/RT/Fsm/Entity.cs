#define DEBUG
using System;
using System.Diagnostics;
using System.Threading;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Fsm
{
	public class Entity
	{
		protected Thread thread = null;

		protected IStateMachine fsm = null;

		public bool Running { get; protected set; }

		public bool Initialize()
		{
			Debug.Assert(fsm != null);
			Init();
			fsm.Start();
			thread = new Thread(fsm.Loop);
			thread.Name = fsm.Name;
			thread.Start();
			while (!thread.IsAlive)
			{
				Thread.Sleep(1);
			}
			return true;
		}

		public virtual void Terminate()
		{
			fsm.Stop();
			Term();
			if (thread == null || !thread.IsAlive)
			{
				return;
			}
			Thread.Sleep(100);
			if (thread.IsAlive)
			{
				try
				{
					thread.Abort();
				}
				catch (Exception ex)
				{
					LOG.Error($"Entity terminate has exception.", ex);
				}
			}
		}

		protected virtual bool Init()
		{
			return true;
		}

		protected virtual void Term()
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
			if (fsm != null)
			{
				fsm.Transition(state, msg, func, next);
			}
		}

		protected void AnyStateTransition(int msg, FsmFunc func, int next)
		{
			if (fsm != null)
			{
				fsm.AnyStateTransition(msg, func, next);
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
			if (fsm != null)
			{
				fsm.EnterExitTransition(state, enter, msg, exit);
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
			if (fsm == null)
			{
				LOG.Error($"fsm is null, post msg {msg}");
			}
			else
			{
				fsm.PostMsg(msg, args);
			}
		}
	}
}
