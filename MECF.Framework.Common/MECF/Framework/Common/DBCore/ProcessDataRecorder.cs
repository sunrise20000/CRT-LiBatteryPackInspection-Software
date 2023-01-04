using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.DBCore
{
	public class ProcessDataRecorder
	{
		public static void StartFlowCount(string processGuid, string waferGuid)
		{
			string sql = string.Format("INSERT INTO \"process_flow_data\"(\"process_guid\", \"wafer_guid\",\"process_begin_time\" )VALUES ('{0}', '{1}','{2}');", processGuid, waferGuid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
			DB.Insert(sql);
		}

		public static void EndFlowCount(string guid)
		{
			string sql = string.Format("UPDATE \"process_flow_data\" SET \"process_end_time\"='{0}' WHERE \"process_guid\"='{1}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), guid);
			DB.Insert(sql);
		}

		public static void Start(string guid, string recipeName)
		{
			string sql = string.Format("INSERT INTO \"process_data\"(\"guid\", \"process_begin_time\", \"recipe_name\"  )VALUES ('{0}', '{1}', '{2}' );", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), recipeName);
			DB.Insert(sql);
		}

		public static void Start(string guid, string recipeName, string waferDataGuid, string processIn, float recipeSettingTime = 0f)
		{
			string sql = string.Format("INSERT INTO \"process_data\"(\"guid\", \"process_begin_time\", \"recipe_name\" , \"wafer_data_guid\", \"process_in\", \"recipe_setting_time\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}' );", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), recipeName, waferDataGuid, processIn, recipeSettingTime);
			DB.Insert(sql);
		}

		public static void UpdateStatus(string guid, string status)
		{
			string sql = $"UPDATE \"process_data\" SET \"process_status\"='{status}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void End(string guid, string status)
		{
			string sql = string.Format("UPDATE \"process_data\" SET \"process_end_time\"='{0}',\"process_status\"='{1}' WHERE \"guid\"='{2}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), status, guid);
			DB.Insert(sql);
		}

		public static void End(string guid)
		{
			string sql = string.Format("UPDATE \"process_data\" SET \"process_end_time\"='{0}' WHERE \"guid\"='{1}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), guid);
			DB.Insert(sql);
		}

		public static void StepStart(string recipeGuid, int stepNumber, string stepName, float stepTime)
		{
			string text = Guid.NewGuid().ToString();
			string sql = $"INSERT INTO \"recipe_step_data\"(\"guid\", \"step_begin_time\", \"step_name\" , \"step_time\", \"process_data_guid\", \"step_number\")VALUES ('{text}', '{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', '{stepName}', '{stepTime}', '{recipeGuid}', '{stepNumber}' );";
			DB.Insert(sql);
		}

		public static void StepEnd(string recipeGuid, int stepNumber, List<FdcDataItem> stepData = null)
		{
			string sql = $"UPDATE \"recipe_step_data\" SET \"step_end_time\"='{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}' WHERE \"process_data_guid\"='{recipeGuid}' and \"step_number\"='{stepNumber}';";
			DB.Insert(sql);
			if (stepData == null || stepData.Count <= 0)
			{
				return;
			}
			foreach (FdcDataItem stepDatum in stepData)
			{
				sql = $"INSERT INTO \"step_fdc_data\"(\"process_data_guid\", \"create_time\", \"step_number\" , \"parameter_name\", \"sample_count\", \"min_value\", \"max_value\", \"setpoint\", \"std_value\", \"mean_value\")VALUES ('{recipeGuid}', '{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', '{stepNumber}', '{stepDatum.Name}', '{stepDatum.SampleCount}', '{stepDatum.MinValue}', '{stepDatum.MaxValue}', '{stepDatum.SetPoint}', '{stepDatum.StdValue}', '{stepDatum.MeanValue}' );";
				DB.Insert(sql);
			}
		}

		public List<string> GetHistoryRecipeList(DateTime begin, DateTime end)
		{
			List<string> list = new List<string>();
			string cmdText = string.Format("SELECT * FROM \"RecipeRunHistory\" where \"ProcessBeginTime\" >= '{0}' and \"ProcessBeginTime\" <= '{1}' order by \"ProcessBeginTime\" ASC;", begin.ToString("yyyy/MM/dd HH:mm:ss.fff"), end.ToString("yyyy/MM/dd HH:mm:ss.fff"));
			DataSet dataSet = DB.ExecuteDataset(cmdText);
			if (dataSet == null)
			{
				return list;
			}
			for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
			{
				string item = dataSet.Tables[0].Rows[i]["RecipeName"].ToString();
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
			dataSet.Clear();
			return list;
		}

		public static List<HistoryProcessData> QueryDBProcess(string sql)
		{
			List<HistoryProcessData> list = new List<HistoryProcessData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryProcessData historyProcessData = new HistoryProcessData();
					historyProcessData.RecipeName = dataSet.Tables[0].Rows[i]["recipe_name"].ToString();
					historyProcessData.Result = dataSet.Tables[0].Rows[i]["process_status"].ToString();
					historyProcessData.Guid = dataSet.Tables[0].Rows[i]["guid"].ToString();
					if (!dataSet.Tables[0].Rows[i]["process_begin_time"].Equals(DBNull.Value))
					{
						historyProcessData.StartTime = ((DateTime)dataSet.Tables[0].Rows[i]["process_begin_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					if (!dataSet.Tables[0].Rows[i]["process_end_time"].Equals(DBNull.Value))
					{
						historyProcessData.EndTime = ((DateTime)dataSet.Tables[0].Rows[i]["process_end_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					list.Add(historyProcessData);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<HistoryDataItem> GetHistoryDataFromStartToEnd(IEnumerable<string> keys, DateTime begin, DateTime end, string module)
		{
			List<HistoryDataItem> list = new List<HistoryDataItem>();
			try
			{
				DateTime dateTime = new DateTime(begin.Year, begin.Month, begin.Day, begin.Hour, begin.Minute, begin.Second, begin.Millisecond);
				DateTime dateTime2 = new DateTime(begin.Year, begin.Month, end.Day, end.Hour, end.Minute, end.Second, end.Millisecond);
				string text = "select time AS InternalTimeStamp";
				foreach (string key in keys)
				{
					text = text + "," + $"\"{key}\"";
				}
				text += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc LIMIT 86400;", begin.ToString("yyyyMMdd") + "." + module, dateTime.Ticks, dateTime2.Ticks);
				DataSet dataSet = DB.ExecuteDataset(text);
				if (dataSet == null)
				{
					return list;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				DateTime dateTime3 = default(DateTime);
				Dictionary<int, string> dictionary = new Dictionary<int, string>();
				for (int i = 0; i < dataSet.Tables[0].Columns.Count; i++)
				{
					dictionary.Add(i, dataSet.Tables[0].Columns[i].ColumnName);
				}
				for (int j = 0; j < dataSet.Tables[0].Rows.Count; j++)
				{
					DataRow dataRow = dataSet.Tables[0].Rows[j];
					for (int k = 0; k < dataSet.Tables[0].Columns.Count; k++)
					{
						HistoryDataItem historyDataItem = new HistoryDataItem();
						if (k == 0)
						{
							long ticks = (long)dataRow[k];
							dateTime3 = new DateTime(ticks);
							continue;
						}
						string text2 = dictionary[k];
						if (dataRow[k] is DBNull || dataRow[k] == null)
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = 0.0;
						}
						else if (dataRow[k] is bool)
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = (((bool)dataRow[k]) ? 1 : 0);
						}
						else
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = float.Parse(dataRow[k].ToString());
						}
						list.Add(historyDataItem);
					}
				}
				dataSet.Clear();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<HistoryDataItem> GetOneDayHistoryData(IEnumerable<string> keys, DateTime begin, string module)
		{
			List<HistoryDataItem> list = new List<HistoryDataItem>();
			try
			{
				DateTime dateTime = new DateTime(begin.Year, begin.Month, begin.Day, 0, 0, 0, 0);
				DateTime dateTime2 = new DateTime(begin.Year, begin.Month, begin.Day, 23, 59, 59, 999);
				string text = "select time AS InternalTimeStamp";
				foreach (string key in keys)
				{
					text = text + "," + $"\"{key}\"";
				}
				text += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc LIMIT 86400;", begin.ToString("yyyyMMdd") + "." + module, dateTime.Ticks, dateTime2.Ticks);
				DataSet dataSet = DB.ExecuteDataset(text);
				if (dataSet == null)
				{
					return list;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				DateTime dateTime3 = default(DateTime);
				Dictionary<int, string> dictionary = new Dictionary<int, string>();
				for (int i = 0; i < dataSet.Tables[0].Columns.Count; i++)
				{
					dictionary.Add(i, dataSet.Tables[0].Columns[i].ColumnName);
				}
				for (int j = 0; j < dataSet.Tables[0].Rows.Count; j++)
				{
					DataRow dataRow = dataSet.Tables[0].Rows[j];
					for (int k = 0; k < dataSet.Tables[0].Columns.Count; k++)
					{
						HistoryDataItem historyDataItem = new HistoryDataItem();
						if (k == 0)
						{
							long ticks = (long)dataRow[k];
							dateTime3 = new DateTime(ticks);
							continue;
						}
						string text2 = dictionary[k];
						if (dataRow[k] is DBNull || dataRow[k] == null)
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = 0.0;
						}
						else if (dataRow[k] is bool)
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = (((bool)dataRow[k]) ? 1 : 0);
						}
						else
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = float.Parse(dataRow[k].ToString());
						}
						list.Add(historyDataItem);
					}
				}
				dataSet.Clear();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<HistoryDataItem> GetHistoryData(IEnumerable<string> keys, string recipeRunGuid, string module)
		{
			List<HistoryDataItem> list = new List<HistoryDataItem>();
			try
			{
				string cmdText = $"SELECT * FROM \"process_data\" where \"guid\" = '{recipeRunGuid}'";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return list;
				}
				if (dataSet.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				object obj = dataSet.Tables[0].Rows[0]["process_begin_time"];
				if (obj is DBNull)
				{
					LOG.Write($"{recipeRunGuid} not set start time");
					return list;
				}
				DateTime dateTime = (DateTime)obj;
				object obj2 = dataSet.Tables[0].Rows[0]["process_end_time"];
				if (obj2 is DBNull)
				{
					obj2 = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, 999);
				}
				DateTime dateTime2 = (DateTime)obj2;
				cmdText = "select time AS InternalTimeStamp";
				foreach (string key in keys)
				{
					cmdText = cmdText + "," + $"\"{key}\"";
				}
				cmdText += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc LIMIT 2000;", dateTime.ToString("yyyyMMdd") + "." + module, dateTime.Ticks, dateTime2.Ticks);
				DataSet dataSet2 = DB.ExecuteDataset(cmdText);
				if (dataSet2 == null)
				{
					return list;
				}
				if (dataSet2.Tables.Count == 0 || dataSet2.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				DateTime dateTime3 = default(DateTime);
				Dictionary<int, string> dictionary = new Dictionary<int, string>();
				for (int i = 0; i < dataSet2.Tables[0].Columns.Count; i++)
				{
					dictionary.Add(i, dataSet2.Tables[0].Columns[i].ColumnName);
				}
				for (int j = 0; j < dataSet2.Tables[0].Rows.Count; j++)
				{
					DataRow dataRow = dataSet2.Tables[0].Rows[j];
					for (int k = 0; k < dataSet2.Tables[0].Columns.Count; k++)
					{
						HistoryDataItem historyDataItem = new HistoryDataItem();
						if (k == 0)
						{
							long ticks = (long)dataRow[k];
							dateTime3 = new DateTime(ticks);
							continue;
						}
						string text = dictionary[k];
						if (dataRow[k] is DBNull || dataRow[k] == null)
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = 0.0;
						}
						else if (dataRow[k] is bool)
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = (((bool)dataRow[k]) ? 1 : 0);
						}
						else
						{
							historyDataItem.dateTime = dateTime3;
							historyDataItem.dbName = dictionary[k];
							historyDataItem.value = float.Parse(dataRow[k].ToString());
						}
						list.Add(historyDataItem);
					}
				}
				dataSet2.Clear();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}
	}
}
