using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using MECF.Framework.Common.DBCore;

namespace MECF.Framework.Common.DataCenter
{
	public class StatsDataManager : Singleton<StatsDataManager>
	{
		private Dictionary<string, StatsDataItem> _items = new Dictionary<string, StatsDataItem>();

		private object _locker = new object();

		public Dictionary<string, StatsDataItem> Item => _items;

		public void Initialize()
		{
			try
			{
				OP.Subscribe("System.Stats.ResetValue", delegate(string method, object[] args)
				{
					Reset((string)args[0]);
					return true;
				});
				OP.Subscribe("System.Stats.EnableAlarm", delegate(string method, object[] args)
				{
					EnableAlarm((string)args[0], (bool)args[1]);
					return true;
				});
				OP.Subscribe("System.Stats.SetAlarmValue", delegate(string method, object[] args)
				{
					SetAlarmValue((string)args[0], (int)args[1]);
					return true;
				});
				OP.Subscribe("System.Stats.ResetTotalValue", delegate(string method, object[] args)
				{
					ResetTotal((string)args[0]);
					return true;
				});
				DataTable dataTable = DataQuery.Query("select * from \"stats_data\"");
				for (int i = 0; i < dataTable.Rows.Count; i++)
				{
					StatsDataItem statsDataItem = new StatsDataItem();
					statsDataItem.Name = dataTable.Rows[i]["name"].ToString();
					statsDataItem.Description = dataTable.Rows[i]["description"].ToString();
					if (int.TryParse(dataTable.Rows[i]["value"].ToString(), out var result))
					{
						statsDataItem.Value = result;
					}
					if (int.TryParse(dataTable.Rows[i]["total"].ToString(), out var result2))
					{
						statsDataItem.Total = result2;
					}
					if (int.TryParse(dataTable.Rows[i]["alarm_value"].ToString(), out var result3))
					{
						statsDataItem.AlarmValue = result3;
					}
					if (!dataTable.Rows[i]["enable_alarm"].Equals(DBNull.Value))
					{
						statsDataItem.AlarmEnable = Convert.ToBoolean(dataTable.Rows[i]["enable_alarm"].ToString());
					}
					if (!dataTable.Rows[i]["is_visible"].Equals(DBNull.Value))
					{
						statsDataItem.IsVisible = Convert.ToBoolean(dataTable.Rows[i]["is_visible"].ToString());
					}
					if (!dataTable.Rows[i]["last_update_time"].Equals(DBNull.Value))
					{
						statsDataItem.LastUpdateTime = (DateTime)dataTable.Rows[i]["last_update_time"];
					}
					if (!dataTable.Rows[i]["last_reset_time"].Equals(DBNull.Value))
					{
						statsDataItem.LastResetTime = (DateTime)dataTable.Rows[i]["last_reset_time"];
					}
					if (!dataTable.Rows[i]["last_total_reset_time"].Equals(DBNull.Value))
					{
						statsDataItem.LastResetTotalTime = (DateTime)dataTable.Rows[i]["last_total_reset_time"];
					}
					_items[statsDataItem.Name] = statsDataItem;
				}
			}
			catch (Exception ex)
			{
				LOG.Error("init stats data manager failed", ex);
			}
		}

