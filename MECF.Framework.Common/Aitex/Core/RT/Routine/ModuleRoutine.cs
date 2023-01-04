using System;
using Aitex.Core.RT.Event;

namespace Aitex.Core.RT.Routine
{
	public class ModuleRoutine : SeqenecRoutine
	{
		public delegate RoutineState RoutineFunc<T>(T arg);

		protected string _stepName;

		protected TimeSpan _stepSpan = new TimeSpan(0, 0, 0, 0);

		protected DateTime _stepStartTime;

		private static object locker = new object();

		public string Module { get; set; }

		public string Name { get; set; }

		public string StepName => _stepName;

		public TimeSpan StepLeftTime
		{
			get
			{
				if (_stepSpan.TotalMilliseconds < 1.0)
				{
					return _stepSpan;
				}
				return _stepSpan - (DateTime.Now - _stepStartTime);
			}
		}

		protected void Notify(string message)
		{
			EV.PostInfoLog(Module, Module + " " + Name + ", " + message);
		}

		protected void Stop(string failReason)
		{
			EV.PostAlarmLog(Module, Module + " " + Name + " failed, " + failReason);
		}

		public void Abort(string failReason)
		{
			EV.PostAlarmLog(Module, Module + " " + Name + "  " + failReason);
		}

		public void TimeDelay(int id, string stepName, double time)
		{
			Tuple<bool, Result> tuple = Delay(id, delegate
			{
				Notify($"Delay {time} seconds");
				_stepSpan = new TimeSpan(0, 0, 0, (int)time);
				_stepStartTime = DateTime.Now;
				_stepName = stepName;
				return true;
			}, time * 1000.0);
			if (tuple.Item1 && tuple.Item2 == Result.RUN)
			{
				throw new RoutineBreakException();
			}
		}

		public void TimeDelay(int id, double time)
		{
			Tuple<bool, Result> tuple = Delay(id, delegate
			{
				Notify($"Delay {time} seconds");
				_stepSpan = new TimeSpan(0, 0, 0, (int)time);
				_stepStartTime = DateTime.Now;
				return true;
			}, time * 1000.0);
			if (tuple.Item1 && tuple.Item2 == Result.RUN)
			{
				throw new RoutineBreakException();
			}
		}

		protected void ExecuteRoutine(int id, IRoutine routine)
		{
			Tuple<bool, Result> tuple = ExecuteAndWait(id, routine);
			if (tuple.Item1)
			{
				if (tuple.Item2 == Result.FAIL)
				{
					throw new RoutineFaildException();
				}
				if (tuple.Item2 == Result.RUN)
				{
					throw new RoutineBreakException();
				}
			}
		}

		protected void PerformStep<T, T1>(T step, RoutineFunc<T1> func, T1 param1)
		{
			int num = Convert.ToInt32(step);
			RoutineState routineState = func(param1);
		}

		public void Loop(int id, int count)
		{
			Tuple<bool, Result> tuple = Loop(id, delegate
			{
				Notify($"Start loop {base.LoopCounter + 1}/{count}");
				return true;
			}, count);
			if (tuple.Item1 && tuple.Item2 == Result.FAIL)
			{
				throw new RoutineFaildException();
			}
		}

		public void EndLoop(int id)
		{
			Tuple<bool, Result> tuple = EndLoop(id, delegate
			{
				Notify("Loop finished");
				return true;
			});
			if (tuple.Item1)
			{
				throw new RoutineBreakException();
			}
		}
	}
}
