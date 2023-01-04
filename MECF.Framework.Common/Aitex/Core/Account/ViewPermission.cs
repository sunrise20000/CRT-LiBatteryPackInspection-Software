using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Account
{
	[Serializable]
	[DataContract]
	public enum ViewPermission
	{
		[EnumMember]
		Invisiable = 1,
		[EnumMember]
		Readonly = 2,
		[EnumMember]
		PartlyControl = 3,
		[EnumMember]
		FullyControl = 4,
		[EnumMember]
		ProcessOPControl = 5
	}
}
