using System;
using System.Data;
using System.Text;
using Aitex.Core.RT.DBCore;

namespace MECF.Framework.Common.DBCore
{
	public class RecipeInfoRecorer
	{
		public static bool WriteRecipeInfo(string rootName, string recipeName, string fileStr)
		{
			try
			{
				if (recipeName.IndexOf("\\") == 0)
				{
					recipeName = recipeName.TrimStart('\\');
				}
				rootName = rootName.TrimEnd('\\');
				recipeName += ".rcp";
				byte[] bytes = Encoding.UTF8.GetBytes(fileStr);
				string cmdText = "Select * from \"recipe_data\" where  \"rootName\"='" + rootName + "' and \"recipeName\"='" + recipeName + "';";
				DataSet dataSet = DB.ExecuteDataset(cmdText, null);
				if (dataSet != null && dataSet.Tables != null && dataSet.Tables[0].Rows.Count > 0)
				{
					cmdText = "update \"recipe_data\" set \"fileDetail\"=@fileDetail where \"rootName\"='" + rootName + "' and \"recipeName\"='" + recipeName + "'";
					return DB.ExcuteQuery(cmdText, new string[1] { "fileDetail" }, bytes) > 0;
				}
				cmdText = "Insert into \"recipe_data\" (\"rootName\",\"recipeName\",\"fileDetail\") values ('" + rootName + "','" + recipeName + "',@fileDetail)";
				return DB.ExcuteQuery(cmdText, new string[1] { "fileDetail" }, bytes) > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool CreateFolder(string rootName, string folder)
		{
			try
			{
				rootName = rootName.TrimEnd('\\');
				string cmdText = "Select * from \"recipe_data\" where  \"rootName\"='" + rootName + "' and \"recipeName\"='" + folder + "';";
				DataSet dataSet = DB.ExecuteDataset(cmdText, null);
				if (dataSet != null && dataSet.Tables != null && dataSet.Tables[0].Rows.Count > 0)
				{
					return true;
				}
				cmdText = "Insert into \"recipe_data\" (\"rootName\",\"recipeName\") values ('" + rootName + "','" + folder + "')";
				return DB.ExcuteQuery(cmdText, null) > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static void DeleteFolder(string rootName, string recipeName)
		{
			if (recipeName.IndexOf("\\") == 0)
			{
				recipeName = recipeName.TrimStart('\\');
			}
			rootName = rootName.TrimEnd('\\');
			string cmdText = "Delete from \"recipe_data\" where \"rootName\"='" + rootName + "' and \"recipeName\"='" + recipeName + "';";
			DB.ExcuteQuery(cmdText, null, null);
		}

		public static string GetRecipeInfoByFileName(string rootName, string recipeName)
		{
			try
			{
				if (recipeName.IndexOf("\\") == 0)
				{
					recipeName = recipeName.TrimStart('\\');
				}
				rootName = rootName.TrimEnd('\\');
				recipeName += ".rcp";
				string text = rootName + "\\" + recipeName;
				if (string.IsNullOrEmpty(rootName))
				{
					text = recipeName;
				}
				string cmdText = "Select \"fileDetail\" from \"recipe_data\" where \"rootName\"||'\\'||\"recipeName\" ='" + text + "';";
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

		public static void DeleteRecipeInfoByFileName(string rootName, string recipeName)
		{
			if (recipeName.IndexOf("\\") == 0)
			{
				recipeName = recipeName.TrimStart('\\');
			}
			rootName = rootName.TrimEnd('\\');
			recipeName += ".rcp";
			string cmdText = "Delete from \"recipe_data\" where \"rootName\"='" + rootName + "' and \"recipeName\"='" + recipeName + "';";
			DB.ExcuteQuery(cmdText, null, null);
		}

		public static void RenameRecipeFileName(string rootName, string recipeName, string newFileName)
		{
			if (recipeName.IndexOf("\\") == 0)
			{
				recipeName = recipeName.TrimStart('\\');
			}
			rootName = rootName.TrimEnd('\\');
			recipeName += ".rcp";
			newFileName += ".rcp";
			string cmdText = "update \"recipe_data\" set \"recipeName\"='" + newFileName + "' where \"rootName\"='" + rootName + "' and \"recipeName\"='" + recipeName + "';";
			DB.ExcuteQuery(cmdText, null, null);
		}

		public static DataTable GetRootAllDirectoryAndFiles(string rootName)
		{
			string cmdText = "Select \"recipeName\" from \"recipe_data\" where \"rootName\"='" + rootName + "' order by \"recipe_data\" ;";
			DataSet dataSet = DB.ExecuteDataset(cmdText, null);
			if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0] != null && dataSet.Tables[0].Rows.Count > 0)
			{
				return dataSet.Tables[0];
			}
			return null;
		}

		public static DataTable GetAllData()
		{
			string cmdText = "Select * from \"recipe_data\" ;";
			DataSet dataSet = DB.ExecuteDataset(cmdText, null);
			if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0] != null && dataSet.Tables[0].Rows.Count > 0)
			{
				return dataSet.Tables[0];
			}
			return null;
		}

		public static void DeleteAllData()
		{
			string cmdText = "Delete from \"recipe_data\" ;";
			DB.ExcuteQuery(cmdText, null, null);
		}
	}
}
