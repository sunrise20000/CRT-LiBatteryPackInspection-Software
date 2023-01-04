using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class PressureTransducerDataItem
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

		[DataMember]
		public double Value { get; set; }

		[DataMember]
		public bool IsEnable { get; set; }

		public PressureTransducerDataItem()
		{
			DisplayName = "未定义设备";
		}
	}
}
