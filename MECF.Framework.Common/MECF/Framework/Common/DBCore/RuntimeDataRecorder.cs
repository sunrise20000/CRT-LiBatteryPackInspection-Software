using System;
using System.Data;
using Aitex.Core.RT.DBCore;

namespace MECF.Framework.Common.DBCore
{
	public class RuntimeDataRecorder
	{
		public static void UpdateElapseTimeSystem(int minutes)
		{
			string cmdText = $"Select  \"guid\",\"elapse_minutes\" from \"runtime_data\" where \"ispm\"='false';";
			DataSet dataSet = DB.ExecuteDataset(cmdText, null);
			if (dataSet != null && dataSet.Tables != null && dataSet.Tables[0].Rows.Count > 0)
			{
				cmdText = "";
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					long num = Convert.ToInt64(dataSet.Tables[0].Rows[i]["elapse_minutes"].ToString()) + minutes;
					string arg = dataSet.Tables[0].Rows[i]["guid"].ToString();
					cmdText += $"Update \"runtime_data\" set  \"elapse_minutes\"='{num}' where \"guid\"='{arg}';";
				}
				if (!string.IsNullOrEmpty(cmdText))
				{
					DB.Insert(cmdText);
				}
			}
		}

		public static void UpdateElapseTimePM(string pmName, int minutes)
		{
			string cmdText = $"Select  \"guid\",\"elapse_minutes\" from \"runtime_data\" where \"ispm\"='true' and \"device_name\"='{pmName}';";
			DataSet dataSet = DB.ExecuteDataset(cmdText, null);
			if (dataSet != null && dataSet.Tables != null && dataSet.Tables[0].Rows.Count > 0)
			{
				cmdText = "";
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					long num = Convert.ToInt64(dataSet.Tables[0].Rows[i]["elapse_minutes"].ToString()) + minutes;
					string arg = dataSet.Tables[0].Rows[i]["guid"].ToString();
					cmdText += $"Update \"runtime_data\" set  \"elapse_minutes\"='{num}' where \"guid\"='{arg}';";
				}
				if (!string.IsNullOrEmpty(cmdText))
				{
					DB.Insert(cmdText);
				}
			}
			else
			{
				cmdText = $"INSERT INTO \"runtime_data\" (\"guid\", \"device_name\", \"set_minutes\" , \"elapse_minutes\", \"ispm\" )VALUES ('{Guid.NewGuid()}', '{pmName}', {0}, {minutes}, 'true');";
				DB.Insert(cmdText);
			}
		}
	}
}
