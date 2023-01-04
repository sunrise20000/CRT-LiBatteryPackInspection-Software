using System;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LineBasedFrameDecoder : IFrameDecoder
	{
		private readonly LineDelimiter _delimiter;

		public LineDelimiter LineDelimiter => _delimiter;

		public LineBasedFrameDecoder()
			: this(LineDelimiter.CRLF)
		{
		}

		public LineBasedFrameDecoder(LineDelimiter delimiter)
		{
			if (delimiter == null)
			{
				throw new ArgumentNullException("delimiter");
			}
			_delimiter = delimiter;
		}

		public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount)
		{
			frameLength = 0;
			payload = null;
			payloadOffset = 0;
			payloadCount = 0;
			if (count < _delimiter.DelimiterBytes.Length)
			{
				return false;
			}
			byte[] delimiterBytes = _delimiter.DelimiterBytes;
			bool flag = false;
			for (int i = 0; i < count; i++)
			{
				for (int j = 0; j < delimiterBytes.Length; j++)
				{
					if (i + j < count && buffer[offset + i + j] == delimiterBytes[j])
					{
						flag = true;
						continue;
					}
					flag = false;
					break;
				}
				if (flag)
				{
					frameLength = i + delimiterBytes.Length;
					payload = buffer;
					payloadOffset = offset;
					payloadCount = i;
					return true;
				}
			}
			return false;
		}
	}
}
