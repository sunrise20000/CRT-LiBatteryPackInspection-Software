using System;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device
{
	public abstract class DeviceEntityT<T> : Entity, IEntity where T : class, IDeviceManager, new()
	{
		public enum STATE
		{
			INIT = 0,
			RUNNING = 1,
			WAIT_RESET = 2,
			ERROR = 3
		}

		public enum MSG
		{
			INIT = 0,
			ERROR = 1,
			WAIT_RESET = 2,
			RESET = 3
		}

		private T mgr = null;

		public bool IsWaitReset => fsm.State == 2;

		public DeviceEntityT()
		{
			base.Running = false;
			fsm = new StateMachine<DeviceEntityT<T>>("DeviceLayer", 0, 50);
			EnterExitTransition<STATE, MSG>(STATE.INIT, null, MSG.INIT, null);
			Transition(STATE.INIT, MSG.INIT, fInit, STATE.RUNNING);
			Transition(STATE.RUNNING, FSM_MSG.TIMER, fRun, STATE.RUNNING);
			Transition(STATE.RUNNING, MSG.RESET, fReset, STATE.RUNNING);
			mgr = Singleton<T>.Instance;
		}

		public bool Check(int msg, out string reason, params object[] args)
		{
			reason = "";
			return true;
		}

		private bool fInit(object[] objs)
		{
			return true;
		}

		private bool fRun(object[] objs)
		{
			try
			{
				mgr.Monitor();
			}
			catch (Exception ex)
			{
				base.Running = false;
				LOG.Error("Device Run exception", ex);
			}
			base.Running = true;
			return true;
		}

		private bool fReset(object[] objs)
		{
			mgr.Reset();
			return true;
		}
	}
}
