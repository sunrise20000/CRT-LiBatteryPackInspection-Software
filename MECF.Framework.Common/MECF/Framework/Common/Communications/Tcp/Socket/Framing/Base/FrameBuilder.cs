using System;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base
{
	public class FrameBuilder : IFrameBuilder
	{
		public IFrameEncoder Encoder { get; private set; }

		public IFrameDecoder Decoder { get; private set; }

		public FrameBuilder(IFrameEncoder encoder, IFrameDecoder decoder)
		{
			if (encoder == null)
			{
				throw new ArgumentNullException("encoder");
			}
			if (decoder == null)
			{
				throw new ArgumentNullException("decoder");
			}
			Encoder = encoder;
			Decoder = decoder;
		}
	}
}
