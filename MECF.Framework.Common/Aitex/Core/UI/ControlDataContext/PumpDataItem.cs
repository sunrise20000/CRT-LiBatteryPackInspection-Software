using System;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class PumpDataItem
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

		[DataMember]
		public bool MainPumpEnable { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }
	}
}
