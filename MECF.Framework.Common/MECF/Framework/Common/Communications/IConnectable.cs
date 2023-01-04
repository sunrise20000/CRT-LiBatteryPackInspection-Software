using System;

namespace MECF.Framework.Common.Communications
{
	public interface IConnectable
	{
		event Action<string> OnCommunicationError;

		event Action<string> OnAsciiDataReceived;

		event Action<byte[]> OnBinaryDataReceived;

		bool Connect(out string reason);

		bool Disconnect(out string reason);

		bool CheckIsConnected();

		bool SendBinaryData(byte[] data);

		bool SendAsciiData(string data);
	}
}
