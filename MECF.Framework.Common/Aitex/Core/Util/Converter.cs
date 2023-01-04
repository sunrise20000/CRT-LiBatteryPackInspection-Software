using System;

namespace Aitex.Core.Util
{
	public static class Converter
	{
		public static double Phy2Logic(double physicalValue, double logicalMin, double logicalMax, double physicalMin, double physicalMax)
		{
			double num = (logicalMax - logicalMin) * (physicalValue - physicalMin) / (physicalMax - physicalMin) + logicalMin;
			if (double.IsNaN(num))
			{
				throw new DivideByZeroException("Phy2Logic除0操作");
			}
			return num;
		}

		public static double Logic2Phy(double logicalValue, double logicalMin, double logicalMax, double physicalMin, double physicalMax)
		{
			double num = (physicalMax - physicalMin) * (logicalValue - logicalMin) / (logicalMax - logicalMin) + physicalMin;
			if (num < physicalMin)
			{
				num = physicalMin;
			}
			if (num > physicalMax)
			{
				num = physicalMax;
			}
			if (double.IsNaN(num))
			{
				throw new DivideByZeroException("Logic2Phy除0操作");
			}
			return num;
		}

		public static float TwoUInt16ToFloat(ushort high, ushort low)
		{
			int value = (high << 16) + (low & 0xFFFF);
			byte[] bytes = BitConverter.GetBytes(value);
			return BitConverter.ToSingle(bytes, 0);
		}

		public static int TwoInt16ToInt32(short high, short low)
		{
			uint num = (uint)((ushort)low + ((ushort)high << 16));
			return (int)((num > int.MaxValue) ? ((~num + 1) * -1) : num);
		}

		public static float TwoInt16ToFloat(short high, short low)
		{
			int value = (high << 16) + (low & 0xFFFF);
			byte[] bytes = BitConverter.GetBytes(value);
			return BitConverter.ToSingle(bytes, 0);
		}
	}
}
