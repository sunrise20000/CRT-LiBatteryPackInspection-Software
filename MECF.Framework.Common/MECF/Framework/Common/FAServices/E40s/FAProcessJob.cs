using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.FAServices.E40s
{
	[Serializable]
	[DataContract]
	public class FAProcessJob : NotifiableItem
	{
		[DataMember]
		public string ObjType { get; set; }

		[DataMember]
		public string ObjtID { get; set; }

		[DataMember]
		public List<int> PauseEvent { get; set; }

		[DataMember]
		public List<ProcessMaterialName> PRMtlNameList { get; set; }

		[DataMember]
		public MtlType PRMtlType { get; set; }

		[DataMember]
		public bool PRProcessStart { get; set; }

		[DataMember]
		public ProcessRecipeMethod PRRecipeMethod { get; set; }

		[DataMember]
		public string RecID { get; set; }

		[DataMember]
		public Dictionary<string, object> RecVariableList { get; set; }

		[DataMember]
		public ProcessJobState PJstate { get; set; }

		[DataMember]
		public DateTime CreateTime { get; set; }

		[DataMember]
		public DateTime CompleteTime { get; set; }
	}
}
