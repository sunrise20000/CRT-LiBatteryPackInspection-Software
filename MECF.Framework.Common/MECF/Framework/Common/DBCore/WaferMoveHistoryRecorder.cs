using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class WaferMoveHistoryRecorder
	{
		public static void WaferMoved(string guid, string station, int slot, string status)
		{
			string sql = string.Format("INSERT INTO \"wafer_move_history\"(\"wafer_data_guid\", \"arrive_time\", \"station\", \"slot\", \"status\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}' );", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), station, slot + 1, status);
			DB.Insert(sql);
		}

		public static List<HistoryMoveData> QueryDBMovement(string sql)
		{
			List<HistoryMoveData> list = new List<HistoryMoveData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryMoveData historyMoveData = new HistoryMoveData();
					historyMoveData.WaferGuid = dataSet.Tables[0].Rows[i]["wafer_data_guid"].ToString();
					historyMoveData.Station = dataSet.Tables[0].Rows[i]["station"].ToString();
					historyMoveData.Slot = dataSet.Tables[0].Rows[i]["slot"].ToString();
					historyMoveData.Result = dataSet.Tables[0].Rows[i]["status"].ToString();
					if (!dataSet.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
					{
						historyMoveData.ArriveTime = ((DateTime)dataSet.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					list.Add(historyMoveData);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<WaferHistoryMovement> GetWaferHistoryMovements(string id)
		{
			List<WaferHistoryMovement> list = new List<WaferHistoryMovement>();
			try
			{
				string cmdText = $"SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{id}' order by \"arrive_time\" ASC limit 1000;";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return list;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					WaferHistoryMovement waferHistoryMovement = new WaferHistoryMovement();
					waferHistoryMovement.Source = string.Format("station : {0} slot : {1}", dataSet.Tables[0].Rows[i]["station"], dataSet.Tables[0].Rows[i]["slot"]);
					if (i != dataSet.Tables[0].Rows.Count - 1)
					{
						waferHistoryMovement.Destination = string.Format("station : {0} slot : {1}", dataSet.Tables[0].Rows[i + 1]["station"], dataSet.Tables[0].Rows[i + 1]["slot"]);
					}
					else
					{
						waferHistoryMovement.Destination = "";
					}
					waferHistoryMovement.InTime = dataSet.Tables[0].Rows[i]["arrive_time"].ToString();
					list.Add(waferHistoryMovement);
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
