using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class ManualTransferTask
	{
		[DataMember]
		public ModuleName SourceModule { get; set; }

		[DataMember]
		public int SourceSlotIndex { get; set; }

		[DataMember]
		public ModuleName DestModule { get; set; }

		[DataMember]
		public int DestSlotIndex { get; set; }
	}
}
