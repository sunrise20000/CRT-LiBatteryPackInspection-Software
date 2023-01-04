using System.Collections.Generic;

namespace MECF.Framework.Common.FAServices.E40s
{
	public struct ProcessMaterialName
	{
		public string CarrierID { get; set; }

		public List<int> SlotID { get; set; }
	}
}
