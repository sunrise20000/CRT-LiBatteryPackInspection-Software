using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class SignalTowerDataItem
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

		[DataMember]
		public bool IsRedLightOn { get; set; }

		[DataMember]
		public bool IsYellowLightOn { get; set; }

		[DataMember]
		public bool IsBlueLightOn { get; set; }

		[DataMember]
		public bool IsGreenLightOn { get; set; }
	}
}