		private int SetAlarmValue(string name, int value)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not set " + name + " alarm value, not defined item");
					return -1;
				}
				int alarmValue = _items[name].AlarmValue;
				_items[name].AlarmValue = value;
				string sql = $"UPDATE \"stats_data\" SET \"alarm_value\"='{_items[name].AlarmValue}'WHERE \"name\"='{name}';";
				DB.Insert(sql);
				EV.PostInfoLog("System", $"{name} stats alarm value changed from {alarmValue} to {value}");
				return alarmValue;
			}
		}

		private void EnableAlarm(string name, bool enable)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not set " + name + " alarm enable, not defined item");
					return;
				}
				bool alarmEnable = _items[name].AlarmEnable;
				_items[name].AlarmEnable = enable;
				string sql = $"UPDATE \"stats_data\" SET \"enable_alarm\"='{_items[name].AlarmEnable}'WHERE \"name\"='{name}';";
				DB.Insert(sql);
				EV.PostInfoLog("System", $"{name} stats alarm enable changed from {alarmEnable} to {enable}");
			}
		}

		public void EnableVisible(string name, bool visible)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not set " + name + " alarm enable, not defined item");
					return;
				}
				bool isVisible = _items[name].IsVisible;
				_items[name].IsVisible = visible;
				string sql = $"UPDATE \"stats_data\" SET \"is_visible\"='{_items[name].IsVisible}'WHERE \"name\"='{name}';";
				DB.Insert(sql);
				EV.PostInfoLog("System", $"{name} stats visible changed from {isVisible} to {visible}");
			}
		}

		public void Terminate()
		{
		}

		public void Subscribe(string name, string description, int initialValue, int alarmValue = 0, bool alarmEnable = false, bool isVisible = true)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					_items[name] = new StatsDataItem
					{
						Description = description,
						Value = initialValue,
						Name = name,
						Total = 0
					};
					string sql = string.Format("Insert into \"stats_data\"(\"name\",\r\n                                                    \"value\",\r\n                                                    \"total\",\r\n                                                    \"description\",\r\n                                                    \"last_update_time\",\r\n                                                    \"last_reset_time\",\r\n                                                    \"last_total_reset_time\",\r\n                                                    \"is_visible\",\r\n                                                    \"enable_alarm\",\r\n                                                    \"alarm_value\"\r\n                                                    ) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", name, initialValue, initialValue, description, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), isVisible, alarmEnable, alarmValue);
					DB.Insert(sql);
				}
			}
		}

		public void SetValue(string name, int value)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not set " + name + " value, not defined item");
					return;
				}
				_items[name].Value = value;
				_items[name].LastUpdateTime = DateTime.Now;
				string sql = string.Format("UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"value\"='{1}' WHERE \"name\"='{2}';", _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), value, name);
				DB.Insert(sql);
			}
		}

		public int GetValue(string name)
		{
			lock (_locker)
			{
				if (_items.ContainsKey(name))
				{
					return _items[name].Value;
				}
			}
			LOG.Error("Can not get " + name + " value, not defined item");
			return 0;
		}

		public StatsDataItem GetItem(string name)
		{
			lock (_locker)
			{
				if (_items.ContainsKey(name))
				{
					return _items[name];
				}
			}
			LOG.Error("Can not get " + name + " value, not defined item");
			return null;
		}

		public int Increase(string name, int additionValue = 1)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not increase " + name + " value, not defined item");
					return -1;
				}
				_items[name].Value += additionValue;
				_items[name].LastUpdateTime = DateTime.Now;
				string sql = string.Format("UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"value\"='{1}' WHERE \"name\"='{2}';", _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), _items[name].Value, name);
				DB.Insert(sql);
				return _items[name].Value;
			}
		}

		public int Reset(string name)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not reset " + name + " value, not defined item");
					return -1;
				}
				int value = _items[name].Value;
				_items[name].Value = 0;
				_items[name].LastUpdateTime = DateTime.Now;
				_items[name].LastResetTime = DateTime.Now;
				string sql = string.Format("UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"last_reset_time\"='{1}',\"value\"='{2}' WHERE \"name\"='{3}';", _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), _items[name].LastResetTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), _items[name].Value, name);
				DB.Insert(sql);
				EV.PostInfoLog("System", name + " stats value reset to 0");
				return value;
			}
		}

		public void ResetTotal(string name)
		{
			lock (_locker)
			{
				if (!_items.ContainsKey(name))
				{
					LOG.Error("Can not reset " + name + " value, not defined item");
					return;
				}
				_items[name].Value = 0;
				_items[name].Total = 0;
				_items[name].LastResetTime = DateTime.Now;
				_items[name].LastUpdateTime = DateTime.Now;
				_items[name].LastResetTotalTime = DateTime.Now;
				string sql = string.Format("UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"last_reset_time\"='{1}',\"last_total_reset_time\"='{2}',\"value\"='{3}',\"total\"='{4}' WHERE \"name\"='{5}';", _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), _items[name].LastResetTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), _items[name].LastResetTotalTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), _items[name].Value, _items[name].Total, name);
				EV.PostInfoLog("System", name + " stats total value reset to 0");
				DB.Insert(sql);
			}
		}
	}
}
