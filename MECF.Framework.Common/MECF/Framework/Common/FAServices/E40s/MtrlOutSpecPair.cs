using System.Collections.Generic;

namespace MECF.Framework.Common.FAServices.E40s
{
	public struct MtrlOutSpecPair
	{
		public string sourceCarID { get; set; }

		public string DestCarID { get; set; }

		public List<int> sourceSlots { get; set; }

		public List<int> destSlots { get; set; }
	}
}
