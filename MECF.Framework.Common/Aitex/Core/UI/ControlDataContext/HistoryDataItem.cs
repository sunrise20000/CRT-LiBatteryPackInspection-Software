using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class HistoryDataItem
	{
		[DataMember]
		public DateTime dateTime { get; set; }

		[DataMember]
		public string dbName { get; set; }

		[DataMember]
		public double value { get; set; }
	}
}
