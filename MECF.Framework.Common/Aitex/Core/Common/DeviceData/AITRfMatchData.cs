using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITRfMatchData : NotifiableItem, IDeviceData
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
		public float DCBias { get; set; }

		[DataMember]
		public float BiasPeak { get; set; }

		[DataMember]
		public float LoadPosition1 { get; set; }

		[DataMember]
		public float LoadPosition2 { get; set; }

		[DataMember]
		public float TunePosition1 { get; set; }

		[DataMember]
		public float TunePosition2 { get; set; }

		[DataMember]
		public float LoadPosition1SetPoint { get; set; }

		[DataMember]
		public float TunePosition1SetPoint { get; set; }

		[DataMember]
		public float TuneRange { get; set; }

		[DataMember]
		public float LoadRange { get; set; }

		[DataMember]
		public EnumRfMatchTuneMode TuneMode1 { get; set; }

		[DataMember]
		public EnumRfMatchTuneMode TuneMode2 { get; set; }

		[DataMember]
		public bool IsInterlockOk { get; set; }

		public AITRfMatchData()
		{
			DisplayName = "Undefined";
		}

		public void Update(IDeviceData data)
		{
		}
	}
}
