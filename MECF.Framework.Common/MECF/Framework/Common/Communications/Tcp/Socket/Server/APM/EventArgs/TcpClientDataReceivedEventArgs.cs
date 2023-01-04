using System;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs
{
	public class TcpClientDataReceivedEventArgs : System.EventArgs
	{
		public TcpSocketSession Session { get; private set; }

		public byte[] Data { get; private set; }

		public int DataOffset { get; private set; }

		public int DataLength { get; private set; }

		public TcpClientDataReceivedEventArgs(TcpSocketSession session, byte[] data)
			: this(session, data, 0, data.Length)
		{
		}

		public TcpClientDataReceivedEventArgs(TcpSocketSession session, byte[] data, int dataOffset, int dataLength)
		{
			Session = session;
			Data = data;
			DataOffset = dataOffset;
			DataLength = dataLength;
		}

		public override string ToString()
		{
			return $"{Session}";
		}
	}
}
