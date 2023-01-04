using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using Npgsql;

namespace Aitex.Core.RT.DataCollection
{
	public class DataCollectionManager : Singleton<DataCollectionManager>, IConnection
	{
		private NpgsqlConnection _conn;

		private bool _bAlive = true;

		private Dictionary<string, Func<object>> _preSubscribedRecordedData;

		private Dictionary<string, Func<object>> _subscribedRecordedData;

		private object _lock = new object();

		private F_TRIG _connFailTrig = new F_TRIG();

		private int dataCollectionInterval;

		private IDataCollectionCallback _callback;

		private string[] _modules = new string[1] { "Data" };

		private bool _dbFailed;

		public string Address => "DB:DataCollection";

		public bool IsConnected { get; set; }

		public DataCollectionManager()
		{
			_preSubscribedRecordedData = new Dictionary<string, Func<object>>();
			_subscribedRecordedData = new Dictionary<string, Func<object>>();
			dataCollectionInterval = (SC.ContainsItem("System.DataCollectionInterval") ? SC.GetValue<int>("System.DataCollectionInterval") : 1000);
		}

		public void Initialize(IDataCollectionCallback callback)
		{
			IDataCollectionCallback callback2;
			if (callback != null)
			{
				callback2 = callback;
			}
			else
			{
				IDataCollectionCallback dataCollectionCallback = new DefaultDataCollectionCallback();
				callback2 = dataCollectionCallback;
			}
			_callback = callback2;
			Connect();
			Task.Factory.StartNew(DataRecorderThread);
		}

		public void Initialize(string[] modules, string dbName)
		{
			_callback = new DefaultDataCollectionCallback(dbName);
			if (modules != null && modules.Length != 0 && !string.IsNullOrEmpty(modules[0]))
			{
				_modules = modules;
			}
			Connect();
			Task.Factory.StartNew(DataRecorderThread);
		}

		public void Initialize(string dbName)
		{
			_callback = new DefaultDataCollectionCallback(dbName);
			Connect();
			Task.Factory.StartNew(DataRecorderThread);
		}

		public void Terminate()
		{
			_bAlive = false;
		}

		public void SubscribeData(string dataName, string alias, Func<object> dataValue)
		{
			lock (_lock)
			{
				if (!_preSubscribedRecordedData.Keys.Contains(dataName))
				{
					_preSubscribedRecordedData[dataName] = dataValue;
				}
			}
		}

		private void MonitorDataCenter()
		{
			lock (_lock)
			{
				SortedDictionary<string, Func<object>> dBRecorderList = DATA.GetDBRecorderList();
				foreach (KeyValuePair<string, Func<object>> item in dBRecorderList)
				{
					if (_subscribedRecordedData.ContainsKey(item.Key))
					{
						continue;
					}
					object obj = item.Value();
					if (obj != null)
					{
						Type type = obj.GetType();
						if (type == typeof(bool) || type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(ushort) || type == typeof(short))
						{
							_subscribedRecordedData[item.Key] = item.Value;
							_preSubscribedRecordedData[item.Key] = item.Value;
						}
					}
				}
			}
		}

		public bool Connect()
		{
			bool flag = true;
			try
			{
				if (_conn != null)
				{
					_conn.Close();
				}
				_conn = new NpgsqlConnection(PostgresqlHelper.ConnectionString);
				_conn.Open();
				_conn.ChangeDatabase(_callback.GetDBName());
				EV.PostInfoLog("DataCollection", "Connected with database");
			}
			catch (Exception ex)
			{
				if (_conn != null)
				{
					_conn.Close();
					_conn = null;
				}
				flag = false;
				EV.PostInfoLog("DataCollection", "Can not connect database");
				LOG.Write(ex);
			}
			IsConnected = flag;
			return flag;
		}

