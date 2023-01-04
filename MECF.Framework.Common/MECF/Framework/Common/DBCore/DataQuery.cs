using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.DBCore
{
	public static class DataQuery
	{
		public static DataTable Query(string sql)
		{
			DataTable result = new DataTable("result");
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet != null)
				{
					result = dataSet.Tables[0];
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return result;
		}

		public static bool ExcuteTransAction(List<string> sql)
		{
			try
			{
				return DB.ExcuteTransAction(sql);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}
	}
}
