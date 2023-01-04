using System;
using System.Collections.Generic;
using System.Data;

namespace Aitex.Core.RT.DBCore
{
	public interface ICommonDB
	{
		void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey);

		void CreateTableIndexIfNotExisted(string table, string index, string sql);

		void CreateTableColumn(string table, Dictionary<string, Type> columns);

		void Insert(string sql);

		int ExecuteNonQueryWithParam(string cmdText, string[] colunmName, params object[] p);

		bool ExcuteTransAction(List<string> sql);

		DataSet ExecuteDataset(string cmdText, params object[] p);
	}
}
