using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class GasValveDataItem
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

		[DataMember]
		public bool SetValue { get; set; }

		[DataMember]
		public bool DefaultValue { get; set; }

		[DataMember]
		public bool Feedback { get; set; }

		public GasValveDataItem()
		{
			DisplayName = "未定义阀门";
		}
	}
}
