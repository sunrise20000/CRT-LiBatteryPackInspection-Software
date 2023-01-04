using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITValveData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public string UniqueName { get; set; }

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceSchematicId { get; set; }

		[DataMember]
		public bool SetPoint { get; set; }

		[DataMember]
		public bool DefaultValue { get; set; }

		[DataMember]
		public bool Feedback { get; set; }

		public bool IsOpen => Feedback;

		public AITValveData()
		{
			DisplayName = "未定义阀门";
		}

		public void Update(IDeviceData data)
		{
			if (data is AITValveData aITValveData)
			{
				DefaultValue = aITValveData.DefaultValue;
				DeviceSchematicId = aITValveData.DeviceSchematicId;
				DeviceName = aITValveData.DeviceName;
				DisplayName = aITValveData.DisplayName;
				Feedback = aITValveData.Feedback;
				SetPoint = aITValveData.SetPoint;
				InvokePropertyChanged();
			}
		}
	}
}
