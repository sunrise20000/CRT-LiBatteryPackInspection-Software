using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Npgsql;

namespace MECF.Framework.Common.DBCore
{
	public sealed class DatabaseCleaner
	{
		private NpgsqlConnection conn;

		private List<string> tableNames = new List<string>();

		private bool _isDataCleanEnabled = false;

		private int _daysOfRetainData = 90;

		private DateTime _dateDataKeepTo;

		private PeriodicJob _cleanThread;

		private string _dbName;

		public void Initialize(string dbName)
		{
			_dbName = dbName;
			_isDataCleanEnabled = !SC.ContainsItem("System.EnableDataClean") || SC.GetValue<bool>("System.EnableDataClean");
			if (_isDataCleanEnabled)
			{
				GetDaysOfRetainData();
			}
			_cleanThread = new PeriodicJob(86400000, MonitorCleanData, "Database cleaner", isStartNow: true);
		}

		public void Terminate()
		{
			_cleanThread.Stop();
		}

		public bool MonitorCleanData()
		{
			try
			{
				string text = null;
				string text2 = null;
				int num = 0;
				NpgsqlCommand npgsqlCommand = null;
				string[,] array = new string[5, 2]
				{
					{ "carrier_data", "load_time" },
					{ "event_data", "occur_time" },
					{ "process_data", "process_begin_time" },
					{ "wafer_data", "create_time" },
					{ "wafer_move_history", "arrive_time" }
				};
				tableNames.Clear();
				GetTableNames();
				if (tableNames.Count == 0)
				{
					return true;
				}
				conn = new NpgsqlConnection(PostgresqlHelper.ConnectionString);
				conn.Open();
				conn.ChangeDatabase(_dbName);
				DeviceTimer deviceTimer = new DeviceTimer();
				for (int i = 0; i < array.GetLength(0); i++)
				{
					text = $"select count(*) from information_schema.columns where table_schema='public' and table_name ='{array[i, 0]}' and  column_name='{array[i, 1]}'";
					npgsqlCommand = new NpgsqlCommand(text, conn);
					num = Convert.ToInt32(npgsqlCommand.ExecuteScalar());
					if (num == 1)
					{
						deviceTimer.Start(300000.0);
						text = string.Format("delete from \"{0}\" where \"{1}\" <= '{2}'", array[i, 0], array[i, 1], _dateDataKeepTo.ToString("yyyy/MM/dd HH:mm:ss.fff"));
						npgsqlCommand = new NpgsqlCommand(text, conn);
						npgsqlCommand.ExecuteNonQuery();
						double num2 = deviceTimer.GetElapseTime() / 1000.0;
						text2 = string.Format("当前日期为{0},删除目录表{1}里{2}天前的记录,用时{3}秒", DateTime.Now.ToString("D"), array[i, 0], _daysOfRetainData, num2);
						LOG.Info(text2, isTraceOn: false);
						Thread.Sleep(50);
					}
				}
				foreach (string tableName in tableNames)
				{
					deviceTimer.Start(300000.0);
					text = $"drop table \"{tableName}\"";
					npgsqlCommand = new NpgsqlCommand(text, conn);
					npgsqlCommand.ExecuteNonQuery();
					double num3 = deviceTimer.GetElapseTime() / 1000.0;
					text2 = string.Format("当前日期为{0},删除{1}天前的数据表{2},用时{3}秒", DateTime.Now.ToString("D"), _daysOfRetainData, tableName, num3);
					LOG.Info(text2, isTraceOn: false);
					Thread.Sleep(50);
				}
				conn.Close();
				conn.ClearPool();
				conn = null;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				if (conn != null)
				{
					conn.Close();
					conn.ClearPool();
				}
				conn = null;
			}
			return true;
		}

		public void GetTableNames()
		{
			try
			{
				conn = new NpgsqlConnection(PostgresqlHelper.ConnectionString);
				_dateDataKeepTo = DateTime.Now.AddDays(-_daysOfRetainData);
				string cmdText = "select tablename from pg_tables where schemaname='public' and tablename like '20%' order by tablename asc";
				conn.Open();
				conn.ChangeDatabase(_dbName);
				NpgsqlCommand npgsqlCommand = new NpgsqlCommand(cmdText, conn);
				NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
				while (npgsqlDataReader.Read())
				{
					for (int i = 0; i < npgsqlDataReader.FieldCount; i++)
					{
						string text = npgsqlDataReader[i].ToString();
						text = text.Substring(0, 8);
						if (DateTime.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture) <= _dateDataKeepTo)
						{
							tableNames.Add(npgsqlDataReader[i].ToString());
						}
					}
				}
				npgsqlDataReader.Close();
				npgsqlDataReader.Dispose();
				conn.Close();
				conn.ClearPool();
				conn = null;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				if (conn != null)
				{
					conn.Close();
					conn.ClearPool();
				}
				conn = null;
			}
		}

		public void GetDaysOfRetainData()
		{
			int num = 90;
			if (SC.ContainsItem("System.DataKeepDays"))
			{
				num = SC.GetValue<int>("System.DataKeepDays");
			}
			if (num < 10)
			{
				LOG.Warning($"database keep days should be at least 10 days.current setting {num}");
				num = 10;
			}
			if (num > 365)
			{
				LOG.Warning($"database keep days should be less than 365 days.current setting {num}");
				num = 365;
			}
			_daysOfRetainData = num;
		}
	}
}
