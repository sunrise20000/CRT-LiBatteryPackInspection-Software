using System;

namespace MECF.Framework.Common.Utilities
{
	public static class Crc16
	{
		private const ushort polynomial = 40961;

		private static readonly ushort[] table;

		public static ushort ComputeChecksum(byte[] bytes)
		{
			ushort num = 0;
			for (int i = 0; i < bytes.Length; i++)
			{
				byte b = (byte)(num ^ bytes[i]);
				num = (ushort)((num >> 8) ^ table[b]);
			}
			return num;
		}

		static Crc16()
		{
			table = new ushort[256];
			for (ushort num = 0; num < table.Length; num = (ushort)(num + 1))
			{
				ushort num2 = 0;
				ushort num3 = num;
				for (byte b = 0; b < 8; b = (byte)(b + 1))
				{
					num2 = ((((num2 ^ num3) & 1) == 0) ? ((ushort)(num2 >> 1)) : ((ushort)((uint)(num2 >> 1) ^ 0xA001u)));
					num3 = (ushort)(num3 >> 1);
				}
				table[num] = num2;
			}
		}

		public static ushort Crc16Ccitt(byte[] bytes)
		{
			ushort[] array = new ushort[256];
			ushort num = ushort.MaxValue;
			ushort num2 = num;
			for (int i = 0; i < array.Length; i++)
			{
				ushort num3 = 0;
				ushort num4 = (ushort)(i << 8);
				for (int j = 0; j < 8; j++)
				{
					num3 = ((((num3 ^ num4) & 0x8000) == 0) ? ((ushort)(num3 << 1)) : ((ushort)((uint)(num3 << 1) ^ 0x1021u)));
					num4 = (ushort)(num4 << 1);
				}
				array[i] = num3;
			}
			for (int k = 0; k < bytes.Length; k++)
			{
				num2 = (ushort)((num2 << 8) ^ array[(num2 >> 8) ^ (0xFF & bytes[k])]);
			}
			return num2;
		}

		public static ushort CRC16_CCITT(byte[] data)
		{
			int num = data.Length;
			ushort num2 = ushort.MaxValue;
			for (int i = 0; i < num; i++)
			{
				byte b = data[i];
				num2 = (ushort)(num2 ^ (b << 8));
				for (int j = 1; j <= 8; j++)
				{
					num2 = (((num2 & 0x8000) <= 0) ? ((ushort)(num2 << 1)) : ((ushort)((uint)(num2 << 1) ^ 0x1021u)));
				}
			}
			return num2;
		}

		public static ushort CRC16_ModbusRTU(byte[] bytes)
		{
			ushort num = ushort.MaxValue;
			int num2 = 0;
			foreach (ushort value in bytes)
			{
				num = (ushort)(Convert.ToInt32(value) ^ Convert.ToInt32(num));
				ushort value2 = 40961;
				while (num2 < 8)
				{
					if (Convert.ToInt32(num) % 2 == 1)
					{
						num = (ushort)(num - 1);
						num = (ushort)(Convert.ToInt32(num) / 2);
						num2++;
						num = (ushort)(Convert.ToInt32(num) ^ Convert.ToInt32(value2));
					}
					else
					{
						num = (ushort)(Convert.ToInt32(num) / 2);
						num2++;
					}
				}
				num2 = 0;
			}
			return num;
		}
	}
}
