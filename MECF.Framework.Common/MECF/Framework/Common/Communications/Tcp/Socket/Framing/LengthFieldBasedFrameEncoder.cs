using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LengthFieldBasedFrameEncoder : IFrameEncoder
	{
		public LengthField LengthField { get; private set; }

		public LengthFieldBasedFrameEncoder(LengthField lengthField)
		{
			LengthField = lengthField;
		}

		public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frameBuffer, out int frameBufferOffset, out int frameBufferLength)
		{
			byte[] array = null;
			switch (LengthField)
			{
			case LengthField.OneByte:
				if (count > 255)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				array = new byte[1 + count];
				array[0] = (byte)count;
				Array.Copy(payload, offset, array, 1, count);
				break;
			case LengthField.TwoBytes:
				if (count > 32767)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				array = new byte[2 + count];
				array[0] = (byte)((ushort)count >> 8);
				array[1] = (byte)count;
				Array.Copy(payload, offset, array, 2, count);
				break;
			case LengthField.FourBytes:
				array = new byte[4 + count];
				array[0] = (byte)((uint)count >> 24);
				array[1] = (byte)((uint)count >> 16);
				array[2] = (byte)((uint)count >> 8);
				array[3] = (byte)count;
				Array.Copy(payload, offset, array, 4, count);
				break;
			case LengthField.EigthBytes:
			{
				array = new byte[8 + count];
				ulong num = (ulong)count;
				array[0] = (byte)(num >> 56);
				array[1] = (byte)(num >> 48);
				array[2] = (byte)(num >> 40);
				array[3] = (byte)(num >> 32);
				array[4] = (byte)(num >> 24);
				array[5] = (byte)(num >> 16);
				array[6] = (byte)(num >> 8);
				array[7] = (byte)num;
				Array.Copy(payload, offset, array, 8, count);
				break;
			}
			default:
				throw new NotSupportedException("Specified length field is not supported.");
			}
			frameBuffer = array;
			frameBufferOffset = 0;
			frameBufferLength = array.Length;
		}
	}
}
