using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryBatData
	{
		[DataMember]
		public string Guid { get; set; }

		[DataMember]
		public string StartTime { get; set; }

		[DataMember]
		public string EndTime { get; set; }

		[DataMember]
		public string RecipeName { get; set; }

		[DataMember]
		public string Result { get; set; }
	}
}
