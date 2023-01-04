using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class RawBufferFrameBuilder : FrameBuilder
	{
		public RawBufferFrameBuilder()
			: this(new RawBufferFrameEncoder(), new RawBufferFrameDecoder())
		{
		}

		public RawBufferFrameBuilder(RawBufferFrameEncoder encoder, RawBufferFrameDecoder decoder)
			: base(encoder, decoder)
		{
		}
	}
}
