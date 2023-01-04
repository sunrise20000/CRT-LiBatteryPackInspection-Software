using System;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.Device.Bases
{
	[Serializable]
	[DataContract]
	public enum LightState
	{
		[EnumMember]
		Off = 0,
		[EnumMember]
		On = 1,
		[EnumMember]
		Blink = 2,
		[EnumMember]
		Unknown = 3
	}
}
