using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistorySecquence : WaferHistoryItem
	{
		[DataMember]
		public string SecquenceName { get; set; }

		[DataMember]
		public string Recipe { get; set; }

		[DataMember]
		public string SecQuenceStartTime { get; set; }

		[DataMember]
		public string SecQuenceEndTime { get; set; }

		[DataMember]
		public string ActualTime { get; set; }
	}
}
