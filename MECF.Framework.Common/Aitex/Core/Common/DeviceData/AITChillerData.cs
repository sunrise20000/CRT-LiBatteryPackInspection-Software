using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITChillerData : AITDeviceData
	{
		[DataMember]
		public bool IsRunning { get; set; }

		[DataMember]
		public bool IsError { get; set; }

		public AITChillerData()
		{
			base.DisplayName = "Undefined";
		}
	}
}
