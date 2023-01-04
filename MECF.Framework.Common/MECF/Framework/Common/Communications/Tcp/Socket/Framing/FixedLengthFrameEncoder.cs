using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class FixedLengthFrameEncoder : IFrameEncoder
	{
		private readonly int _fixedFrameLength;

		public int FixedFrameLength => _fixedFrameLength;

		public FixedLengthFrameEncoder(int fixedFrameLength)
		{
			if (fixedFrameLength <= 0)
			{
				throw new ArgumentOutOfRangeException("fixedFrameLength");
			}
			_fixedFrameLength = fixedFrameLength;
		}

		public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frameBuffer, out int frameBufferOffset, out int frameBufferLength)
		{
			if (count == FixedFrameLength)
			{
				frameBuffer = payload;
				frameBufferOffset = offset;
				frameBufferLength = count;
				return;
			}
			byte[] array = new byte[FixedFrameLength];
			if (count >= FixedFrameLength)
			{
				Array.Copy(payload, offset, array, 0, FixedFrameLength);
			}
			else
			{
				Array.Copy(payload, offset, array, 0, count);
				for (int i = 0; i < FixedFrameLength - count; i++)
				{
					array[count + i] = 10;
				}
			}
			frameBuffer = array;
			frameBufferOffset = 0;
			frameBufferLength = array.Length;
		}
	}
}
