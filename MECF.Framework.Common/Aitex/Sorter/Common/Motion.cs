using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	public enum Motion
	{
		[EnumMember]
		Pick = 0,
		[EnumMember]
		Place = 1,
		[EnumMember]
		Exchange = 2,
		[EnumMember]
		Alignment = 3
	}
}
