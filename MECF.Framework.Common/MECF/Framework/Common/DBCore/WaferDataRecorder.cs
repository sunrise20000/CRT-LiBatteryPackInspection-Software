using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class WaferDataRecorder
	{
		public static void CreateWafer(string guid, string carrierGuid, string station, int slot, string waferId, string status = "")
		{
			string sql = string.Format("INSERT INTO \"wafer_data\"(\"guid\", \"create_time\", \"carrier_data_guid\", \"create_station\", \"wafer_id\",\"create_slot\",\"process_status\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' );", guid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), carrierGuid, station, waferId, slot + 1, status);
			DB.Insert(sql);
		}

		public static void SetProcessInfo(string guid, string processGuid)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"process_data_guid\"='{processGuid}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferMarker(string guid, string marker)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"lasermarker1\"='{marker}'  WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferMarkerByID(string guid, int id, string marker)
		{
			string sql = string.Format("UPDATE \"wafer_data\" SET \"lasermarker{2}\"='{0}'  WHERE \"guid\"='{1}';", marker, guid, id);
			DB.Insert(sql);
		}

		public static void SetPjInfo(string guid, string pjGuid)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"pj_data_guid\"='{pjGuid}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetCjInfo(string guid, string cjGuid)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"lot_data_guid\"='{cjGuid}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferLotId(string guid, string lotId)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"lot_id\"='{lotId}'  WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferStatus(string guid, string status)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"process_status\"='{status}'  WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferNotchAngle(string guid, float angle)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"notch_angle\"='{angle}'  WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferSequence(string guid, string sequence)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"sequence_name\"='{sequence}'  WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferMarkerWithScoreAndFileName(string guid, string marker, string score, string fileName, string filePath)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"lasermarker1\"='{marker}' , \"lasermarker1Score\"='{score}' , \"fileName\"='{fileName}' , \"filePath\"='{filePath}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferT7Code(string guid, string t7code)
		{
			string sql = $"UPDATE \"wafer_data\" SET  \"t7code1\"='{t7code}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferT7CodeWithScoreAndFileName(string guid, string t7code, string score, string fileName, string filePath)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"t7code1\"='{t7code}' , \"t7code1Score\"='{score}' , \"fileName\"='{fileName}' , \"filePath\"='{filePath}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void SetWaferId(string guid, string marker1, string marker2, string marker3, string t7code1, string t7code2, string t7code3)
		{
			string sql = $"UPDATE \"wafer_data\" SET \"lasermarker1\"='{marker1}', \"t7code1\"='{t7code1}' WHERE \"guid\"='{guid}';";
			DB.Insert(sql);
		}

		public static void DeleteWafer(string guid)
		{
			string sql = string.Format("UPDATE \"wafer_data\" SET \"delete_time\"='{0}' WHERE \"guid\"='{1}';", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), guid);
			DB.Insert(sql);
		}

		public static List<HistoryWaferData> QueryDBWafer(string sql)
		{
			List<HistoryWaferData> list = new List<HistoryWaferData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryWaferData historyWaferData = new HistoryWaferData();
					historyWaferData.Guid = dataSet.Tables[0].Rows[i]["guid"].ToString();
					historyWaferData.LaserMarker = dataSet.Tables[0].Rows[i]["lasermarker1"].ToString();
					historyWaferData.T7Code = dataSet.Tables[0].Rows[i]["t7code1"].ToString();
					if (!dataSet.Tables[0].Rows[i]["create_time"].Equals(DBNull.Value))
					{
						historyWaferData.CreateTime = ((DateTime)dataSet.Tables[0].Rows[i]["create_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					if (!dataSet.Tables[0].Rows[i]["delete_time"].Equals(DBNull.Value))
					{
						historyWaferData.DeleteTime = ((DateTime)dataSet.Tables[0].Rows[i]["delete_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					historyWaferData.Station = dataSet.Tables[0].Rows[i]["create_station"].ToString();
					historyWaferData.Slot = dataSet.Tables[0].Rows[i]["create_slot"].ToString();
					historyWaferData.CarrierGuid = dataSet.Tables[0].Rows[i]["carrier_data_guid"].ToString();
					historyWaferData.WaferId = dataSet.Tables[0].Rows[i]["wafer_id"].ToString();
					list.Add(historyWaferData);
				}
				list.Sort((HistoryWaferData x, HistoryWaferData y) => int.Parse(x.Slot) - int.Parse(y.Slot));
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
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
	}
}
