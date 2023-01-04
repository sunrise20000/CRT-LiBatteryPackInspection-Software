using System;
using System.Collections.Generic;

namespace Aitex.Core.Util
{
	[Serializable]
	public class DataItem
	{
		public string DataName { get; set; }

		public List<DateTime> TimeStamp { get; set; }

		public List<float> RawData { get; set; }
	}
}
