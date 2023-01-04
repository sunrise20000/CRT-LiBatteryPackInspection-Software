using System;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.DataCenter
{
	public class DataItem<T>
	{
		private readonly Func<T> _getter;

		public T Value
		{
			get
			{
				try
				{
					return _getter();
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
				return default(T);
			}
		}

		public DataItem(Func<T> getter)
		{
			if (getter == null)
			{
				throw new ArgumentNullException("getter");
			}
			_getter = getter;
		}
	}
}
