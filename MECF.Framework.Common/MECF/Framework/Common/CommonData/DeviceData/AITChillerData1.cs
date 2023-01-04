using System;
using System.Runtime.Serialization;
using Aitex.Core.Common.DeviceData;

namespace MECF.Framework.Common.CommonData.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITChillerData1 : AITDeviceData
	{
		[DataMember]
		public string DeviceModule { get; set; }

		[DataMember]
		public float CH1Temperature { get; set; }

		[DataMember]
		public float CH2Temperature { get; set; }

		[DataMember]
		public float CH1TemperatureSetPoint { get; set; }

		[DataMember]
		public float CH2TemperatureSetPoint { get; set; }

		[DataMember]
		public bool IsCH1On { get; set; }

		[DataMember]
		public bool IsCH2On { get; set; }

		[DataMember]
		public bool IsCH1Warning { get; set; }

		[DataMember]
		public bool IsCH2Warning { get; set; }

		[DataMember]
		public bool IsCH1Alarm { get; set; }

		[DataMember]
		public bool IsCH2Alarm { get; set; }

		[DataMember]
		public double CH1WaterFlow { get; set; }

		[DataMember]
		public double CH2WaterFlow { get; set; }

		[DataMember]
		public float TemperatureHighLimit { get; set; }

		[DataMember]
		public float TemperatureLowLimit { get; set; }

		[DataMember]
		public string FormatString { get; set; }

		public AITChillerData1()
		{
			base.DisplayName = "未定义";
		}
	}
}
