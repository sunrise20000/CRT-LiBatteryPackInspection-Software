using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistoryRecipe : WaferHistoryItem
	{
		[DataMember]
		public string Chamber { get; set; }

		[DataMember]
		public string Recipe { get; set; }

		[DataMember]
		public string SettingTime { get; set; }

		[DataMember]
		public string ActualTime { get; set; }

		[DataMember]
		public List<RecipeStep> Steps { get; set; }

		[DataMember]
		public List<RecipeStepFdcData> Fdcs { get; set; }
	}
}
