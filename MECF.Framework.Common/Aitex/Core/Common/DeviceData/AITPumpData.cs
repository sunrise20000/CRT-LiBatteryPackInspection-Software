using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITPumpData : AITDeviceData
	{
		[DataMember]
		public string DeviceModule { get; set; }

		[DataMember]
		public bool IsOn { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		[DataMember]
		public int Speed { get; set; }

		[DataMember]
		public bool OverTemp { get; set; }

		[DataMember]
		public bool AtSpeed { get; set; }

		[DataMember]
		public int Temperature { get; set; }

		[DataMember]
		public int LocalRemoteMode { get; set; }

		[DataMember]
		public double WaterFlow { get; set; }

		[DataMember]
		public bool IsDryPumpEnable { get; set; }

		[DataMember]
		public bool IsN2PressureEnable { get; set; }

		[DataMember]
		public bool N2PressureWarning { get; set; }

		[DataMember]
		public bool N2PressureAlarm { get; set; }

		[DataMember]
		public bool IsWaterFlowEnable { get; set; }

		[DataMember]
		public bool WaterFlowWarning { get; set; }

		[DataMember]
		public bool WaterFlowAlarm { get; set; }

		[DataMember]
		public bool IsOverLoad { get; set; }

		public AITPumpData()
		{
			base.DisplayName = "未定义";
		}
	}
}
