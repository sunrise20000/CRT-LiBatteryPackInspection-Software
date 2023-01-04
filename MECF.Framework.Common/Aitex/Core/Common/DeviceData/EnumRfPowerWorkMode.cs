using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	public enum EnumRfPowerWorkMode
	{
		[EnumMember]
		Undefined = 0,
		[EnumMember]
		ContinuousWaveMode = 1,
		[EnumMember]
		PulsingMode = 2
	}
}
