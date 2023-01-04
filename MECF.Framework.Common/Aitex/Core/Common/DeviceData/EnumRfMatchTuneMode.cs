using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	public enum EnumRfMatchTuneMode
	{
		[EnumMember]
		Undefined = 0,
		[EnumMember]
		Auto = 1,
		[EnumMember]
		Manual = 2
	}
}
