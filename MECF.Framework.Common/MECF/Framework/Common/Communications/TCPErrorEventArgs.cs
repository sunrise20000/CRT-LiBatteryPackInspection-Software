using System;

namespace MECF.Framework.Common.Communications
{
	public class TCPErrorEventArgs : EventArgs
	{
		public readonly string Reason;

		public readonly string Code;

		public TCPErrorEventArgs(string reason, string code = "")
		{
			Reason = reason;
			Code = code;
		}
	}
}
