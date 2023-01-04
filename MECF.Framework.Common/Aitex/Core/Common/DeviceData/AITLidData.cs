using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITLidData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public int SetPoint { get; set; }

		[DataMember]
		public int Status { get; set; }

		public bool IsOpen => Status == 1;

		public bool IsClose => Status == 0;

		public AITLidData()
		{
			DisplayName = "Undefined Lid";
		}

		public void Update(IDeviceData data)
		{
		}
	}
}
