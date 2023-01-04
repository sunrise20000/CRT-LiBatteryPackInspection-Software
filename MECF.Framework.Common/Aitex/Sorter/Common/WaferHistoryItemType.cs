using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public enum WaferHistoryItemType
	{
		[EnumMember]
		None = 0,
		[EnumMember]
		Lot = 1,
		[EnumMember]
		Wafer = 2,
		[EnumMember]
		Recipe = 3
	}
}
