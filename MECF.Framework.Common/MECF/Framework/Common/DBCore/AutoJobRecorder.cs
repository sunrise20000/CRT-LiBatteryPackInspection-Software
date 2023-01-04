using System;
using Aitex.Core.RT.DBCore;

namespace MECF.Framework.Common.DBCore
{
	public class AutoJobRecorder
	{
		public static void Add(string waferguid, string recipename, string lotName, string position, string statues)
		{
			string sql = string.Format("INSERT INTO \"autojob_data\"(\"guid\", \"wafer_guid\", \"start_time\" , \"recipe_name\", \"lot_name\", \"current_position\",\"status\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}','{6}');", Guid.NewGuid(), waferguid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), recipename, lotName, position, statues);
			DB.Insert(sql);
		}

		public static void UpdatePosition(string waferguid, string position, string status)
		{
			string sql = $"Update \"autojob_data\" set \"current_position\"='{position}',\"status\"='{status}' where \"wafer_guid\"='{waferguid}';";
			DB.Insert(sql);
		}

		public static void EndJob(string waferguid)
		{
			string sql = string.Format("Update \"autojob_data\" set \"end_time\"='{0}' where \"wafer_guid\"='{1}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), waferguid);
			DB.Insert(sql);
		}
	}
}
