using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common
{
	[Serializable]
	[DataContract]
	public class SubstHistory
	{
		[DataMember]
		public string locationID { get; set; }

		[DataMember]
		public int SlotNO { get; set; }

		[DataMember]
		public DateTime AccessTime { get; set; }

		[DataMember]
		public SubstAccessType AccessType { get; set; }

		public SubstHistory(string loc, int slot, DateTime dt, SubstAccessType type)
		{
			locationID = loc;
			SlotNO = slot;
			AccessTime = dt;
			AccessType = type;
		}
	}
}
