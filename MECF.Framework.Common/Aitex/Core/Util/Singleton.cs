using System;

namespace Aitex.Core.Util
{
	[Serializable]
	public class Singleton<T> where T : class, new()
	{
		private static volatile T instance;

		private static object locker = new object();

		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					lock (locker)
					{
						if (instance == null)
						{
							instance = new T();
						}
					}
				}
				return instance;
			}
		}
	}
}
