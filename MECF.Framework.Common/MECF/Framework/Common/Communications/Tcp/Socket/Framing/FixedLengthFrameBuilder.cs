using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class FixedLengthFrameBuilder : FrameBuilder
	{
		public FixedLengthFrameBuilder(int fixedFrameLength)
			: this(new FixedLengthFrameEncoder(fixedFrameLength), new FixedLengthFrameDecoder(fixedFrameLength))
		{
		}

		public FixedLengthFrameBuilder(FixedLengthFrameEncoder encoder, FixedLengthFrameDecoder decoder)
			: base(encoder, decoder)
		{
		}
	}
}
