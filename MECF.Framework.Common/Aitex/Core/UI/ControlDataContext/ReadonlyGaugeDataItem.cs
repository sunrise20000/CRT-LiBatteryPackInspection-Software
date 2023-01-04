using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class ReadonlyGaugeDataItem
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string Unit { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

		[DataMember]
		public double Value { get; set; }

		public ReadonlyGaugeDataItem()
		{
			DisplayName = "未定义";
		}
	}
}
