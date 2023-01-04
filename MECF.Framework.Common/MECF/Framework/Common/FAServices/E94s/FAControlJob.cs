using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.FAServices.E40s;

namespace MECF.Framework.Common.FAServices.E94s
{
	[Serializable]
	[DataContract]
	public class FAControlJob : NotifiableItem
	{
		[DataMember]
		public string ObjType { get; set; }

		[DataMember]
		public string ObjtID { get; set; }

		[DataMember]
		public List<string> CurrentPRJob { get; set; }

		[DataMember]
		public string DataCollectionPlan { get; set; }

		[DataMember]
		public List<string> CarrierIputSpec { get; set; }

		[DataMember]
		public List<MtrlOutSpecPair> MtrlOutSpec { get; set; }

		[DataMember]
		public List<int> PauseEvent { get; set; }

		[DataMember]
		public List<string> ProcessingCtrlSpec { get; set; }

		[DataMember]
		public ProcessOrderManagement ProcessingOrderMgmt { get; set; }

		[DataMember]
		public bool StartMethod { get; set; }

		[DataMember]
		public CJState state { get; set; }

		[DataMember]
		public DateTime CreateTime { get; set; }

		[DataMember]
		public DateTime CompleteTime { get; set; }
	}
}
