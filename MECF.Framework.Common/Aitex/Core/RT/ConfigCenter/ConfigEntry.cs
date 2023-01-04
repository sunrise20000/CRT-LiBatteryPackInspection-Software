using System;
using System.Runtime.Serialization;

namespace Aitex.Core.RT.ConfigCenter
{
	[Serializable]
	[DataContract]
	public class ConfigEntry
	{
		[DataMember]
		public string SystemType { get; set; }

		[DataMember]
		public string SectionName { get; set; }

		[DataMember]
		public string EntryName { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public string Unit { get; set; }

		[DataMember]
		public string Value { get; set; }

		[DataMember]
		public string Default { get; set; }

		[DataMember]
		public string RangeLowLimit { get; set; }

		[DataMember]
		public string RangeUpLimit { get; set; }

		[DataMember]
		public string XPath { get; set; }
	}
}
