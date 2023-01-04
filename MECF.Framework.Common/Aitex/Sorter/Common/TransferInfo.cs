using System.Collections.Generic;
using System.Runtime.Serialization;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
	[DataContract]
	public class TransferInfo
	{
		[DataMember]
		public string WaferID { get; set; }

		[DataMember]
		public ModuleName Source { get; set; }

		[DataMember]
		public int SourceSlot { get; set; }

		[DataMember]
		public ModuleName Station { get; set; }

		[DataMember]
		public int Slot { get; set; }

		[DataMember]
		public MoveOption Option { get; set; }

		[DataMember]
		public bool PreAlign { get; set; }

		[DataMember]
		public double Angle { get; set; }

		[DataMember]
		public bool VerifyAny { get; set; }

		[DataMember]
		public bool VerifyLaserMaker { get; set; }

		[DataMember]
		public bool VerifyLM1Checksum { get; set; }

		[DataMember]
		public string LaserMaker { get; set; }

		[DataMember]
		public bool VerifyT7Code { get; set; }

		[DataMember]
		public bool VerifyLM2Checksum { get; set; }

		[DataMember]
		public string T7Code { get; set; }

		[DataMember]
		public List<string> LM1JobFile { get; set; }

		[DataMember]
		public List<string> LM2JobFile { get; set; }

		[DataMember]
		public LMReadOption LM1Reader { get; set; }

		[DataMember]
		public LMReadOption LM2Reader { get; set; }

		[DataMember]
		public bool PostAlign { get; set; }

		[DataMember]
		public double PostAlignAngle { get; set; }
	}
}
