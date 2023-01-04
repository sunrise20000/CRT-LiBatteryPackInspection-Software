namespace MECF.Framework.Common.Communications
{
	public interface IConnectionContext
	{
		bool IsEnabled { get; }

		int RetryConnectIntervalMs { get; }

		int MaxRetryConnectCount { get; }

		bool EnableCheckConnection { get; }

		string Address { get; }

		bool IsAscii { get; }

		string NewLine { get; }

		bool EnableLog { get; }
	}
}
