using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class RawBufferFrameDecoder : IFrameDecoder
	{
		public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount)
		{
			frameLength = 0;
			payload = null;
			payloadOffset = 0;
			payloadCount = 0;
			if (count <= 0)
			{
				return false;
			}
			frameLength = count;
			payload = buffer;
			payloadOffset = offset;
			payloadCount = count;
			return true;
		}
	}
}
