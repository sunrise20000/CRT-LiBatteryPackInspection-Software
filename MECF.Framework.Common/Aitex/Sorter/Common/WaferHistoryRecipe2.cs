using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	public class WaferHistoryRecipe2 : WaferHistoryItem
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

		public object SelectedLot { get; set; }

		public object SelectedWafer { get; set; }

		public object SelectedProcess { get; set; }

		public bool IsToCompare { get; set; } = false;


		public object Cache { get; set; }
	}
}
