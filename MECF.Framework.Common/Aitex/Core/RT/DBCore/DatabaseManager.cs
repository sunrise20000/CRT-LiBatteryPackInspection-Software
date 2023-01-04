using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.DBCore;

namespace Aitex.Core.RT.DBCore
{
	public class DatabaseManager : ICommonDB
	{
		private PeriodicJob _thread;

		private FixSizeQueue<string> _sqlQueue = new FixSizeQueue<string>(1000);

		private PostgresqlDB _db = new PostgresqlDB();

		private DatabaseCleaner _cleaner = new DatabaseCleaner();

		private string _dbName;

		public void Initialize(string connectionString, string dbName, string sqlFile)
		{
			if (string.IsNullOrEmpty(connectionString))
			{
				throw new ApplicationException("数据库连接字段未设置");
			}
			_dbName = dbName;
			PostgresqlHelper.ConnectionString = connectionString;
			if (!_db.Open(connectionString, dbName))
			{
				LOG.Error("数据库连接失败");
			}
			else
			{
				PrepareDatabaseTable(sqlFile);
			}
			_thread = new PeriodicJob(100, PeriodicRun, "DBJob", isStartNow: true);
			DB.Instance = this;
			DatabaseTable.UpgradeDataTable();
		}

		public void StartDataCleaner()
		{
			_cleaner.Initialize(_dbName);
		}

		public void Terminate()
		{
			if (_thread != null)
			{
				_thread.Stop();
				_thread = null;
			}
			_cleaner.Terminate();
			_db.Close();
		}

		private bool PeriodicRun()
		{
			if (!_db.ActiveConnection())
			{
				return true;
			}
			string obj;
			while (_sqlQueue.TryDequeue(out obj))
			{
				try
				{
					_db.ExecuteNonQuery(obj);
				}
				catch (Exception ex)
				{
					LOG.Error($"DB operation error, {ex.Message}, {obj}");
				}
			}
			return true;
		}

		public void Insert(string sql)
		{
			_sqlQueue.Enqueue(sql);
		}

		public void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey)
		{
			_db.CreateTableIfNotExisted(table, columns, addPID, primaryKey);
		}

		public void CreateTableIndexIfNotExisted(string table, string index, string sql)
		{
			_db.CreateTableIndexIfNotExisted(table, index, sql);
		}

		public void CreateTableColumn(string table, Dictionary<string, Type> columns)
		{
			_db.CreateTableColumn(table, columns);
		}

		public DataSet ExecuteDataset(string cmdText, params object[] p)
		{
			return _db.ExecuteDataset(cmdText, p);
		}

		public bool ExcuteTransAction(List<string> sql)
		{
			return _db.ExcuteTransAction(sql);
		}

		private void PrepareDatabaseTable(string sqlFile)
		{
			if (string.IsNullOrEmpty(sqlFile) || !File.Exists(sqlFile))
			{
				LOG.Info("没有更新Sql数据库，文件：" + sqlFile, isTraceOn: false);
				return;
			}
			try
			{
				using StreamReader streamReader = new StreamReader(sqlFile);
				string cmdText = streamReader.ReadToEnd();
				_db.ExecuteNonQuery(cmdText);
				LOG.Write("Database updated by sql file " + sqlFile);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public int ExecuteNonQueryWithParam(string cmdText, string[] columName, params object[] p)
		{
			return _db.ExecuteNonQueryWithParam(cmdText, columName, p);
		}
	}
}
