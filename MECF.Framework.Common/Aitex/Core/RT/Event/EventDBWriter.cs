using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Event
{
	internal class EventDBWriter
	{
		private EventDB _db = new EventDB();

		public void Initialize()
		{
			Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
			dictionary["event_id"] = typeof(int);
			dictionary["event_enum"] = typeof(string);
			dictionary["type"] = typeof(string);
			dictionary["source"] = typeof(string);
			dictionary["description"] = typeof(string);
			dictionary["level"] = typeof(string);
			dictionary["occur_time"] = typeof(DateTime);
			DB.CreateTableIfNotExisted("event_data", dictionary, addPID: true, "gid");
		}

		public void WriteEvent(EventItem ev)
		{
			string sql = string.Format("Insert into \"event_data\"(\"event_id\",\"event_enum\",\"type\",\"source\",\"description\",\"level\",\"occur_time\") values({0},'{1}','{2}','{3}','{4}','{5}','{6}')", ev.Id, ev.EventEnum, ev.Type.ToString(), ev.Source, ev.Description.Replace("'", "''"), ev.Level.ToString(), ev.OccuringTime.ToString("yyyy/MM/dd HH:mm:ss.fff"));
			DB.Insert(sql);
		}

		public List<EventItem> QueryDBEvent(string sql)
		{
			List<EventItem> list = new List<EventItem>();
			DataSet dataSet = DB.ExecuteDataset(sql);
			if (dataSet == null)
			{
				return list;
			}
			try
			{
				for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
				{
					EventItem eventItem = new EventItem();
					int result = 0;
					if (int.TryParse(dataSet.Tables[0].Rows[i]["event_id"].ToString(), out result))
					{
						eventItem.Id = result;
					}
					EventType result2 = EventType.EventUI_Notify;
					if (Enum.TryParse<EventType>(dataSet.Tables[0].Rows[i]["type"].ToString(), out result2))
					{
						eventItem.Type = result2;
					}
					eventItem.Source = dataSet.Tables[0].Rows[i]["source"].ToString();
					eventItem.EventEnum = dataSet.Tables[0].Rows[i]["event_enum"].ToString();
					eventItem.Description = dataSet.Tables[0].Rows[i]["description"].ToString();
					EventLevel result3 = EventLevel.Information;
					if (Enum.TryParse<EventLevel>(dataSet.Tables[0].Rows[i]["level"].ToString(), out result3))
					{
						eventItem.Level = result3;
					}
					if (!dataSet.Tables[0].Rows[i]["occur_time"].Equals(DBNull.Value))
					{
						eventItem.OccuringTime = (DateTime)dataSet.Tables[0].Rows[i]["occur_time"];
					}
					list.Add(eventItem);
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
