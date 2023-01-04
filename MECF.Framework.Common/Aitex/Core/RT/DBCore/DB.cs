using System;
using System.Collections.Generic;
using System.Data;

namespace Aitex.Core.RT.DBCore
{
	public static class DB
	{
		public static ICommonDB Instance { private get; set; }

		public static void Insert(string sql)
		{
			if (Instance != null)
			{
				Instance.Insert(sql);
			}
		}

		public static void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey)
		{
			if (Instance != null)
			{
				Instance.CreateTableIfNotExisted(table, columns, addPID, primaryKey);
			}
		}

		public static void CreateTableIndexIfNotExisted(string table, string index, string sql)
		{
			if (Instance != null)
			{
				Instance.CreateTableIndexIfNotExisted(table, index, sql);
			}
		}

		public static void CreateTableColumn(string table, Dictionary<string, Type> columns)
		{
			if (Instance != null)
			{
				Instance.CreateTableColumn(table, columns);
			}
		}

		public static DataSet ExecuteDataset(string cmdText, params object[] p)
		{
			if (Instance != null)
			{
				return Instance.ExecuteDataset(cmdText, p);
			}
			return null;
		}

		public static bool ExcuteTransAction(List<string> cmdText)
		{
			if (Instance != null)
			{
				return Instance.ExcuteTransAction(cmdText);
			}
			return false;
		}

		public static int ExcuteQuery(string cmdText, string[] paramsColumn, params object[] p)
		{
			if (Instance != null)
			{
				return Instance.ExecuteNonQueryWithParam(cmdText, paramsColumn, p);
			}
			return 0;
		}
	}
}
