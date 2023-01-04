using System;
using System.Runtime.Serialization;

namespace Aitex.Core.RT.Event
{
	[Serializable]
	[DataContract]
	public enum EventLevel
	{
		[EnumMember]
		Information = 0,
		[EnumMember]
		Warning = 1,
		[EnumMember]
		Alarm = 2
	}
}
