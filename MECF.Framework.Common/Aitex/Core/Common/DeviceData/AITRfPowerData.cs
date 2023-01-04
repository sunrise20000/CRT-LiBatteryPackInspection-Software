using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITRfPowerData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string Module { get; set; }

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public string UnitPower { get; set; }

		[DataMember]
		public string UnitFrequency { get; set; }

		[DataMember]
		public string UnitDuty { get; set; }

		[DataMember]
		public double ScalePower { get; set; }

		[DataMember]
		public double ScaleFrequency { get; set; }

		[DataMember]
		public double ScaleDuty { get; set; }

		[DataMember]
		public float PowerSetPoint { get; set; }

		[DataMember]
		public float FrequencySetPoint { get; set; }

		[DataMember]
		public float DutySetPoint { get; set; }

		[DataMember]
		public bool IsInterlockOk { get; set; }

		[DataMember]
		public bool IsRfOn { get; set; }

		[DataMember]
		public bool IsRfAlarm { get; set; }

		[DataMember]
		public float ForwardPower { get; set; }

		[DataMember]
		public float ReflectPower { get; set; }

		[DataMember]
		public float Frequency { get; set; }

		[DataMember]
		public float PulsingFrequency { get; set; }

		[DataMember]
		public float PulsingDutyCycle { get; set; }

		public string TextOnOff => IsRfOn ? "On" : "Off";

		[DataMember]
		public EnumRfPowerRegulationMode RegulationMode { get; set; }

		public AITRfPowerData()
		{
			DisplayName = "Undefined";
		}

		public void Update(IDeviceData data)
		{
		}
	}
}
