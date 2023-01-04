using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	[DataContract]
	public class AITOpticsViperData : NotifiableItem, IDeviceData
	{
		[DataMember]
		public int WaferNo { get; set; }

		[DataMember]
		public double DateTime { get; set; }

		[DataMember]
		public double Para1 { get; set; }

		[DataMember]
		public double Para2 { get; set; }

		[DataMember]
		public double Para3 { get; set; }

		[DataMember]
		public double Para4 { get; set; }

		[DataMember]
		public double Temperature { get; set; }

		public void Update(IDeviceData data)
		{
		}
	}
}
