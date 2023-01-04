using System;

namespace MECF.Framework.Common.DataCenter
{
	public class StatsDataItem
	{
		public string Name { get; set; }

		public int Value { get; set; }

		public string Description { get; set; }

		public int Total { get; set; }

		public DateTime LastUpdateTime { get; set; }

		public DateTime LastResetTime { get; set; }

		public DateTime LastResetTotalTime { get; set; }

		public int AlarmValue { get; set; }

		public bool AlarmEnable { get; set; }

		public bool IsVisible { get; set; }
	}
}