		public bool Disconnect()
		{
			try
			{
				if (_conn != null)
				{
					_conn.Close();
					_conn = null;
					EV.PostInfoLog("DataCollection", "Disconnected with database");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			IsConnected = false;
			return true;
		}

		public void Reset()
		{
			_connFailTrig.CLK = false;
		}

		private void DataRecorderThread()
		{
			while (_bAlive)
			{
				try
				{
					Thread.Sleep(dataCollectionInterval);
					_connFailTrig.CLK = IsConnected;
					if (_connFailTrig.Q)
					{
						EV.PostWarningLog("DataCollection", "Can not connect with database");
					}
					if (!_connFailTrig.M)
					{
						continue;
					}
					MonitorDataCenter();
					Dictionary<string, Dictionary<string, Func<object>>> dictionary = new Dictionary<string, Dictionary<string, Func<object>>>();
					Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
					string[] modules = _modules;
					foreach (string key in modules)
					{
						dictionary[key] = new Dictionary<string, Func<object>>();
					}
					string key2 = (_modules.Contains("System") ? "System" : (_modules.Contains("Data") ? "Data" : _modules[0]));
					if (_subscribedRecordedData.Count > 0)
					{
						lock (_lock)
						{
							foreach (string key4 in _subscribedRecordedData.Keys)
							{
								bool flag = false;
								string[] modules2 = _modules;
								foreach (string text in modules2)
								{
									if (key4.StartsWith(text + ".") || key4.StartsWith("IO." + text + "."))
									{
										dictionary[text][key4] = _subscribedRecordedData[key4];
										flag = true;
										break;
									}
								}
								if (!flag)
								{
									dictionary[key2][key4] = _subscribedRecordedData[key4];
								}
							}
							_preSubscribedRecordedData.Clear();
						}
					}
					DateTime date = DateTime.Now.Date;
					string[] modules3 = _modules;
					foreach (string text2 in modules3)
					{
						string text3 = $"{date:yyyyMMdd}.{text2}";
						UpdateTableSchema(text3, dictionary[text2]);
						string text4 = $"INSERT INTO \"{text3}\"(\"time\" ";
						foreach (string key5 in dictionary[text2].Keys)
						{
							text4 += $",\"{key5}\"";
						}
						text4 = (dictionary2[text2] = text4 + ")");
					}
					StringBuilder stringBuilder = new StringBuilder(10000);
					while (_bAlive)
					{
						Thread.Sleep((int)((double)dataCollectionInterval * 0.99));
						string value = DateTime.Now.Ticks.ToString();
						string[] modules4 = _modules;
						foreach (string key3 in modules4)
						{
							stringBuilder.Remove(0, stringBuilder.Length);
							stringBuilder.Append("Values(");
							stringBuilder.Append(value);
							foreach (string key6 in dictionary[key3].Keys)
							{
								stringBuilder.Append(",");
								object obj = dictionary[key3][key6]();
								if (obj == null)
								{
									stringBuilder.Append("0");
								}
								else if (obj is double || obj is float)
								{
									double d = Convert.ToDouble(obj);
									if (double.IsNaN(d))
									{
										d = 0.0;
									}
									stringBuilder.Append(d.ToString());
								}
								else
								{
									stringBuilder.Append(obj.ToString());
								}
							}
							stringBuilder.Append(");");
							NpgsqlCommand npgsqlCommand = new NpgsqlCommand(dictionary2[key3] + stringBuilder.ToString(), _conn);
							try
							{
								npgsqlCommand.ExecuteNonQuery();
								_dbFailed = false;
							}
							catch (Exception ex)
							{
								if (!_dbFailed)
								{
									LOG.Write(ex, "数据记录发生异常" + dictionary2[key3]);
									_dbFailed = true;
								}
							}
						}
						if (DateTime.Now.Date != date)
						{
							break;
						}
						MonitorDataCenter();
						if (_preSubscribedRecordedData.Count > 0)
						{
							break;
						}
					}
					_dbFailed = false;
				}
				catch (Exception ex2)
				{
					if (!_dbFailed)
					{
						LOG.Write(ex2, "数据库操作记录发生异常");
						_dbFailed = true;
					}
				}
			}
		}

		private string UpdateTableSchema(string tblName, Dictionary<string, Func<object>> dataItem)
		{
			string cmdText = $"select column_name from information_schema.columns where table_name = '{tblName}';";
			NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, _conn);
			NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
			string text = string.Empty;
			List<string> list = new List<string>();
			while (npgsqlDataReader.Read())
			{
				for (int i = 0; i < npgsqlDataReader.FieldCount; i++)
				{
					list.Add(npgsqlDataReader[i].ToString());
				}
			}
			npgsqlDataReader.Close();
			if (list.Count > 0)
			{
				foreach (string key in dataItem.Keys)
				{
					if (!list.Contains(key))
					{
						Type type = dataItem[key]().GetType();
						if (type == typeof(bool))
						{
							text += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tblName, key, "Boolean");
						}
						else if (type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(ushort) || type == typeof(short))
						{
							text += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tblName, key, "Real");
						}
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						NpgsqlCommand npgsqlCommand2 = new NpgsqlCommand(text, _conn);
						npgsqlCommand2.ExecuteNonQuery();
						_dbFailed = false;
					}
					catch (Exception ex)
					{
						if (!_dbFailed)
						{
							LOG.Write(ex);
							_dbFailed = true;
						}
					}
				}
			}
			else
			{
				string text2 = $"CREATE TABLE \"{tblName}\"(Time bigint NOT NULL,";
				string text3 = "";
				foreach (string key2 in dataItem.Keys)
				{
					Type type2 = dataItem[key2]().GetType();
					if (type2 == typeof(bool))
					{
						text2 += $"\"{key2}\" Boolean,\n";
					}
					else if (type2 == typeof(double) || type2 == typeof(float) || type2 == typeof(int) || type2 == typeof(ushort) || type2 == typeof(short))
					{
						text2 += $"\"{key2}\" Real,\n";
					}
				}
				text2 += $"CONSTRAINT \"{tblName}_pkey\" PRIMARY KEY (Time));";
				text2 += $"GRANT SELECT ON TABLE \"{tblName}\" TO postgres;";
				text2 += text3;
				try
				{
					NpgsqlCommand npgsqlCommand3 = new NpgsqlCommand(text2.ToString(), _conn);
					npgsqlCommand3.ExecuteNonQuery();
					_dbFailed = false;
				}
				catch (Exception ex2)
				{
					if (!_dbFailed)
					{
						LOG.Write(ex2, "创建数据库表格失败");
						_dbFailed = true;
					}
				}
			}
			return tblName;
		}
	}
}
