namespace MECF.Framework.Common.Communications
{
	public interface IConnection
	{
		string Address { get; }

		bool IsConnected { get; }

		bool Connect();

		bool Disconnect();
	}
}
