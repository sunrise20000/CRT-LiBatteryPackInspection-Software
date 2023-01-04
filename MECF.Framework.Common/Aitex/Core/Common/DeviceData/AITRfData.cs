using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITRfData : AITDeviceData
	{
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
		public int WorkMode { get; set; }

		[DataMember]
		public string PowerOnElapsedTime { get; set; }

		[DataMember]
		public bool EnablePulsing { get; set; }

		[DataMember]
		public bool EnableC1C2Position { get; set; }

		[DataMember]
		public bool EnableReflectPower { get; set; }

		[DataMember]
		public bool EnableVoltageCurrent { get; set; }

		[DataMember]
		public int MatchMode { get; set; }

		[DataMember]
		public int MatchPresetMode { get; set; }

		[DataMember]
		public float MatchPositionC1 { get; set; }

		[DataMember]
		public float MatchPositionC2 { get; set; }

		[DataMember]
		public float MatchPositionC1SetPoint { get; set; }

		[DataMember]
		public float MatchPositionC2SetPoint { get; set; }

		[DataMember]
		public float Voltage { get; set; }

		[DataMember]
		public float Current { get; set; }

		public string TextWorkMode => (WorkMode == 1) ? "Continuous" : "Pulsing";

		public string TextOnOff => IsRfOn ? "On" : "Off";

		public bool HasError => IsRfAlarm || !IsInterlockOk;

		[DataMember]
		public bool IsToleranceError { get; set; }

		[DataMember]
		public bool IsToleranceWarning { get; set; }

		public AITRfData()
		{
			base.DisplayName = "Undefined";
		}
	}
}
