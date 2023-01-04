using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITThrottleValveData : NotifiableItem, IDeviceData
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
		public string UnitPosition { get; set; }

		[DataMember]
		public string UnitPressure { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public bool TVEnable { get; set; }

		[DataMember]
		public double MaxValuePosition { get; set; }

		[DataMember]
		public double MaxValuePressure { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public double Factor { get; set; }

		[DataMember]
		public int Mode { get; set; }

		[DataMember]
		public float PositionFeedback { get; set; }

		[DataMember]
		public float PressureFeedback { get; set; }

		[DataMember]
		public float PressureSetPoint { get; set; }

		[DataMember]
		public float PositionSetPoint { get; set; }

		[DataMember]
		public float PositionSetPointCurrent { get; set; }

		[DataMember]
		public float PressureSetPointCurrent { get; set; }

		[DataMember]
		public int State { get; set; }

		public string TextMode => Mode switch
		{
			2 => "Position", 
			5 => "Pressure", 
			4 => "Open", 
			3 => "Close", 
			_ => "Undefined", 
		};

		public AITThrottleValveData()
		{
			DisplayName = "Undefined";
			Factor = 1.0;
			UnitPosition = "%";
			UnitPressure = "mbar";
			Type = "TV";
			MaxValuePosition = 100.0;
			MaxValuePressure = 1010.0;
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
