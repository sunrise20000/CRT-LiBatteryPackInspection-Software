using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.DBCore
{
	public class LinearProcessDataRecorder
	{
		public static void Start(string guid, string recipeName, int ppId, string batchId, string cassette1Id, string cassette2Id, string user)
		{
			string sql = string.Format("INSERT INTO \"process_data_linear\"(\"guid\", \"process_begin_time\", \"recipe_name\" , \"pp_id\", \"batch_id\", \"cassette1_id\", \"cassette2_id\", \"user\" , \"cassette1_load_wafer_position\", \"cassette2_load_wafer_position\", \"cassette1_unload_wafer_position\", \"cassette2_unload_wafer_position\")VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}',' ',' ',' ',' ');", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), recipeName, ppId, batchId, cassette1Id, cassette2Id, user);
			DB.Insert(sql);
		}

		public static void UpdateWaferCount(string guid, int cassette1Count, int cassette2Count)
		{
			string sql = string.Format("UPDATE \"process_data_linear\" SET \"cassette1_count\"='{1}', \"cassette2_count\"='{2}' WHERE \"guid\"='{0}';", guid, cassette1Count, cassette2Count);
			DB.Insert(sql);
		}

		public static void UpdateLoadWaferPosition(string guid, string cassette1WaferPosition, string cassette2WaferPosition)
		{
			string sql = string.Format("UPDATE \"process_data_linear\" SET \"cassette1_load_wafer_position\" = '{1}', \"cassette2_load_wafer_position\"='{2}' WHERE \"guid\"='{0}';", guid, cassette1WaferPosition, cassette2WaferPosition);
			DB.Insert(sql);
		}

		public static void UpdateUnloadWaferPosition(string guid, string cassette1WaferPosition, string cassette2WaferPosition)
		{
			string sql = string.Format("UPDATE \"process_data_linear\" SET \"cassette1_unload_wafer_position\" = '{1}', \"cassette2_unload_wafer_position\"='{2}' WHERE \"guid\"='{0}';", guid, cassette1WaferPosition, cassette2WaferPosition);
			DB.Insert(sql);
		}

		public static void End(string guid, string finishType)
		{
			string sql = string.Format("UPDATE \"process_data_linear\" SET \"process_end_time\"='{1}',\"finish_type\"='{2}' WHERE \"guid\"='{0}';", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), finishType);
			DB.Insert(sql);
		}

		public static List<string> GetProcesData(int ppId, string batchId)
		{
			List<string> list = new List<string>();
			string cmdText = $"SELECT * FROM \"process_data_linear\" where \"pp_id\" = '{ppId.ToString()}' and \"batch_id\" = '{batchId}' and (\"finish_type\" is null or \"finish_type\" != 'RecipeFinish')  order by \"process_begin_time\" ASC;";
			DataSet dataSet = DB.ExecuteDataset(cmdText);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				return list;
			}
			DataTable dataTable = dataSet.Tables[0];
			if (dataTable == null || dataTable.Rows.Count == 0)
			{
				return list;
			}
			try
			{
				list.Add(dataTable.Rows[dataTable.Rows.Count - 1]["guid"].ToString());
				list.Add(dataTable.Rows[dataTable.Rows.Count - 1]["cassette1_count"].ToString());
				list.Add(dataTable.Rows[dataTable.Rows.Count - 1]["cassette2_count"].ToString());
				list.Add(dataTable.Rows[dataTable.Rows.Count - 1]["process_begin_time"].ToString());
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return null;
			}
			return list;
		}
	}
}
