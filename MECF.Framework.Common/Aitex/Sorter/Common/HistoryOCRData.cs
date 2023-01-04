using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryOCRData
	{
		[DataMember]
		public string Guid { get; set; }

		[DataMember]
		public string wafer_id { get; set; }

		[DataMember]
		public string read_time { get; set; }

		[DataMember]
		public string source_lp { get; set; }

		[DataMember]
		public string source_carrier { get; set; }

		[DataMember]
		public string source_slot { get; set; }

		[DataMember]
		public string ocr_no { get; set; }

		[DataMember]
		public string ocr_job { get; set; }

		[DataMember]
		public string read_result { get; set; }

		[DataMember]
		public string lasermark { get; set; }

		[DataMember]
		public string ocr_score { get; set; }

		[DataMember]
		public string read_period { get; set; }
	}
}
