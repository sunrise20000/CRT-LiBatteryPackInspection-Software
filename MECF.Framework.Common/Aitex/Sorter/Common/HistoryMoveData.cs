using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryMoveData
	{
		[DataMember]
		public string WaferGuid { get; set; }

		[DataMember]
		public string ArriveTime { get; set; }

		[DataMember]
		public string Station { get; set; }

		[DataMember]
		public string Slot { get; set; }

		[DataMember]
		public string Result { get; set; }
	}
}
