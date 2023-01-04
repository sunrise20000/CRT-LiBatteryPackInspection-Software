using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryJobMoveData
	{
		[DataMember]
		public string JobGuid { get; set; }

		[DataMember]
		public string Station { get; set; }

		[DataMember]
		public string ProcessTime { get; set; }

		[DataMember]
		public string ArriveTime { get; set; }

		[DataMember]
		public string LeaveTime { get; set; }
	}
}
