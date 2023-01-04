using System;

namespace Aitex.Core.Util
{
	public static class ArrayUtil
	{
		public static byte[] CombomBinaryArray(byte[] srcArray1, byte[] srcArray2)
		{
			byte[] array = new byte[srcArray1.Length + srcArray2.Length];
			Array.Copy(srcArray1, 0, array, 0, srcArray1.Length);
			Array.Copy(srcArray2, 0, array, srcArray1.Length, srcArray2.Length);
			return array;
		}
	}
}
