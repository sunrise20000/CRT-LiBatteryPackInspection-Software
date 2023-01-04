using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using SciChart.Core.Extensions;

namespace MECF.Framework.Common.DBCore
{
	public class JobMoveHistoryRecorder
	{
		public static void JobArrived(string jobGuid, string stationName)
		{
			string sql = string.Format("INSERT INTO \"job_move_history\"(\"job_guid\", \"arrive_time\", \"station\")VALUES ('{0}', '{1}', '{2}');", jobGuid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), stationName);
			DB.Insert(sql);
		}

		public static void JobLeft(string jobGuid, string stationName, string processTime)
		{
			string sql = string.Format("UPDATE \"job_move_history\" SET \"leave_time\"='{1}' , \"process_time\"='{2}' WHERE \"job_guid\"='{0}' AND \"station\"='{3}' AND \"leave_time\" is null ;", jobGuid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), processTime, stationName);
			DB.Insert(sql);
		}

		public static List<HistoryJobMoveData> QueryJobMovement(string jobGuid)
		{
			List<HistoryJobMoveData> list = new List<HistoryJobMoveData>();
			try
			{
				if (EnumerableExtensions.IsNullOrEmpty<char>((IEnumerable<char>)jobGuid))
				{
					return list;
				}
				string cmdText = $"SELECT * FROM \"job_move_history\" WHERE \"job_guid\"='{jobGuid}';";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryJobMoveData historyJobMoveData = new HistoryJobMoveData();
					historyJobMoveData.JobGuid = dataSet.Tables[0].Rows[i]["job_guid"].ToString();
					historyJobMoveData.Station = dataSet.Tables[0].Rows[i]["station"].ToString();
					historyJobMoveData.ProcessTime = dataSet.Tables[0].Rows[i]["process_time"].ToString();
					if (!dataSet.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
					{
						historyJobMoveData.ArriveTime = ((DateTime)dataSet.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					if (!dataSet.Tables[0].Rows[i]["leave_time"].Equals(DBNull.Value))
					{
						historyJobMoveData.LeaveTime = ((DateTime)dataSet.Tables[0].Rows[i]["leave_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					list.Add(historyJobMoveData);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<HistoryJobMoveData> QueryJobMovement(string jobGuid, string stationName)
		{
			List<HistoryJobMoveData> list = new List<HistoryJobMoveData>();
			try
			{
				string cmdText = $"SELECT * FROM \"job_move_history\" WHERE \"job_guid\"='{jobGuid}' AND \"station\"='{stationName}' ;";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryJobMoveData historyJobMoveData = new HistoryJobMoveData();
					historyJobMoveData.JobGuid = dataSet.Tables[0].Rows[i]["job_guid"].ToString();
					historyJobMoveData.Station = dataSet.Tables[0].Rows[i]["station"].ToString();
					historyJobMoveData.ProcessTime = dataSet.Tables[0].Rows[i]["process_time"].ToString();
					if (!dataSet.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
					{
						historyJobMoveData.ArriveTime = ((DateTime)dataSet.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					if (!dataSet.Tables[0].Rows[i]["leave_time"].Equals(DBNull.Value))
					{
						historyJobMoveData.LeaveTime = ((DateTime)dataSet.Tables[0].Rows[i]["leave_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					list.Add(historyJobMoveData);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<HistoryJobMoveData> QueryJobMovementBysql(string sql)
		{
			List<HistoryJobMoveData> list = new List<HistoryJobMoveData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryJobMoveData historyJobMoveData = new HistoryJobMoveData();
					historyJobMoveData.JobGuid = dataSet.Tables[0].Rows[i]["job_guid"].ToString();
					historyJobMoveData.Station = dataSet.Tables[0].Rows[i]["station"].ToString();
					historyJobMoveData.ProcessTime = dataSet.Tables[0].Rows[i]["process_time"].ToString();
					if (!dataSet.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
					{
						historyJobMoveData.ArriveTime = ((DateTime)dataSet.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					if (!dataSet.Tables[0].Rows[i]["leave_time"].Equals(DBNull.Value))
					{
						historyJobMoveData.LeaveTime = ((DateTime)dataSet.Tables[0].Rows[i]["leave_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					list.Add(historyJobMoveData);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}
	}
}
