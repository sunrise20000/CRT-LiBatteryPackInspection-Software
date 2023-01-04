using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITWaterFlowSensorData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public string Unit { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public double Scale { get; set; }

		[DataMember]
		public double FeedBack { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		[DataMember]
		public bool IsOutOfTolerance { get; set; }

		public AITWaterFlowSensorData()
		{
			DisplayName = "Undefined";
			Unit = "slm";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
