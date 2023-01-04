using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LengthPrefixedFrameBuilder : FrameBuilder
	{
		public LengthPrefixedFrameBuilder(bool isMasked = false)
			: this(new LengthPrefixedFrameEncoder(isMasked), new LengthPrefixedFrameDecoder(isMasked))
		{
		}

		public LengthPrefixedFrameBuilder(LengthPrefixedFrameEncoder encoder, LengthPrefixedFrameDecoder decoder)
			: base(encoder, decoder)
		{
		}
	}
}
