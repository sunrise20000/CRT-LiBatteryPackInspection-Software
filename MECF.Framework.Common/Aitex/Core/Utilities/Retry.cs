using System;
using System.Threading;

namespace Aitex.Core.Utilities
{
	public class Retry
	{
		private bool _result = false;

		private int _retryTime = -1;

		public bool IsSucceeded { get; set; }

		public bool IsErrored { get; set; }

		public bool Result
		{
			get
			{
				return _result;
			}
			set
			{
				if (value)
				{
					IsSucceeded = !_result;
					IsErrored = false;
				}
				else
				{
					IsErrored = _result || _retryTime == -1;
					IsSucceeded = false;
				}
				_result = value;
				_retryTime = ++_retryTime % 100;
			}
		}

		public void Do(Func<bool> function, int time)
		{
			for (int i = 0; i < time; i++)
			{
				if (function())
				{
					Result = true;
				}
				Thread.Sleep(10);
			}
			Result = false;
		}
	}
}
