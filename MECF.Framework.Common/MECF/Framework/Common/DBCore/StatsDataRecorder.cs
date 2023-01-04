using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class StatsDataRecorder
	{
		public static List<StatsStatisticsData> QueryStatsStatistics(string sql)
		{
			List<StatsStatisticsData> list = new List<StatsStatisticsData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					string text = dataSet.Tables[0].Rows[i]["name"].ToString();
					string text2 = dataSet.Tables[0].Rows[i]["description"].ToString();
					string text3 = dataSet.Tables[0].Rows[i]["value"].ToString();
					string[] nameSplit = text.Split('.');
					if (nameSplit == null || nameSplit.Length != 3)
					{
						continue;
					}
					StatsStatisticsData statsStatisticsData = list.Where((StatsStatisticsData x) => x.Date == nameSplit[2]).FirstOrDefault();
					if (statsStatisticsData != null)
					{
						switch (text2)
						{
						case "Unknown":
							statsStatisticsData.Unknown = text3;
							break;
						case "Setup":
							statsStatisticsData.Setup = text3;
							break;
						case "Idle":
							statsStatisticsData.Idle = text3;
							break;
						case "Ready":
							statsStatisticsData.Ready = text3;
							break;
						case "Executing":
							statsStatisticsData.Executing = text3;
							break;
						case "Pause":
							statsStatisticsData.Pause = text3;
							break;
						}
						continue;
					}
					StatsStatisticsData statsStatisticsData2 = new StatsStatisticsData();
					statsStatisticsData2.Date = nameSplit[2];
					switch (text2)
					{
					case "Unknown":
						statsStatisticsData2.Unknown = text3;
						break;
					case "Setup":
						statsStatisticsData2.Setup = text3;
						break;
					case "Idle":
						statsStatisticsData2.Idle = text3;
						break;
					case "Ready":
						statsStatisticsData2.Ready = text3;
						break;
					case "Executing":
						statsStatisticsData2.Executing = text3;
						break;
					case "Pause":
						statsStatisticsData2.Pause = text3;
						break;
					}
					list.Add(statsStatisticsData2);
				}
				return list;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}
	}
}
