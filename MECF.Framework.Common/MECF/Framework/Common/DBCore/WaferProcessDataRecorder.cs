using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class WaferProcessDataRecorder
	{
		public static void WaferProcessDataRecord(string guid, string station, string dataName, string dataValue)
		{
			string sql = string.Format("INSERT INTO \"wafer_process_data\"(\"wafer_guid\", \"process_time\", \"station\", \"data_name\", \"data_value\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}' );", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), station, dataName, dataValue);
			DB.Insert(sql);
		}

		public static List<WaferHistoryMetrology> GetWaferProcessDatas(string id)
		{
			List<WaferHistoryMetrology> list = new List<WaferHistoryMetrology>();
			try
			{
				string cmdText = $"SELECT * FROM \"wafer_process_data\" where \"wafer_guid\" = '{id}' order by \"process_time\" ASC limit 1000;";
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
					WaferHistoryMetrology waferHistoryMetrology = new WaferHistoryMetrology();
					waferHistoryMetrology.stationname = dataSet.Tables[0].Rows[i]["station"].ToString();
					waferHistoryMetrology.processtime = dataSet.Tables[0].Rows[i]["processtime"].ToString();
					waferHistoryMetrology.dataname = dataSet.Tables[0].Rows[i]["data_name"].ToString();
					waferHistoryMetrology.datavalue = dataSet.Tables[0].Rows[i]["data_value"].ToString();
					list.Add(waferHistoryMetrology);
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
