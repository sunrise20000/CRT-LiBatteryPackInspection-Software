using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryStatisticsOCRData
	{
		[DataMember]
		public string Date { get; set; }

		[DataMember]
		public string Totaltimes { get; set; }

		[DataMember]
		public string Successfueltimes { get; set; }

		[DataMember]
		public string Failuretimes { get; set; }

		[DataMember]
		public string Result { get; set; }
	}
}
