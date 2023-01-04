using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LineBasedFrameBuilder : FrameBuilder
	{
		public LineBasedFrameBuilder()
			: this(new LineBasedFrameEncoder(), new LineBasedFrameDecoder())
		{
		}

		public LineBasedFrameBuilder(LineDelimiter delimiter)
			: this(new LineBasedFrameEncoder(delimiter), new LineBasedFrameDecoder(delimiter))
		{
		}

		public LineBasedFrameBuilder(LineBasedFrameEncoder encoder, LineBasedFrameDecoder decoder)
			: base(encoder, decoder)
		{
		}
	}
}
