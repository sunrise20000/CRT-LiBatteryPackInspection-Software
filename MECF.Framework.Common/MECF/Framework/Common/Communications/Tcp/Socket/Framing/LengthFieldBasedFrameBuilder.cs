using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LengthFieldBasedFrameBuilder : FrameBuilder
	{
		public LengthFieldBasedFrameBuilder()
			: this(LengthField.FourBytes)
		{
		}

		public LengthFieldBasedFrameBuilder(LengthField lengthField)
			: this(new LengthFieldBasedFrameEncoder(lengthField), new LengthFieldBasedFrameDecoder(lengthField))
		{
		}

		public LengthFieldBasedFrameBuilder(LengthFieldBasedFrameEncoder encoder, LengthFieldBasedFrameDecoder decoder)
			: base(encoder, decoder)
		{
		}
	}
}
