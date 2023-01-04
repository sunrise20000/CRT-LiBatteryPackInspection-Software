using System;
using System.Linq;
using System.Net;
using System.Text;

namespace MECF.Framework.Common.Utilities
{
	public static class ModbusUtility
	{
		private static readonly ushort[] CrcTable = new ushort[256]
		{
			0, 49345, 49537, 320, 49921, 960, 640, 49729, 50689, 1728,
			1920, 51009, 1280, 50625, 50305, 1088, 52225, 3264, 3456, 52545,
			3840, 53185, 52865, 3648, 2560, 51905, 52097, 2880, 51457, 2496,
			2176, 51265, 55297, 6336, 6528, 55617, 6912, 56257, 55937, 6720,
			7680, 57025, 57217, 8000, 56577, 7616, 7296, 56385, 5120, 54465,
			54657, 5440, 55041, 6080, 5760, 54849, 53761, 4800, 4992, 54081,
			4352, 53697, 53377, 4160, 61441, 12480, 12672, 61761, 13056, 62401,
			62081, 12864, 13824, 63169, 63361, 14144, 62721, 13760, 13440, 62529,
			15360, 64705, 64897, 15680, 65281, 16320, 16000, 65089, 64001, 15040,
			15232, 64321, 14592, 63937, 63617, 14400, 10240, 59585, 59777, 10560,
			60161, 11200, 10880, 59969, 60929, 11968, 12160, 61249, 11520, 60865,
			60545, 11328, 58369, 9408, 9600, 58689, 9984, 59329, 59009, 9792,
			8704, 58049, 58241, 9024, 57601, 8640, 8320, 57409, 40961, 24768,
			24960, 41281, 25344, 41921, 41601, 25152, 26112, 42689, 42881, 26432,
			42241, 26048, 25728, 42049, 27648, 44225, 44417, 27968, 44801, 28608,
			28288, 44609, 43521, 27328, 27520, 43841, 26880, 43457, 43137, 26688,
			30720, 47297, 47489, 31040, 47873, 31680, 31360, 47681, 48641, 32448,
			32640, 48961, 32000, 48577, 48257, 31808, 46081, 29888, 30080, 46401,
			30464, 47041, 46721, 30272, 29184, 45761, 45953, 29504, 45313, 29120,
			28800, 45121, 20480, 37057, 37249, 20800, 37633, 21440, 21120, 37441,
			38401, 22208, 22400, 38721, 21760, 38337, 38017, 21568, 39937, 23744,
			23936, 40257, 24320, 40897, 40577, 24128, 23040, 39617, 39809, 23360,
			39169, 22976, 22656, 38977, 34817, 18624, 18816, 35137, 19200, 35777,
			35457, 19008, 19968, 36545, 36737, 20288, 36097, 19904, 19584, 35905,
			17408, 33985, 34177, 17728, 34561, 18368, 18048, 34369, 33281, 17088,
			17280, 33601, 16640, 33217, 32897, 16448
		};

		public static double GetDouble(ushort b3, ushort b2, ushort b1, ushort b0)
		{
			byte[] value = BitConverter.GetBytes(b0).Concat(BitConverter.GetBytes(b1)).Concat(BitConverter.GetBytes(b2))
				.Concat(BitConverter.GetBytes(b3))
				.ToArray();
			return BitConverter.ToDouble(value, 0);
		}

		public static float GetSingle(ushort highOrderValue, ushort lowOrderValue)
		{
			byte[] value = BitConverter.GetBytes(lowOrderValue).Concat(BitConverter.GetBytes(highOrderValue)).ToArray();
			return BitConverter.ToSingle(value, 0);
		}

		public static uint GetUInt32(ushort highOrderValue, ushort lowOrderValue)
		{
			byte[] value = BitConverter.GetBytes(lowOrderValue).Concat(BitConverter.GetBytes(highOrderValue)).ToArray();
			return BitConverter.ToUInt32(value, 0);
		}

		public static byte[] GetAsciiBytes(params byte[] numbers)
		{
			return Encoding.UTF8.GetBytes(numbers.SelectMany((byte n) => n.ToString("X2")).ToArray());
		}

		public static byte[] GetAsciiBytes(params ushort[] numbers)
		{
			return Encoding.UTF8.GetBytes(numbers.SelectMany((ushort n) => n.ToString("X4")).ToArray());
		}

		public static ushort[] NetworkBytesToHostUInt16(byte[] networkBytes)
		{
			if (networkBytes == null)
			{
				throw new ArgumentNullException("networkBytes");
			}
			if (networkBytes.Length % 2 != 0)
			{
				throw new FormatException("NetworkBytesNotEven");
			}
			ushort[] array = new ushort[networkBytes.Length / 2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(networkBytes, i * 2));
			}
			return array;
		}

		public static byte[] HexToBytes(string hex)
		{
			if (hex == null)
			{
				throw new ArgumentNullException("hex");
			}
			if (hex.Length % 2 != 0)
			{
				throw new FormatException("HexCharacterCountNotEven");
			}
			byte[] array = new byte[hex.Length / 2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
			}
			return array;
		}

		public static byte CalculateLrc(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			byte b = 0;
			foreach (byte b2 in data)
			{
				b = (byte)(b + b2);
			}
			return (byte)((b ^ 0xFF) + 1);
		}

		public static byte[] CalculateCrc(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			ushort num = ushort.MaxValue;
			foreach (byte b in data)
			{
				byte b2 = (byte)(num ^ b);
				num = (ushort)(num >> 8);
				num = (ushort)(num ^ CrcTable[b2]);
			}
			return BitConverter.GetBytes(num);
		}
	}
}
