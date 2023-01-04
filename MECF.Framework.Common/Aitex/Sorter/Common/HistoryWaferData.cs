using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryWaferData
	{
		[DataMember]
		public string Guid { get; set; }

		[DataMember]
		public string CreateTime { get; set; }

		[DataMember]
		public string DeleteTime { get; set; }

		[DataMember]
		public string Station { get; set; }

		[DataMember]
		public string Slot { get; set; }

		[DataMember]
		public string LaserMarker { get; set; }

		[DataMember]
		public string LaserMarkerScore { get; set; }

		[DataMember]
		public string T7Code { get; set; }

		[DataMember]
		public string T7CodeScore { get; set; }

		[DataMember]
		public string CarrierGuid { get; set; }

		[DataMember]
		public string WaferId { get; set; }

		[DataMember]
		public string ImageFileName { get; set; }

		[DataMember]
		public string ImageFilePath { get; set; }
	}
}
