using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistoryMovement
	{
		[DataMember]
		public string Source { get; set; }

		[DataMember]
		public string Destination { get; set; }

		[DataMember]
		public string InTime { get; set; }
	}
}
