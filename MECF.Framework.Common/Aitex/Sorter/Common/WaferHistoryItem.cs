using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SciChart.Core.Extensions;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistoryItem : ITreeItem<WaferHistoryItem>
	{
		[DataMember]
		public WaferHistoryItemType Type { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string ID { get; set; }

		[DataMember]
		public DateTime StartTime { get; set; }

		[DataMember]
		public DateTime EndTime { get; set; }

		public string Duration => (EndTime.CompareTo(StartTime) < 0) ? "" : EndTime.Subtract(StartTime).ToString("hh\\:mm\\:ss");

		public string ItemInfo => EnumerableExtensions.IsNullOrEmpty<char>((IEnumerable<char>)Name) ? "" : Name;

		[DataMember]
		public ITreeItem<WaferHistoryItem> SubItems { get; set; }

		[DataMember]
		public string RfID { get; set; }
	}
}
