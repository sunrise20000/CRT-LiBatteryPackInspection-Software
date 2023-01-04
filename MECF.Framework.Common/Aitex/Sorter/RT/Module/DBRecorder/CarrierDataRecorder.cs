using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace Aitex.Sorter.RT.Module.DBRecorder
{
	public class CarrierDataRecorder
	{
		public static void Loaded(string guid, string station)
		{
			string sql = string.Format("INSERT INTO \"carrier_data\"(\"guid\", \"load_time\", \"station\" )VALUES ('{0}', '{1}', '{2}');", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), station);
			DB.Insert(sql);
		}

		public static void UpdateCarrierId(string guid, string rfid)
		{
			string sql = $"UPDATE \"carrier_data\" SET \"rfid\"='{rfid}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void UpdateLotId(string guid, string lotId)
		{
			string sql = $"UPDATE \"carrier_data\" SET \"lot_id\"='{lotId}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void UpdateProductCategory(string guid, string productCategory)
		{
			string sql = $"UPDATE \"carrier_data\" SET \"product_category\"='{productCategory}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void Unloaded(string guid)
		{
			string sql = string.Format("UPDATE \"carrier_data\" SET \"unload_time\"='{0}' WHERE \"guid\"='{1}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), guid);
			DB.Insert(sql);
		}

		public static List<HistoryCarrierData> QueryDBCarrier(string sql)
		{
			List<HistoryCarrierData> list = new List<HistoryCarrierData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryCarrierData historyCarrierData = new HistoryCarrierData();
					historyCarrierData.Guid = dataSet.Tables[0].Rows[i]["guid"].ToString();
					historyCarrierData.Rfid = dataSet.Tables[0].Rows[i]["rfid"].ToString();
					historyCarrierData.LotId = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
					historyCarrierData.ProductCategory = dataSet.Tables[0].Rows[i]["product_category"].ToString();
					historyCarrierData.Station = dataSet.Tables[0].Rows[i]["station"].ToString();
					if (!dataSet.Tables[0].Rows[i]["load_time"].Equals(DBNull.Value))
					{
						historyCarrierData.LoadTime = ((DateTime)dataSet.Tables[0].Rows[i]["load_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					if (!dataSet.Tables[0].Rows[i]["unload_time"].Equals(DBNull.Value))
					{
						historyCarrierData.UnloadTime = ((DateTime)dataSet.Tables[0].Rows[i]["unload_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					list.Add(historyCarrierData);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<WaferHistoryLot> QueryWaferHistoryLotsBySql(string sql)
		{
			List<WaferHistoryLot> list = new List<WaferHistoryLot>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
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
					WaferHistoryLot waferHistoryLot = new WaferHistoryLot();
					if (!dataSet.Tables[0].Rows[i]["lot_id"].Equals(DBNull.Value))
					{
						waferHistoryLot.CarrierID = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
						waferHistoryLot.Name = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
						waferHistoryLot.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
						waferHistoryLot.Type = WaferHistoryItemType.Lot;
						if (!dataSet.Tables[0].Rows[i]["rfid"].Equals(DBNull.Value))
						{
							waferHistoryLot.Rfid = dataSet.Tables[0].Rows[i]["rfid"].ToString();
						}
						if (!dataSet.Tables[0].Rows[i]["load_time"].Equals(DBNull.Value))
						{
							waferHistoryLot.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["load_time"].ToString());
						}
						if (!dataSet.Tables[0].Rows[i]["unload_time"].Equals(DBNull.Value))
						{
							waferHistoryLot.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["unload_time"].ToString());
						}
						DataSet dataSet2 = DB.ExecuteDataset($"SELECT * FROM \"wafer_data\" Where \"carrier_data_guid\"='{waferHistoryLot.ID}' ;");
						if (dataSet2 != null && dataSet2.Tables.Count != 0 && dataSet2.Tables[0].Rows.Count != 0)
						{
							waferHistoryLot.WaferCount = dataSet2.Tables[0].Rows.Count;
							waferHistoryLot.FaultWaferCount = dataSet2.Tables[0].Select("process_status='Failed'").Length;
						}
						list.Add(waferHistoryLot);
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}

		public static List<WaferHistoryLot> GetWaferHistoryLots(DateTime startTime, DateTime endTime, string keyWord)
		{
			List<WaferHistoryLot> list = new List<WaferHistoryLot>();
			try
			{
				string text = "";
				if (keyWord != null && !string.IsNullOrEmpty(keyWord.Trim()))
				{
					text = "and (";
					string[] array = keyWord.Split(',');
					for (int i = 0; i < array.Length; i++)
					{
						text = text + "\"lot_id\" like '%" + array[i].Trim() + "%'";
						if (i < array.Length - 1)
						{
							text += " or ";
						}
					}
					text += ")";
				}
				string cmdText = string.Format("SELECT * FROM \"carrier_data\" where (\"unload_time\" is null and \"load_time\" >= '{2}') or (\"load_time\" >= '{0}' and \"load_time\" <= '{1}') or (\"unload_time\" >= '{0}' and \"unload_time\" <= '{1}') or (\"load_time\" <= '{0}' and \"unload_time\" >= '{1}')  {3} order by \"station\" ASC, \"load_time\" ASC limit 1000;", startTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), startTime.AddDays(-1.0).ToString("yyyy/MM/dd HH:mm:ss.fff"), endTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), text);
				DataSet dataSet = DB.ExecuteDataset(cmdText);
				if (dataSet == null)
				{
					return list;
				}
				if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				{
					return list;
				}
				for (int j = 0; j < dataSet.Tables[0].Rows.Count; j++)
				{
					WaferHistoryLot waferHistoryLot = new WaferHistoryLot();
					if (!dataSet.Tables[0].Rows[j]["lot_id"].Equals(DBNull.Value))
					{
						waferHistoryLot.CarrierID = dataSet.Tables[0].Rows[j]["lot_id"].ToString();
						waferHistoryLot.Name = dataSet.Tables[0].Rows[j]["lot_id"].ToString();
						waferHistoryLot.ID = dataSet.Tables[0].Rows[j]["guid"].ToString();
						waferHistoryLot.Type = WaferHistoryItemType.Lot;
						if (!dataSet.Tables[0].Rows[j]["rfid"].Equals(DBNull.Value))
						{
							waferHistoryLot.Rfid = dataSet.Tables[0].Rows[j]["rfid"].ToString();
						}
						if (!dataSet.Tables[0].Rows[j]["load_time"].Equals(DBNull.Value))
						{
							waferHistoryLot.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[j]["load_time"].ToString());
						}
						if (!dataSet.Tables[0].Rows[j]["unload_time"].Equals(DBNull.Value))
						{
							waferHistoryLot.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[j]["unload_time"].ToString());
						}
						DataSet dataSet2 = DB.ExecuteDataset($"SELECT * FROM \"wafer_data\" Where \"carrier_data_guid\"='{waferHistoryLot.ID}' ;");
						if (dataSet2 != null && dataSet2.Tables.Count != 0 && dataSet2.Tables[0].Rows.Count != 0)
						{
							waferHistoryLot.WaferCount = dataSet2.Tables[0].Rows.Count;
							waferHistoryLot.FaultWaferCount = dataSet2.Tables[0].Select("process_status='Failed'").Length;
						}
						list.Add(waferHistoryLot);
					}
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
