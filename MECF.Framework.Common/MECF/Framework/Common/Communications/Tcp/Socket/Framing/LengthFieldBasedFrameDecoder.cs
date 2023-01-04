using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LengthFieldBasedFrameDecoder : IFrameDecoder
	{
		public LengthField LengthField { get; private set; }

		public LengthFieldBasedFrameDecoder(LengthField lengthField)
		{
			LengthField = lengthField;
		}

		public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount)
		{
			frameLength = 0;
			payload = null;
			payloadOffset = 0;
			payloadCount = 0;
			byte[] array = null;
			long num = 0L;
			switch (LengthField)
			{
			case LengthField.OneByte:
				if (count < 1)
				{
					return false;
				}
				num = buffer[offset];
				if (count - 1 < num)
				{
					return false;
				}
				array = new byte[num];
				Array.Copy(buffer, offset + 1, array, 0L, num);
				break;
			case LengthField.TwoBytes:
				if (count < 2)
				{
					return false;
				}
				num = (short)((buffer[offset] << 8) | buffer[offset + 1]);
				if (count - 2 < num)
				{
					return false;
				}
				array = new byte[num];
				Array.Copy(buffer, offset + 2, array, 0L, num);
				break;
			case LengthField.FourBytes:
				if (count < 4)
				{
					return false;
				}
				num = (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
				if (count - 4 < num)
				{
					return false;
				}
				array = new byte[num];
				Array.Copy(buffer, offset + 4, array, 0L, num);
				break;
			case LengthField.EigthBytes:
			{
				if (count < 8)
				{
					return false;
				}
				int num2 = (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
				int num3 = (buffer[offset + 4] << 24) | (buffer[offset + 5] << 16) | (buffer[offset + 6] << 8) | buffer[offset + 7];
				num = (uint)num3 | ((long)num2 << 32);
				if (count - 8 < num)
				{
					return false;
				}
				array = new byte[num];
				Array.Copy(buffer, offset + 8, array, 0L, num);
				break;
			}
			default:
				throw new NotSupportedException("Specified length field is not supported.");
			}
			payload = array;
			payloadOffset = 0;
			payloadCount = array.Length;
			frameLength = (int)(LengthField + array.Length);
			return true;
		}
	}
}
