namespace MECF.Framework.Common.Communications
{
	public class StaticConnectionContext : IConnectionContext
	{
		public bool IsEnabled { get; set; }

		public int RetryConnectIntervalMs { get; set; }

		public int MaxRetryConnectCount { get; set; }

		public bool EnableCheckConnection { get; set; }

		public string Address { get; set; }

		public bool IsAscii { get; set; }

		public string NewLine { get; set; }

		public bool EnableLog { get; set; }
	}
}
