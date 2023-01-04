using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class HistoryFfuDiffPressureData
	{
		[DataMember]
		public string Time { get; set; }

		[DataMember]
		public string FFU1Speed { get; set; }

		[DataMember]
		public string FFU2Speed { get; set; }

		[DataMember]
		public string DiffPressure1 { get; set; }

		[DataMember]
		public string DiffPressure2 { get; set; }
	}
}
