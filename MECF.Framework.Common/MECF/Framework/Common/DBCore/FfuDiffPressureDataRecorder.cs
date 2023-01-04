using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class FfuDiffPressureDataRecorder
	{
		public static List<HistoryFfuDiffPressureData> FfuDiffPressureHistory(string sql)
		{
			string[] array = sql.Split(';');
			string s = array[0];
			string s2 = array[1];
			DateTime dateTime = new DateTime(long.Parse(s));
			DateTime dateTime2 = new DateTime(long.Parse(s2));
			if (dateTime2 <= dateTime)
			{
				return null;
			}
			if (dateTime.Day != dateTime2.Day)
			{
				string cmdText = string.Format("SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{2}.Data\" where \"time\" >= '{0}' and \"time\" <= '{1}'  order by \"time\" ASC;", long.Parse(s), DateTime.Parse(dateTime.ToString("yyyy-MM-dd 23:59:59")).Ticks, dateTime.ToString("yyyyMMdd"));
				DataSet ds = DB.ExecuteDataset(cmdText);
				List<HistoryFfuDiffPressureData> result = new List<HistoryFfuDiffPressureData>();
				DSToList(ds, result);
				for (int i = 1; dateTime.AddDays(i).Day != dateTime2.Day; i++)
				{
					string cmdText2 = string.Format("SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{0}.Data\"  order by \"time\" ASC;", dateTime.AddDays(i).ToString("yyyyMMdd"));
					DataSet ds2 = DB.ExecuteDataset(cmdText2);
					DSToList(ds2, result);
				}
				string cmdText3 = string.Format("SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{2}.Data\" where \"time\" >= '{0}' and \"time\" <= '{1}'  order by \"time\" ASC;", DateTime.Parse(dateTime2.ToString("yyyy-MM-dd 0:0:0")).Ticks, long.Parse(s2), dateTime2.ToString("yyyyMMdd"));
				DataSet ds3 = DB.ExecuteDataset(cmdText3);
				DSToList(ds3, result);
				return result;
			}
			string cmdText4 = string.Format("SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{2}.Data\" where \"time\" >= '{0}' and \"time\" <= '{1}'  order by \"time\" ASC;", long.Parse(s), long.Parse(s2), dateTime.ToString("yyyyMMdd"));
			DataSet ds4 = DB.ExecuteDataset(cmdText4);
			List<HistoryFfuDiffPressureData> result2 = new List<HistoryFfuDiffPressureData>();
			DSToList(ds4, result2);
			return result2;
		}

		private static void DSToList(DataSet ds, List<HistoryFfuDiffPressureData> result)
		{
			if (ds != null && ds.Tables.Count != 0)
			{
				for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
				{
					result.Add(new HistoryFfuDiffPressureData
					{
						Time = ds.Tables[0].Rows[i]["time"].ToString(),
						DiffPressure1 = ds.Tables[0].Rows[i]["DiffPressure.DiffPressure1"].ToString(),
						DiffPressure2 = ds.Tables[0].Rows[i]["DiffPressure.DiffPressure2"].ToString(),
						FFU1Speed = ds.Tables[0].Rows[i]["FFU.FFU1Speed"].ToString(),
						FFU2Speed = ds.Tables[0].Rows[i]["FFU.FFU2Speed"].ToString()
					});
				}
			}
		}
	}
}
