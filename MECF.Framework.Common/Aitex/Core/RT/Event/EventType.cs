using System;
using System.Runtime.Serialization;

namespace Aitex.Core.RT.Event
{
	[Serializable]
	[DataContract]
	public enum EventType
	{
		[EnumMember]
		EventUI_Notify = 0,
		[EnumMember]
		Dialog_Nofity = 1,
		[EnumMember]
		KickOut_Notify = 2,
		[EnumMember]
		Sound_Notify = 3,
		[EnumMember]
		UIMessage_Notify = 4,
		[EnumMember]
		OperationLog = 5,
		[EnumMember]
		HostNotification = 6
	}
}
