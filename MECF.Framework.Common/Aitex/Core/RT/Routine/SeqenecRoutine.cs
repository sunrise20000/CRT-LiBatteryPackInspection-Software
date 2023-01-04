using System;
using System.Collections.Generic;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Routine
{
	public class SeqenecRoutine
	{
		private enum STATE
		{
			IDLE = 0,
			WAIT = 1
		}

		protected DeviceTimer counter = new DeviceTimer();

		protected DeviceTimer delayTimer = new DeviceTimer();

		private int _id;

		private Stack<int> _steps = new Stack<int>();

		private STATE state;

		private int loop = 0;

		private int loopCount = 0;

		private int loopID = 0;

		private DeviceTimer timer = new DeviceTimer();

		protected RoutineResult RoutineToken = new RoutineResult
		{
			Result = RoutineState.Running
		};

		public int TokenId => _id;

		public int LoopCounter => loop;

		public int LoopTotalTime => loopCount;

		public int Elapsed => (int)(timer.GetElapseTime() / 1000.0);

		public void Reset()
		{
			_id = 0;
			_steps.Clear();
			loop = 0;
			loopCount = 0;
			state = STATE.IDLE;
			counter.Start(360000.0);
			RoutineToken.Result = RoutineState.Running;
		}

		protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
		{
			if (Acitve(id))
			{
				result.Result = execution();
			}
		}

		public void StopLoop()
		{
			loop = loopCount;
		}

		public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count)
		{
			int id2 = Convert.ToInt32(id);
			bool flag = Acitve(id2);
			if (flag)
			{
				if (!func())
				{
					return Tuple.Create(flag, Result.FAIL);
				}
				loopID = id2;
				loopCount = count;
				next();
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
		{
			int id2 = Convert.ToInt32(id);
			bool flag = Acitve(id2);
			if (flag)
			{
				if (++loop >= loopCount)
				{
					if (!func())
					{
						return Tuple.Create(flag, Result.FAIL);
					}
					loop = 0;
					loopCount = 0;
					next();
					return Tuple.Create(item1: true, Result.RUN);
				}
				next(loopID);
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
		{
			int id2 = Convert.ToInt32(id);
			if (Acitve(id2))
			{
				if (state == STATE.IDLE)
				{
					switch (routine.Start())
					{
					case Result.FAIL:
						return Tuple.Create(item1: true, Result.FAIL);
					case Result.DONE:
						next();
						return Tuple.Create(item1: true, Result.DONE);
					}
					state = STATE.WAIT;
				}
				Result result = routine.Monitor();
				switch (result)
				{
				case Result.DONE:
					next();
					return Tuple.Create(item1: true, Result.DONE);
				default:
					if (result != Result.TIMEOUT)
					{
						return Tuple.Create(item1: true, Result.RUN);
					}
					goto case Result.FAIL;
				case Result.FAIL:
					return Tuple.Create(item1: true, Result.FAIL);
				}
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
		{
			int id2 = Convert.ToInt32(id);
			if (Acitve(id2))
			{
				if (state == STATE.IDLE)
				{
					foreach (IRoutine routine in routines)
					{
						if (routine.Start() == Result.FAIL)
						{
							return Tuple.Create(item1: true, Result.FAIL);
						}
					}
					state = STATE.WAIT;
				}
				bool flag = false;
				bool flag2 = true;
				foreach (IRoutine routine2 in routines)
				{
					Result result = routine2.Monitor();
					flag2 = flag2 && (result == Result.FAIL || result == Result.DONE);
					flag = flag || result == Result.FAIL;
				}
				if (flag2)
				{
					next();
					if (flag)
					{
						return Tuple.Create(item1: true, Result.FAIL);
					}
					return Tuple.Create(item1: true, Result.DONE);
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> Check<T>(T id, Func<bool> func)
		{
			return Check(Check(Convert.ToInt32(id), func));
		}

		public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)
		{
			return Check(execute(Convert.ToInt32(id), func));
		}

		public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = 2147483647.0)
		{
			return Check(wait(Convert.ToInt32(id), func, timeout));
		}

		public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = 2147483647.0)
		{
			return Check(wait(Convert.ToInt32(id), func, timeout));
		}

		public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, double timeout = 2147483647.0)
		{
			int id2 = Convert.ToInt32(id);
			bool flag = Acitve(id2);
			bool? flag2 = false;
			if (flag)
			{
				if (state == STATE.IDLE)
				{
					if (!execute())
					{
						return Tuple.Create(flag, Result.FAIL);
					}
					timer.Start(timeout);
					state = STATE.WAIT;
				}
				flag2 = check();
				if (!flag2.HasValue)
				{
					return Tuple.Create(flag, Result.FAIL);
				}
				if (flag2.Value)
				{
					next();
					return Tuple.Create(item1: true, Result.RUN);
				}
				if (timer.IsTimeout())
				{
					return Tuple.Create(item1: true, Result.TIMEOUT);
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, Func<double> time)
		{
			int id2 = Convert.ToInt32(id);
			bool flag = Acitve(id2);
			bool? flag2 = false;
			double num = 0.0;
			if (flag)
			{
				if (state == STATE.IDLE)
				{
					num = time();
					if (!execute())
					{
						return Tuple.Create(item1: true, Result.FAIL);
					}
					timer.Start(num);
					state = STATE.WAIT;
				}
				flag2 = check();
				if (!flag2.HasValue)
				{
					return Tuple.Create(item1: true, Result.FAIL);
				}
				if (flag2.Value)
				{
					next();
					return Tuple.Create(item1: true, Result.RUN);
				}
				if (timer.IsTimeout())
				{
					return Tuple.Create(item1: true, Result.TIMEOUT);
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
		{
			int id2 = Convert.ToInt32(id);
			if (Acitve(id2))
			{
				if (state == STATE.IDLE)
				{
					rt.Start();
					state = STATE.WAIT;
				}
				Result item = rt.Monitor();
				return Tuple.Create(item1: true, item);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
		{
			int id2 = Convert.ToInt32(id);
			bool flag = Acitve(id2);
			bool flag2 = false;
			if (flag)
			{
				if (state == STATE.IDLE)
				{
					if (func != null && !func())
					{
						return Tuple.Create(item1: true, Result.FAIL);
					}
					timer.Start(time);
					state = STATE.WAIT;
				}
				if (!check())
				{
					return Tuple.Create(item1: true, Result.FAIL);
				}
				if (timer.IsTimeout())
				{
					next();
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
		{
			int id2 = Convert.ToInt32(id);
			if (Acitve(id2))
			{
				if (state == STATE.IDLE)
				{
					if (func != null && !func())
					{
						return Tuple.Create(item1: true, Result.FAIL);
					}
					timer.Start(time);
					state = STATE.WAIT;
				}
				if (timer.IsTimeout())
				{
					next();
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
		{
			int id2 = Convert.ToInt32(id);
			if (Acitve(id2))
			{
				if (state == STATE.IDLE)
				{
					timer.Start(time);
					state = STATE.WAIT;
				}
				if (timer.IsTimeout())
				{
					if (func != null && !func())
					{
						return Tuple.Create(item1: true, Result.FAIL);
					}
					next();
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		private Tuple<bool, bool> execute(int id, Func<bool> func)
		{
			bool flag = Acitve(id);
			bool flag2 = false;
			if (flag)
			{
				flag2 = func();
				if (flag2)
				{
					next();
				}
			}
			return Tuple.Create(flag, flag2);
		}

		private Tuple<bool, bool> Check(int id, Func<bool> func)
		{
			bool flag = Acitve(id);
			bool item = false;
			if (flag)
			{
				item = func();
				next();
			}
			return Tuple.Create(flag, item);
		}

		private Tuple<bool, bool, bool> wait(int id, Func<bool> func, double timeout = 2147483647.0)
		{
			bool flag = Acitve(id);
			bool flag2 = false;
			bool item = false;
			if (flag)
			{
				if (state == STATE.IDLE)
				{
					timer.Start(timeout);
					state = STATE.WAIT;
				}
				flag2 = func();
				if (flag2)
				{
					next();
				}
				item = timer.IsTimeout();
			}
			return Tuple.Create(flag, flag2, item);
		}

		private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = 2147483647.0)
		{
			bool flag = Acitve(id);
			bool? item = false;
			bool item2 = false;
			if (flag)
			{
				if (state == STATE.IDLE)
				{
					timer.Start(timeout);
					state = STATE.WAIT;
				}
				item = func();
				if (item.HasValue && item.Value)
				{
					next();
				}
				item2 = timer.IsTimeout();
			}
			return Tuple.Create(flag, item, item2);
		}

		private Tuple<bool, Result> Check(Tuple<bool, bool> value)
		{
			if (value.Item1)
			{
				if (!value.Item2)
				{
					return Tuple.Create(item1: true, Result.FAIL);
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
		{
			if (value.Item1)
			{
				if (CheckTimeout(value))
				{
					return Tuple.Create(item1: true, Result.TIMEOUT);
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
		{
			if (value.Item1)
			{
				if (!value.Item2.HasValue)
				{
					return Tuple.Create(item1: true, Result.FAIL);
				}
				if (value.Item2 == false && value.Item3)
				{
					return Tuple.Create(item1: true, Result.TIMEOUT);
				}
				return Tuple.Create(item1: true, Result.RUN);
			}
			return Tuple.Create(item1: false, Result.RUN);
		}

		private bool CheckTimeout(Tuple<bool, bool, bool> value)
		{
			return value.Item1 && !value.Item2 && value.Item3;
		}

		private bool Acitve(int id)
		{
			if (_steps.Contains(id))
			{
				return false;
			}
			_id = id;
			return true;
		}

		private void next()
		{
			_steps.Push(_id);
			state = STATE.IDLE;
		}

		private void next(int step)
		{
			while (_steps.Pop() != step)
			{
			}
			state = STATE.IDLE;
		}

		public void Delay(int id, double delaySeconds)
		{
			Tuple<bool, Result> tuple = Delay(id, () => true, delaySeconds * 1000.0);
			if (tuple.Item1 && tuple.Item2 == Result.RUN)
			{
				throw new RoutineBreakException();
			}
		}

		public bool IsActived(int id)
		{
			return _steps.Contains(id);
		}
	}
}
