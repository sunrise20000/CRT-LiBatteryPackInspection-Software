using System.Collections.Generic;

namespace Aitex.Core.RT.Routine
{
	public class QueueRoutine
	{
		private Queue<IRoutine> _steps = new Queue<IRoutine>();

		private IRoutine _currentStep = null;

		public void Add(IRoutine step)
		{
			_steps.Enqueue(step);
		}

		public void Reset()
		{
			_steps.Clear();
			_currentStep = null;
		}

		public void Abort()
		{
			if (_currentStep != null)
			{
				_currentStep.Abort();
			}
			Reset();
		}

		public Result Start(params object[] objs)
		{
			if (_steps.Count == 0)
			{
				return Result.DONE;
			}
			_currentStep = _steps.Dequeue();
			return _currentStep.Start(objs);
		}

		public Result Monitor(params object[] objs)
		{
			Result result = Result.FAIL;
			if (_currentStep != null)
			{
				result = _currentStep.Monitor();
			}
			if (result == Result.DONE)
			{
				_currentStep = ((_steps.Count > 0) ? _steps.Dequeue() : null);
				if (_currentStep != null)
				{
					return _currentStep.Start(objs);
				}
			}
			return result;
		}
	}
}
