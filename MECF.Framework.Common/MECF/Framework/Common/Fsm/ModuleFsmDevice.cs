using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace MECF.Framework.Common.Fsm
{
	public class ModuleFsmDevice : FsmDevice
	{
		private Queue<IRoutine> _routine = new Queue<IRoutine>();

		public bool IsInstalled
		{
			get
			{
				if (!SC.ContainsItem("System.SetUp.Is" + base.Module + "Installed"))
				{
					return true;
				}
				return SC.GetValue<bool>("System.SetUp.Is" + base.Module + "Installed");
			}
		}

		public bool IsOnline { get; set; }

		protected Queue<IRoutine> QueueRoutine => _routine;

		public override bool Initialize()
		{
			return base.Initialize();
		}

		public Result StartRoutine(IRoutine routine, params object[] args)
		{
			QueueRoutine.Clear();
			QueueRoutine.Enqueue(routine);
			return QueueRoutine.Peek().Start(args);
		}

		public Result StartRoutine(IRoutine routine)
		{
			QueueRoutine.Clear();
			QueueRoutine.Enqueue(routine);
			return QueueRoutine.Peek().Start();
		}

		public Result StartRoutine()
		{
			if (_routine.Count == 0)
			{
				return Result.DONE;
			}
			Result result = Result.DONE;
			List<IRoutine> list = _routine.ToList();
			for (int i = 0; i < list.Count; _routine.Dequeue(), i++)
			{
				switch (list[i].Start())
				{
				case Result.DONE:
					continue;
				case Result.FAIL:
					return Result.FAIL;
				}
				break;
			}
			return Result.RUN;
		}

		public Result MonitorRoutine()
		{
			if (_routine.Count == 0)
			{
				return Result.DONE;
			}
			IRoutine routine = _routine.Peek();
			Result result = routine.Monitor();
			if (result == Result.DONE)
			{
				_routine.Dequeue();
				List<IRoutine> list = _routine.ToList();
				for (int i = 0; i < list.Count; _routine.Dequeue(), i++)
				{
					result = list[i].Start();
					switch (result)
					{
					case Result.DONE:
						continue;
					case Result.FAIL:
						return Result.FAIL;
					}
					break;
				}
			}
			return result;
		}

		public void AbortRoutine()
		{
			if (_routine != null)
			{
				_routine.Peek().Abort();
				_routine.Clear();
			}
		}
	}
}
