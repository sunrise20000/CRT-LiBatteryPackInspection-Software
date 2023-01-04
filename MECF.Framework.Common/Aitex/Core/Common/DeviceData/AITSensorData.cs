using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITSensorData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public bool Value { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		public AITSensorData()
		{
			DisplayName = "Undefined";
		}

		public void Update(IDeviceData data)
		{
			throw new NotImplementedException();
		}
	}
}
