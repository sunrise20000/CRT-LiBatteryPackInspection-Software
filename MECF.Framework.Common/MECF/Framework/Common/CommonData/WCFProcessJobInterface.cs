using System;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.CommonData
{
	[Serializable]
	[DataContract]
	public class WCFProcessJobInterface
	{
		[DataMember]
		public string ObjtID { get; set; }

		[DataMember]
		public string ObjType { get; set; }

		[DataMember]
		public string PRMtlType { get; set; }

		[DataMember]
		public bool PRProcessStart { get; set; }

		[DataMember]
		public string PRRecipeMethod { get; set; }

		[DataMember]
		public string PJstate { get; set; }

		[DataMember]
		public DateTime CreateTime { get; set; }

		[DataMember]
		public DateTime CompleteTime { get; set; }

		[DataMember]
		public string RecID { get; set; }
	}
}
