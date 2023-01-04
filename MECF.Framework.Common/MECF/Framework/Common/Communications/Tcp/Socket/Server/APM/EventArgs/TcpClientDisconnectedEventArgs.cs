using System;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs
{
	public class TcpClientDisconnectedEventArgs : System.EventArgs
	{
		public TcpSocketSession Session { get; private set; }

		public TcpClientDisconnectedEventArgs(TcpSocketSession session)
		{
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			Session = session;
		}

		public override string ToString()
		{
			return $"{Session}";
		}
	}
}
