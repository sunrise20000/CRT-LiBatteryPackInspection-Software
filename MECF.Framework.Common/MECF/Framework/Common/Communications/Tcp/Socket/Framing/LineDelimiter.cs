using System;
using System.Text;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing
{
	public class LineDelimiter : IEquatable<LineDelimiter>
	{
		public static readonly LineDelimiter CRLF = new LineDelimiter("\r\n");

		public static readonly LineDelimiter UNIX = new LineDelimiter("\n");

		public static readonly LineDelimiter MAC = new LineDelimiter("\r");

		public static readonly LineDelimiter WINDOWS = CRLF;

		public string DelimiterString { get; private set; }

		public char[] DelimiterChars { get; private set; }

		public byte[] DelimiterBytes { get; private set; }

		public LineDelimiter(string delimiter)
		{
			DelimiterString = delimiter;
			DelimiterChars = DelimiterString.ToCharArray();
			DelimiterBytes = Encoding.UTF8.GetBytes(DelimiterChars);
		}

		public bool Equals(LineDelimiter other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return StringComparer.OrdinalIgnoreCase.Compare(DelimiterString, other.DelimiterString) == 0;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as LineDelimiter);
		}

		public override int GetHashCode()
		{
			return DelimiterString.GetHashCode();
		}

		public override string ToString()
		{
			return DelimiterString;
		}
	}
}
