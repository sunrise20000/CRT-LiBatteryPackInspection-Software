using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryCarrierData
	{
		[DataMember]
		public string Guid { get; set; }

		[DataMember]
		public string Rfid { get; set; }

		[DataMember]
		public string Station { get; set; }

		[DataMember]
		public string LoadTime { get; set; }

		[DataMember]
		public string UnloadTime { get; set; }

		[DataMember]
		public string LotId { get; set; }

		[DataMember]
		public string ProductCategory { get; set; }
	}
}
