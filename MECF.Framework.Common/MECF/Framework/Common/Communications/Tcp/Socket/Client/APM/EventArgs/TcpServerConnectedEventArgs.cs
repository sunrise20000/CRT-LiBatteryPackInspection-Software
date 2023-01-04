using System;
using System.Net;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Client.APM.EventArgs
{
	public class TcpServerConnectedEventArgs : System.EventArgs
	{
		public IPEndPoint RemoteEndPoint { get; private set; }

		public IPEndPoint LocalEndPoint { get; private set; }

		public TcpServerConnectedEventArgs(IPEndPoint remoteEP)
			: this(remoteEP, null)
		{
		}

		public TcpServerConnectedEventArgs(IPEndPoint remoteEP, IPEndPoint localEP)
		{
			if (remoteEP == null)
			{
				throw new ArgumentNullException("remoteEP");
			}
			RemoteEndPoint = remoteEP;
			LocalEndPoint = localEP;
		}

		public override string ToString()
		{
			return RemoteEndPoint.ToString();
		}
	}
}
