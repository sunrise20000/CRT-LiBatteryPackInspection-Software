using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistoryLot : WaferHistoryItem
	{
		[DataMember]
		public string CarrierID { get; set; }

		[DataMember]
		public string Rfid { get; set; }

		[DataMember]
		public int WaferCount { get; set; }

		[DataMember]
		public int FaultWaferCount { get; set; }
	}
}
