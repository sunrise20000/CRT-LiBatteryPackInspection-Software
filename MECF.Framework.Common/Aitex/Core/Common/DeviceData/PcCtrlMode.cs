using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public enum PcCtrlMode
	{
		[EnumMember]
		Normal = 0,
		[EnumMember]
		Close = 1,
		[EnumMember]
		Open = 2,
		[EnumMember]
		Hold = 3
	}
}
