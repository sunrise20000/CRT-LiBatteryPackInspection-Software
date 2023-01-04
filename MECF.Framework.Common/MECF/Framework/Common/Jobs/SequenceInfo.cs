using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.Jobs
{
	[Serializable]
	[DataContract]
	public class SequenceInfo
	{
		[DataMember]
		public List<SequenceStepInfo> Steps { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public Guid InnerId { get; set; }

		public SequenceInfo(string name)
		{
			Name = name;
			InnerId = Guid.NewGuid();
			Steps = new List<SequenceStepInfo>();
		}
	}
}
