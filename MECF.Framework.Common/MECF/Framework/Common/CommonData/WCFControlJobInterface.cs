using System;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.CommonData
{
	[Serializable]
	[DataContract]
	public class WCFControlJobInterface
	{
		[DataMember]
		public string ObjtID { get; set; }

		[DataMember]
		public string ObjType { get; set; }

		[DataMember]
		public string ProcessingOrderMgmt { get; set; }

		[DataMember]
		public bool StartMethod { get; set; }

		[DataMember]
		public string state { get; set; }

		[DataMember]
		public DateTime CreateTime { get; set; }

		[DataMember]
		public DateTime CompleteTime { get; set; }
	}
}
