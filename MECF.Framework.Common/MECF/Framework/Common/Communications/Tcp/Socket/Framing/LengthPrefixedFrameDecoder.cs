using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public sealed class LengthPrefixedFrameDecoder : IFrameDecoder
	{
		internal sealed class Header
		{
			public bool IsMasked { get; set; }

			public int PayloadLength { get; set; }

			public int MaskingKeyOffset { get; set; }

			public int Length { get; set; }

			public override string ToString()
			{
				return $"IsMasked[{IsMasked}], PayloadLength[{PayloadLength}], MaskingKeyOffset[{MaskingKeyOffset}], Length[{Length}]";
			}
		}

		private static readonly int MaskingKeyLength = 4;

		public bool IsMasked { get; private set; }

		public LengthPrefixedFrameDecoder(bool isMasked = false)
		{
			IsMasked = isMasked;
		}

		public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount)
		{
			frameLength = 0;
			payload = null;
			payloadOffset = 0;
			payloadCount = 0;
			Header header = DecodeHeader(buffer, offset, count);
			if (header != null && header.Length + header.PayloadLength <= count)
			{
				if (IsMasked)
				{
					payload = DecodeMaskedPayload(buffer, offset, header.MaskingKeyOffset, header.Length, header.PayloadLength);
					payloadOffset = 0;
					payloadCount = payload.Length;
				}
				else
				{
					payload = buffer;
					payloadOffset = offset + header.Length;
					payloadCount = header.PayloadLength;
				}
				frameLength = header.Length + header.PayloadLength;
				return true;
			}
			return false;
		}

		private static Header DecodeHeader(byte[] buffer, int offset, int count)
		{
			if (count < 1)
			{
				return null;
			}
			Header header = new Header
			{
				IsMasked = ((buffer[offset] & 0x80) == 128),
				PayloadLength = (buffer[offset] & 0x7F),
				Length = 1
			};
			if (header.PayloadLength >= 126)
			{
				if (header.PayloadLength == 126)
				{
					header.Length += 2;
				}
				else
				{
					header.Length += 8;
				}
				if (count < header.Length)
				{
					return null;
				}
				if (header.PayloadLength == 126)
				{
					header.PayloadLength = buffer[offset + 1] * 256 + buffer[offset + 2];
				}
				else
				{
					int num = 0;
					int num2 = 1;
					for (int num3 = 7; num3 >= 0; num3--)
					{
						num += buffer[offset + num3 + 1] * num2;
						num2 *= 256;
					}
					header.PayloadLength = num;
				}
			}
			if (header.IsMasked)
			{
				if (count < header.Length + MaskingKeyLength)
				{
					return null;
				}
				header.MaskingKeyOffset = header.Length;
				header.Length += MaskingKeyLength;
			}
			return header;
		}

		private static byte[] DecodeMaskedPayload(byte[] buffer, int offset, int maskingKeyOffset, int payloadOffset, int payloadCount)
		{
			byte[] array = new byte[payloadCount];
			for (int i = 0; i < payloadCount; i++)
			{
				array[i] = (byte)(buffer[offset + payloadOffset + i] ^ buffer[offset + maskingKeyOffset + i % MaskingKeyLength]);
			}
			return array;
		}
	}
}
