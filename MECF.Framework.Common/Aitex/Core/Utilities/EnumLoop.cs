using System;

namespace Aitex.Core.Utilities
{
	public class EnumLoop<Key> where Key : struct, IConvertible
	{
		private static readonly Key[] arr = (Key[])Enum.GetValues(typeof(Key));

		public static void ForEach(Action<Key> act)
		{
			for (int i = 0; i < arr.Length; i++)
			{
				act(arr[i]);
			}
		}
	}
}
