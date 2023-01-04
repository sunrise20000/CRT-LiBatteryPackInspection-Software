using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LengthPrefixedFrameEncoder : IFrameEncoder
	{
		private static readonly Random _rng = new Random(DateTime.UtcNow.Millisecond);

		private static readonly int MaskingKeyLength = 4;

		public bool IsMasked { get; private set; }

		public LengthPrefixedFrameEncoder(bool isMasked = false)
		{
			IsMasked = isMasked;
		}

		public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frameBuffer, out int frameBufferOffset, out int frameBufferLength)
		{
			byte[] array = (frameBuffer = Encode(payload, offset, count, IsMasked));
			frameBufferOffset = 0;
			frameBufferLength = array.Length;
		}

		private static byte[] Encode(byte[] payload, int offset, int count, bool isMasked = false)
		{
			byte[] array;
			if (count < 126)
			{
				array = new byte[1 + (isMasked ? MaskingKeyLength : 0) + count];
				array[0] = (byte)count;
			}
			else if (count < 65536)
			{
				array = new byte[3 + (isMasked ? MaskingKeyLength : 0) + count];
				array[0] = 126;
				array[1] = (byte)(count / 256);
				array[2] = (byte)(count % 256);
			}
			else
			{
				array = new byte[9 + (isMasked ? MaskingKeyLength : 0) + count];
				array[0] = 127;
				int num = count;
				for (int num2 = 8; num2 > 0; num2--)
				{
					array[num2] = (byte)(num % 256);
					num /= 256;
					if (num == 0)
					{
						break;
					}
				}
			}
			if (isMasked)
			{
				array[0] = (byte)(array[0] | 0x80u);
			}
			if (isMasked)
			{
				int num3 = array.Length - (MaskingKeyLength + count);
				for (int i = num3; i < num3 + MaskingKeyLength; i++)
				{
					array[i] = (byte)_rng.Next(0, 255);
				}
				if (count > 0)
				{
					int num4 = array.Length - count;
					for (int j = 0; j < count; j++)
					{
						array[num4 + j] = (byte)(payload[offset + j] ^ array[num3 + j % MaskingKeyLength]);
					}
				}
			}
			else if (count > 0)
			{
				int destinationIndex = array.Length - count;
				Array.Copy(payload, offset, array, destinationIndex, count);
			}
			return array;
		}
	}
}
