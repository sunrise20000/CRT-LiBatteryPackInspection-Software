using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class RecipeDataRecorder
	{
		public static void RecipeStart(string guid, string waferDataGuid, string recipeName, string settingTime, string chamber)
		{
			string sql = string.Format("INSERT INTO \"recipe_data\"(\"guid\", \"wafer_data_guid\", \"recipe_begin_time\", \"recipe_name\", \"recipe_setting_time\" , \"chamber\"  )VALUES ('{0}', '{1}', '{2}' , '{3}', '{4}', '{5}' );", guid, waferDataGuid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), recipeName, settingTime, chamber);
			DB.Insert(sql);
		}

		public static void RecipeEnd(string guid)
		{
			string sql = string.Format("UPDATE \"recipe_data\" SET \"recipe_end_time\"='{0}' WHERE \"guid\"='{1}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), guid);
			DB.Insert(sql);
		}

		public static void RecipeStepStart(string guid, int recipeStepNo, string recipeStepName, string settingTime)
		{
			string sql = string.Format("INSERT INTO \"recipe_step_data\"(\"guid\", \"recipe_step_no\", \"recipe_step_name\", \"recipe_step_setting_time\" , \"recipe_step_begin_time\")VALUES ('{0}', '{1}', '{2}' , '{3}', '{4}' );", guid, recipeStepNo, recipeStepName, settingTime, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
			DB.Insert(sql);
		}

		public static void RecipeStepEnd(string guid, int recipeStepNo)
		{
			string sql = string.Format("UPDATE \"recipe_step_data\" SET \"recipe_step_end_time\"='{0}' WHERE \"guid\"='{1}' AND \"recipe_step_no\"='{2}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), guid, recipeStepNo);
			DB.Insert(sql);
		}

		public static WaferHistoryRecipe GetWaferHistoryRecipe(string id)
		{
			WaferHistoryRecipe waferHistoryRecipe = new WaferHistoryRecipe();
			try
			{
				string cmdText = $"SELECT * FROM \"recipe_data\" where \"guid\" = '{id}'  limit 1000;";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return waferHistoryRecipe;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return waferHistoryRecipe;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					waferHistoryRecipe.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
					waferHistoryRecipe.Type = WaferHistoryItemType.Recipe;
					waferHistoryRecipe.Chamber = dataSet.Tables[0].Rows[i]["chamber"].ToString();
					waferHistoryRecipe.SettingTime = dataSet.Tables[0].Rows[i]["recipe_setting_time"].ToString();
					if (!dataSet.Tables[0].Rows[i]["recipe_begin_time"].Equals(DBNull.Value))
					{
						waferHistoryRecipe.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["recipe_begin_time"].ToString());
					}
					if (!dataSet.Tables[0].Rows[i]["recipe_end_time"].Equals(DBNull.Value))
					{
						waferHistoryRecipe.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["recipe_end_time"].ToString());
					}
					waferHistoryRecipe.ActualTime = ((waferHistoryRecipe.EndTime.CompareTo(waferHistoryRecipe.StartTime) <= 0) ? "" : waferHistoryRecipe.EndTime.Subtract(waferHistoryRecipe.StartTime).ToString());
					waferHistoryRecipe.Recipe = dataSet.Tables[0].Rows[i]["recipe_name"].ToString();
					waferHistoryRecipe.Name = dataSet.Tables[0].Rows[i]["recipe_name"].ToString();
					waferHistoryRecipe.Steps = GetRecipeStepInfoList(id);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return waferHistoryRecipe;
		}

		public static List<WaferHistoryWafer> GetWaferHistoryWafers(string id)
		{
			List<WaferHistoryWafer> list = new List<WaferHistoryWafer>();
			try
			{
				string cmdText = $"SELECT * FROM \"wafer_data\" where \"carrier_data_guid\" = '{id}' and \"lot_id\" <> '' order by  \"wafer_id\"  ASC  limit 1000;";
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
					WaferHistoryWafer waferHistoryWafer = new WaferHistoryWafer();
					waferHistoryWafer.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
					waferHistoryWafer.Type = WaferHistoryItemType.Wafer;
					waferHistoryWafer.Name = dataSet.Tables[0].Rows[i]["wafer_id"].ToString();
					if (!dataSet.Tables[0].Rows[i]["sequence_name"].Equals(DBNull.Value))
					{
						waferHistoryWafer.ProcessJob = dataSet.Tables[0].Rows[i]["sequence_name"].ToString();
					}
					if (!dataSet.Tables[0].Rows[i]["create_time"].Equals(DBNull.Value))
					{
						waferHistoryWafer.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["create_time"].ToString());
					}
					if (!dataSet.Tables[0].Rows[i]["delete_time"].Equals(DBNull.Value))
					{
						waferHistoryWafer.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["delete_time"].ToString());
					}
					list.Add(waferHistoryWafer);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		private static List<RecipeStep> GetRecipeStepInfoList(string guid)
		{
			List<RecipeStep> list = new List<RecipeStep>();
			DataSet dataSet = DB.ExecuteDataset($"SELECT * FROM \"recipe_step_data\" where \"guid\" = '{guid}'  limit 1000;");
			if (dataSet != null && dataSet.Tables.Count != 0 && dataSet.Tables[0].Rows.Count != 0)
			{
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					RecipeStep recipeStep = new RecipeStep();
					recipeStep.No = int.Parse(dataSet.Tables[0].Rows[i]["recipe_step_no"].ToString());
					recipeStep.Name = dataSet.Tables[0].Rows[i]["recipe_step_name"].ToString();
					recipeStep.SettingTime = dataSet.Tables[0].Rows[i]["recipe_step_setting_time"].ToString();
					if (!dataSet.Tables[0].Rows[i]["recipe_step_begin_time"].Equals(DBNull.Value))
					{
						recipeStep.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["recipe_step_begin_time"].ToString());
					}
					if (!dataSet.Tables[0].Rows[i]["recipe_step_end_time"].Equals(DBNull.Value))
					{
						recipeStep.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["recipe_step_end_time"].ToString());
					}
					recipeStep.ActualTime = ((recipeStep.EndTime.CompareTo(recipeStep.StartTime) <= 0) ? "" : recipeStep.EndTime.Subtract(recipeStep.StartTime).ToString());
					list.Add(recipeStep);
				}
			}
			return list;
		}

		public static List<WaferHistoryRecipe> GetWaferHistoryRecipes(string id)
		{
			List<WaferHistoryRecipe> list = new List<WaferHistoryRecipe>();
			try
			{
				string cmdText = $"SELECT * FROM \"process_data\" where \"wafer_data_guid\" = '{id}'  limit 1000;";
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
					list.Add(GetWaferHistoryRecipe(dataSet.Tables[0].Rows[i]["guid"].ToString()));
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<WaferHistorySecquence> GetWaferHistorySecquences(string id)
		{
			List<WaferHistorySecquence> list = new List<WaferHistorySecquence>();
			try
			{
				string cmdText = $"SELECT * FROM \"wafer_data\" where \"guid\" = '{id}'  and \"lot_id\" <> '' order by  \"wafer_id\"  ASC  limit 1000;";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return list;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				int num = 0;
				try
				{
					cmdText = $"SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{id}' order by \"arrive_time\" ASC limit 1000;";
					DataSet dataSet2 = DB.ExecuteDataset(cmdText);
					if (dataSet2.Tables.Count == 0 || dataSet2.Tables[0].Rows.Count == 0)
					{
						return list;
					}
					for (int i = 0; i < dataSet2.Tables[0].Rows.Count; i++)
					{
						if (dataSet2.Tables[0].Rows[i]["station"].ToString() == "Cassette")
						{
							num = i + 1;
							break;
						}
						if (i == dataSet2.Tables[0].Rows.Count - 1)
						{
							num = i + 1;
						}
					}
					num = ((dataSet2.Tables[0].Rows.Count % num <= 0) ? (dataSet2.Tables[0].Rows.Count / num) : (dataSet2.Tables[0].Rows.Count / num + 1));
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
				for (int j = 0; j < num; j++)
				{
					list.Add(GetWaferHistorySecquence(id, dataSet.Tables[0].Rows[0]["sequence_name"].ToString(), j));
				}
			}
			catch (Exception ex2)
			{
				LOG.Write(ex2);
			}
			return list;
		}

		public static WaferHistorySecquence GetWaferHistorySecquence(string id, string SecquenceName, int count)
		{
			WaferHistorySecquence waferHistorySecquence = new WaferHistorySecquence();
			try
			{
				string cmdText = $"SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{id}' order by \"arrive_time\" ASC limit 1000;";
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return waferHistorySecquence;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return waferHistorySecquence;
				}
				int num = 0;
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					if (dataSet.Tables[0].Rows[i]["station"].ToString() == "Cassette")
					{
						num = i;
						break;
					}
					if (i == dataSet.Tables[0].Rows.Count - 1)
					{
						num = i;
					}
				}
				int num2 = 0;
				if (num2 < dataSet.Tables[0].Rows.Count)
				{
					num2 = ((count > 0) ? (num * count + count - 1) : (num * count + count));
					waferHistorySecquence.SecquenceName = SecquenceName;
					waferHistorySecquence.Recipe = SecquenceName;
					waferHistorySecquence.SecQuenceStartTime = dataSet.Tables[0].Rows[num2]["arrive_time"].ToString();
					waferHistorySecquence.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[num2]["arrive_time"].ToString());
					num2 = num * count + count;
					num2 += num;
					if (num2 >= dataSet.Tables[0].Rows.Count)
					{
						waferHistorySecquence.SecQuenceEndTime = dataSet.Tables[0].Rows[dataSet.Tables[0].Rows.Count - 1]["arrive_time"].ToString();
						waferHistorySecquence.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[dataSet.Tables[0].Rows.Count - 1]["arrive_time"].ToString());
					}
					else
					{
						waferHistorySecquence.SecQuenceEndTime = dataSet.Tables[0].Rows[num2]["arrive_time"].ToString();
						waferHistorySecquence.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[num2]["arrive_time"].ToString());
					}
					waferHistorySecquence.ActualTime = ((waferHistorySecquence.EndTime.CompareTo(waferHistorySecquence.StartTime) <= 0) ? "" : waferHistorySecquence.EndTime.Subtract(waferHistorySecquence.StartTime).ToString());
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return waferHistorySecquence;
		}
	}
}
