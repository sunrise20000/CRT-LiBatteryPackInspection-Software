using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class RawBufferFrameEncoder : IFrameEncoder
	{
		public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frameBuffer, out int frameBufferOffset, out int frameBufferLength)
		{
			frameBuffer = payload;
			frameBufferOffset = offset;
			frameBufferLength = count;
		}
	}
}
