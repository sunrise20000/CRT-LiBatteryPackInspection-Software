using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public enum PressureCtrlMode
	{
		[EnumMember]
		Undefined = 0,
		[EnumMember]
		TVCalib = 1,
		[EnumMember]
		TVPositionCtrl = 2,
		[EnumMember]
		TVClose = 3,
		[EnumMember]
		TVOpen = 4,
		[EnumMember]
		TVPressureCtrl = 5
	}
}
