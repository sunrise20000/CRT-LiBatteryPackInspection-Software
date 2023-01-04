using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LineBasedFrameEncoder : IFrameEncoder
	{
		private readonly LineDelimiter _delimiter;

		public LineDelimiter LineDelimiter => _delimiter;

		public LineBasedFrameEncoder()
			: this(LineDelimiter.CRLF)
		{
		}

		public LineBasedFrameEncoder(LineDelimiter delimiter)
		{
			if (delimiter == null)
			{
				throw new ArgumentNullException("delimiter");
			}
			_delimiter = delimiter;
		}

		public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frameBuffer, out int frameBufferOffset, out int frameBufferLength)
		{
			byte[] array = new byte[count + _delimiter.DelimiterBytes.Length];
			Array.Copy(payload, offset, array, 0, count);
			Array.Copy(_delimiter.DelimiterBytes, 0, array, count, _delimiter.DelimiterBytes.Length);
			frameBuffer = array;
			frameBufferOffset = 0;
			frameBufferLength = array.Length;
		}
	}
}
