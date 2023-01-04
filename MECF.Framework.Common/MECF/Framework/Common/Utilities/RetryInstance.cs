using System;
using System.Collections.Generic;
using System.Threading;
using Aitex.Core.RT.Event;

namespace MECF.Framework.Common.Utilities
{
	public class RetryInstance
	{
		public static RetryInstance Instance()
		{
			return new RetryInstance();
		}

		public TResult Execute<TResult>(Func<TResult> action, int secondsInterval, int retryCount, TResult expectedResult, bool isSuppressException = true)
		{
			TResult val = default(TResult);
			List<Exception> list = new List<Exception>();
			if (retryCount == 0)
			{
				retryCount = int.MaxValue;
			}
			for (int i = 0; i < retryCount; i++)
			{
				try
				{
					if (i > 0)
					{
						Thread.Sleep(secondsInterval * 1000);
					}
					EV.PostInfoLog(action.ToString(), $"Retry Instance executing {i + 1} times, result : {val}");
					val = action();
				}
				catch (Exception item)
				{
					list.Add(item);
				}
				if (val.Equals(expectedResult))
				{
					return val;
				}
			}
			if (!isSuppressException)
			{
				throw new AggregateException(list);
			}
			return val;
		}
	}
}
