using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITDeviceData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string UniqueName { get; set; }

		[DataMember]
		public string Module { get; set; }

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
		public Dictionary<string, object> AttrValue { get; set; }

		public AITDeviceData()
		{
			DisplayName = "Undefined";
			Unit = "";
			AttrValue = new Dictionary<string, object>();
		}

		public void Update(IDeviceData data)
		{
		}
	}
}
