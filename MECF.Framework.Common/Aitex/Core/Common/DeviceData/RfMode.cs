using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public enum RfMode
	{
		[EnumMember]
		ContinuousWaveMode = 1,
		[EnumMember]
		PulsingMode = 2
	}
}
