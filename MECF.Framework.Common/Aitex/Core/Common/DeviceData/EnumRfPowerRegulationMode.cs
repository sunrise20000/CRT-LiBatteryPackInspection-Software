using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	public enum EnumRfPowerRegulationMode
	{
		[EnumMember]
		Undefined = 0,
		[EnumMember]
		Forward = 1,
		[EnumMember]
		Load = 2,
		[EnumMember]
		DcBias = 3,
		[EnumMember]
		VALimit = 4
	}
}
