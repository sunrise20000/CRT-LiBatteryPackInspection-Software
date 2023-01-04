using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[DataContract]
	public enum ServoState
	{
		[EnumMember]
		Unknown = 0,
		[EnumMember]
		NotInitial = 1,
		[EnumMember]
		Idle = 2,
		[EnumMember]
		Moving = 3,
		[EnumMember]
		Error = 4
	}
}
