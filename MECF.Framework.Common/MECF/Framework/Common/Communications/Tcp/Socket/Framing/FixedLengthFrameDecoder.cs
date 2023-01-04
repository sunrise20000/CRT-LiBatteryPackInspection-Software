using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class FixedLengthFrameDecoder : IFrameDecoder
	{
		private readonly int _fixedFrameLength;

		public int FixedFrameLength => _fixedFrameLength;

		public FixedLengthFrameDecoder(int fixedFrameLength)
		{
			if (fixedFrameLength <= 0)
			{
				throw new ArgumentOutOfRangeException("fixedFrameLength");
			}
			_fixedFrameLength = fixedFrameLength;
		}

		public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount)
		{
			frameLength = 0;
			payload = null;
			payloadOffset = 0;
			payloadCount = 0;
			if (count < FixedFrameLength)
			{
				return false;
			}
			frameLength = FixedFrameLength;
			payload = buffer;
			payloadOffset = offset;
			payloadCount = FixedFrameLength;
			return true;
		}
	}
}
