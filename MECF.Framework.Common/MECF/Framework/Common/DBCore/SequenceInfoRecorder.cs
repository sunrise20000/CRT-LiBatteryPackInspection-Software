using System;
using System.Data;
using System.Text;
using Aitex.Core.RT.DBCore;

namespace MECF.Framework.Common.DBCore
{
	public class SequenceInfoRecorder
	{
		public static bool WriteSequenceInfo(string seqName, string fileStr)
		{
			try
			{
				if (seqName.IndexOf("\\") == 0)
				{
					seqName = seqName.Substring(1);
				}
				seqName += ".seq";
				byte[] bytes = Encoding.UTF8.GetBytes(fileStr);
				string cmdText = "Select * from \"sequence_data\" where  \"seqName\"='" + seqName + "';";
				DataSet dataSet = DB.ExecuteDataset(cmdText, null);
				if (dataSet != null && dataSet.Tables != null && dataSet.Tables[0].Rows.Count > 0)
				{
					cmdText = "update \"sequence_data\" set \"fileDetail\"=@fileDetail where  \"seqName\"='" + seqName + "'";
					return DB.ExcuteQuery(cmdText, new string[1] { "fileDetail" }, bytes) > 0;
				}
				cmdText = "Insert into \"sequence_data\" (\"seqName\",\"fileDetail\") values ('" + seqName + "',@fileDetail)";
				return DB.ExcuteQuery(cmdText, new string[1] { "fileDetail" }, bytes) > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static string GetSequenceInfoByFileName(string seqName)
		{
			try
			{
				if (seqName.IndexOf("\\") == 0)
				{
					seqName = seqName.Substring(1);
				}
				seqName += ".seq";
				string cmdText = "Select \"fileDetail\" from \"sequence_data\" where \"seqName\"='" + seqName + "';";
				DataSet dataSet = DB.ExecuteDataset(cmdText, null);
				if (dataSet != null && dataSet.Tables != null && dataSet.Tables[0].Rows.Count > 0)
				{
					object obj = dataSet.Tables[0].Rows[0][0];
					return Encoding.UTF8.GetString((byte[])obj);
				}
				return "";
			}
			catch (Exception)
			{
				return "";
			}
		}

		public static void RenameFileName(string oldName, string newName)
		{
			if (oldName.IndexOf("\\") == 0)
			{
				oldName = oldName.Substring(1);
			}
			oldName += ".seq";
			newName += ".seq";
			string sql = "update \"sequence_data\" set \"seqName\"='" + newName + "' where \"seqName\"='" + oldName + "';";
			DB.Insert(sql);
		}

		public static void DeleteByFileName(string seqName)
		{
			if (seqName.IndexOf("\\") == 0)
			{
				seqName = seqName.Substring(1);
			}
			seqName += ".seq";
			string sql = "Delete from \"sequence_data\" where  \"seqName\"='" + seqName + "';";
			DB.Insert(sql);
		}

		public static DataTable GetAllData()
		{
			string cmdText = "Select * from \"sequence_data\" ;";
			DataSet dataSet = DB.ExecuteDataset(cmdText, null);
			if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0] != null && dataSet.Tables[0].Rows.Count > 0)
			{
				return dataSet.Tables[0];
			}
			return null;
		}

		public static void DeleteAllData()
		{
			string sql = "Delete from \"sequence_data\" ;";
			DB.Insert(sql);
		}
	}
}
