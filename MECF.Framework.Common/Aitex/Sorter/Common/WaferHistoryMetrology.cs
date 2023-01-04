using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistoryMetrology
	{
		[DataMember]
		public string dataname { get; set; }

		[DataMember]
		public string datavalue { get; set; }

		[DataMember]
		public string processtime { get; set; }

		[DataMember]
		public string stationname { get; set; }
	}
}
