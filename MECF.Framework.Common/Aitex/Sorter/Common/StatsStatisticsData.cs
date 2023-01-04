using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class StatsStatisticsData
	{
		[DataMember]
		public string Date { get; set; }

		[DataMember]
		public string Unknown { get; set; }

		[DataMember]
		public string Setup { get; set; }

		[DataMember]
		public string Idle { get; set; }

		[DataMember]
		public string Ready { get; set; }

		[DataMember]
		public string Executing { get; set; }

		[DataMember]
		public string Pause { get; set; }
	}
}
