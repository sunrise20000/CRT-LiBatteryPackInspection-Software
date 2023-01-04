using System;

namespace Aitex.Core.RT.Fsm
{
	public class DemoEntity : Entity
	{
		public enum STATE
		{
			IDLE = 0,
			RUN = 1,
			ERROR = 2
		}

		public enum MSG
		{
			INIT = 0,
			RUN = 1,
			STOP = 2,
			ERROR = 3
		}

		public static void Testcase()
		{
			DemoEntity demoEntity = new DemoEntity();
			demoEntity.Initialize();
			Console.WriteLine("DemoEntity Unit Test.");
			Console.WriteLine("Input \"exit\" for terminate.");
			string text;
			while ((text = Console.ReadLine()) != "exit")
			{
				switch (text)
				{
				case "run":
					demoEntity.PostMsg(MSG.RUN, null);
					break;
				case "init":
					demoEntity.PostMsg(MSG.INIT, null);
					break;
				case "stop":
					demoEntity.PostMsg(MSG.STOP, null);
					break;
				case "error":
					demoEntity.PostMsg(MSG.ERROR, null);
					break;
				}
			}
			demoEntity.Terminate();
			Console.WriteLine("Input any key for quite.");
			Console.Read();
			Console.WriteLine("DemoEntity Unit Test end");
		}

		public DemoEntity()
		{
			fsm = new StateMachine<DemoEntity>("Demo", 0, 3000);
			EnterExitTransition<STATE, MSG>(STATE.IDLE, EnterIdel, null, null);
			AnyStateTransition(MSG.ERROR, Error, STATE.ERROR);
			Transition(STATE.IDLE, MSG.RUN, Run, STATE.RUN);
			Transition(STATE.ERROR, MSG.INIT, Init, STATE.IDLE);
			Transition(STATE.RUN, MSG.STOP, null, STATE.IDLE);
			Transition(STATE.RUN, FSM_MSG.TIMER, OnTimeout, STATE.RUN);
		}

		private bool Run(object[] objs)
		{
			Console.WriteLine("Demo Run");
			return true;
		}

		private bool Init(object[] objs)
		{
			Console.WriteLine("Demo Init");
			return true;
		}

		private bool Error(object[] objs)
		{
			Console.WriteLine("Demo Error");
			return true;
		}

		private bool EnterIdel(object[] objs)
		{
			Console.WriteLine("Enter idle state");
			return true;
		}

		private bool OnTimeout(object[] objs)
		{
			if (fsm.ElapsedTime > 10000)
			{
				Console.WriteLine("Demo run is timeout");
				PostMsg(MSG.ERROR);
			}
			else
			{
				Console.WriteLine("Demo run recive timer msg");
			}
			return true;
		}
	}
}
