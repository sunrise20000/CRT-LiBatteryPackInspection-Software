using Aitex.Core.UI.ControlDataContext;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SicPM.RecipeExecutions
{
    public class RecipeEndDataItem
    {
		public RecipeEndDataItem(string name, List<HistoryDataItem> historyDataItem)
		{
			Name = name;
			HistoryDataItem = historyDataItem;
		}

		public List<HistoryDataItem> HistoryDataItem { get; set; }

		public string Name { get; }

		public string Max
		{
			get { return HistoryDataItem.Count > 0 ? HistoryDataItem.Max(item => item.value).ToString("F2") : "0"; }
		}

		public string Min
		{
			get { return HistoryDataItem.Count > 0 ? HistoryDataItem.Min(item => item.value).ToString("F2") : "0"; }
		}

		public string Mean
		{
			get { return HistoryDataItem.Count > 0 ? HistoryDataItem.Average(item => item.value).ToString("F2") : "0"; }
		}

		/// <summary>
		/// 样本标准差
		/// </summary>
		public string StdDev
		{
			get
			{
				if (HistoryDataItem.Count == 0)
					return "0";

				double avg = HistoryDataItem.Average(item => item.value); // 计算平均数 
				double sum = HistoryDataItem.Sum(item => Math.Pow(item.value - avg, 2)); // 计算各数值与平均数的差值的平方，然后求和 
				double stdDev = Math.Sqrt(sum / (HistoryDataItem.Count - 1)); // 除以数量减1，然后开方

				return stdDev.ToString("F2");
			}
		}
	}
}