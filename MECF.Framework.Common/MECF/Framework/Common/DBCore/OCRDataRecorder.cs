using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
	public class OCRDataRecorder
	{
		public static void OcrReadComplete(string guid, string waferid, string sourcelp, string sourcecarrier, string sourceslot, string ocrno, string ocrjob, bool readresult, string lasermark, string ocrscore, string readperiod)
		{
			string sql = string.Format("INSERT INTO \"ocr_data\"(\"guid\", \"wafer_id\", \"read_time\" , \"source_lp\", \"source_carrier\", \"source_slot\", \"ocr_no\", \"ocr_job\", \"read_result\" , \"lasermark\", \"ocr_score\", \"read_period\")VALUES ('{0}', '{1}', '{2}','{3}','{4}', '{5}', '{6}','{7}', '{8}', '{9}','{10}','{11}');", guid, waferid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), sourcelp, sourcecarrier, sourceslot, ocrno, ocrjob, readresult ? "1" : "0", lasermark, ocrscore, readperiod);
			DB.Insert(sql);
		}

		public static List<HistoryStatisticsOCRData> QueryDBOCRStatistics(string sql)
		{
			List<HistoryStatisticsOCRData> result = new List<HistoryStatisticsOCRData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return result;
				}
				List<Tuple<DateTime, bool>> list = new List<Tuple<DateTime, bool>>();
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					list.Add(new Tuple<DateTime, bool>(DateTime.Parse(dataSet.Tables[0].Rows[i]["read_time"].ToString()), Convert.ToBoolean(int.Parse(dataSet.Tables[0].Rows[i]["read_result"].ToString()))));
				}
				result = (from time in list
					group time by time.Item1.Date into x
					select new HistoryStatisticsOCRData
					{
						Date = x.Key.Date.ToString("yyyy-MM-dd"),
						Totaltimes = x.Count().ToString(),
						Successfueltimes = x.Where((Tuple<DateTime, bool> data) => data.Item2).Count().ToString(),
						Failuretimes = x.Where((Tuple<DateTime, bool> data) => !data.Item2).Count().ToString()
					}).ToList();
				return result;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return result;
		}

		public static List<HistoryOCRData> QueryDBOCRHistory(string sql)
		{
			List<HistoryOCRData> list = new List<HistoryOCRData>();
			try
			{
				DataSet dataSet = DB.ExecuteDataset(sql);
				if (dataSet == null)
				{
					return list;
				}
				List<Tuple<DateTime, bool>> list2 = new List<Tuple<DateTime, bool>>();
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					HistoryOCRData historyOCRData = new HistoryOCRData();
					historyOCRData.Guid = dataSet.Tables[0].Rows[i]["guid"].ToString();
					historyOCRData.wafer_id = dataSet.Tables[0].Rows[i]["wafer_id"].ToString();
					if (!dataSet.Tables[0].Rows[i]["read_time"].Equals(DBNull.Value))
					{
						historyOCRData.read_time = ((DateTime)dataSet.Tables[0].Rows[i]["read_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
					}
					historyOCRData.source_lp = dataSet.Tables[0].Rows[i]["source_lp"].ToString();
					historyOCRData.source_carrier = dataSet.Tables[0].Rows[i]["source_carrier"].ToString();
					historyOCRData.source_slot = dataSet.Tables[0].Rows[i]["source_slot"].ToString();
					historyOCRData.ocr_no = dataSet.Tables[0].Rows[i]["ocr_no"].ToString();
					historyOCRData.ocr_job = dataSet.Tables[0].Rows[i]["ocr_job"].ToString();
					historyOCRData.read_result = dataSet.Tables[0].Rows[i]["read_result"].ToString();
					historyOCRData.lasermark = dataSet.Tables[0].Rows[i]["lasermark"].ToString();
					historyOCRData.ocr_score = dataSet.Tables[0].Rows[i]["ocr_score"].ToString();
					historyOCRData.read_period = dataSet.Tables[0].Rows[i]["read_period"].ToString();
					list.Add(historyOCRData);
				}
				return list;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return list;
		}
	}
	public class OcrDataRecorder
	{
		public static void RecordOcrReadResult(string guid, string waferid, int souceplp, string sourcecarrier, int sourceslot, int ocrno, string ocrjob, bool readresult, string lasermark, string ocrscore, string readperiod)
		{
			string sql = string.Format("INSERT INTO \"ocr_data\"(\"guid\", \"wafer_id\", \"read_time\", \"source_lp\", \"source_carrier\",\"source_slot\",\"ocr_no\" ,\"ocr_job\",\"read_result\",\"lasermark\" ,\"ocr_score\" ,\"read_period\")VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}','{6}', '{7}', '{8}', '{9}', '{10}', '{11}' );", guid, waferid, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), souceplp.ToString(), sourcecarrier, sourceslot.ToString(), ocrno.ToString(), ocrjob, readresult, lasermark, ocrscore, readperiod);
			DB.Insert(sql);
		}
	}
}
