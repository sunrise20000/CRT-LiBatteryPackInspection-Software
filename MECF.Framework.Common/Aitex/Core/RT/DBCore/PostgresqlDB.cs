using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;
using Npgsql;

namespace Aitex.Core.RT.DBCore
{
	public class PostgresqlDB
	{
		private NpgsqlConnection _conn;

		private string _connectionString;

		private string _dbName;

		private Retry _retryConnection = new Retry();

		private bool _dbFailed;

		public bool Open(string connectionString, string dbName)
		{
			_connectionString = connectionString;
			_dbName = dbName;
			bool result = true;
			try
			{
				if (_conn != null)
				{
					_conn.Close();
				}
				_conn = new NpgsqlConnection(connectionString);
				_conn.Open();
				if (!string.IsNullOrEmpty(dbName))
				{
					CreateDBIfNotExisted(dbName);
				}
				_dbFailed = false;
			}
			catch (Exception ex)
			{
				if (_conn != null)
				{
					_conn.Close();
					_conn = null;
				}
				result = false;
				if (!_dbFailed)
				{
					LOG.Write(ex);
					_dbFailed = true;
				}
			}
			_retryConnection.Result = result;
			return result;
		}

		private bool Open()
		{
			return Open(_connectionString, _dbName);
		}

		private void PrepareCommand(NpgsqlCommand cmd, NpgsqlConnection conn, string cmdText, params object[] p)
		{
			cmd.Parameters.Clear();
			cmd.Connection = conn;
			cmd.CommandText = cmdText;
			cmd.CommandType = CommandType.Text;
			if (p != null)
			{
				foreach (object value in p)
				{
					cmd.Parameters.AddWithValue(string.Empty, value);
				}
			}
		}

		private void PrepareCommand(NpgsqlCommand cmd, NpgsqlConnection conn, string cmdText, string[] columnsName, params object[] p)
		{
			cmd.Parameters.Clear();
			cmd.Connection = conn;
			cmd.CommandText = cmdText;
			cmd.CommandType = CommandType.Text;
			if (p != null && columnsName != null && p.Length != 0 && columnsName.Length == p.Length)
			{
				for (int i = 0; i < p.Length; i++)
				{
					cmd.Parameters.AddWithValue(columnsName[i], p[i]);
				}
			}
		}

		public int ExecuteNonQuery(string cmdText, params object[] p)
		{
			try
			{
				using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
				PrepareCommand(npgsqlCommand, _conn, cmdText, p);
				return npgsqlCommand.ExecuteNonQuery();
			}
			catch
			{
				Close();
				throw;
			}
		}

		public int ExecuteNonQueryWithParam(string cmdText, string[] columnsName, params object[] p)
		{
			try
			{
				DataSet dataSet = new DataSet();
				using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString))
				{
					npgsqlConnection.Open();
					npgsqlConnection.ChangeDatabase(_dbName);
					using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
					try
					{
						PrepareCommand(npgsqlCommand, npgsqlConnection, cmdText, columnsName, p);
						return npgsqlCommand.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						LOG.Error("执行查询出错，" + cmdText, ex);
					}
				}
				return 0;
			}
			catch
			{
				Close();
				throw;
			}
		}

		public DataSet ExecuteDataset(string cmdText, params object[] p)
		{
			try
			{
				DataSet dataSet = new DataSet();
				using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString))
				{
					npgsqlConnection.Open();
					npgsqlConnection.ChangeDatabase(_dbName);
					using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
					try
					{
						PrepareCommand(npgsqlCommand, npgsqlConnection, cmdText, p);
						NpgsqlDataAdapter npgsqlDataAdapter = new NpgsqlDataAdapter(npgsqlCommand);
						npgsqlDataAdapter.Fill(dataSet);
					}
					catch (Exception ex)
					{
						LOG.Error("执行查询出错，" + cmdText, ex);
					}
				}
				return dataSet;
			}
			catch (Exception ex2)
			{
				LOG.Error("执行查询出错，" + cmdText, ex2);
			}
			return null;
		}

		public NpgsqlDataReader ExecuteReader(string cmdText, params object[] p)
		{
			try
			{
				using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
				PrepareCommand(npgsqlCommand, _conn, cmdText, p);
				return npgsqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
			}
			catch
			{
				Close();
				throw;
			}
		}

		public bool ExcuteTransAction(List<string> lstStr)
		{
			try
			{
				DataSet dataSet = new DataSet();
				using NpgsqlConnection npgsqlConnection = new NpgsqlConnection(_connectionString);
				npgsqlConnection.Open();
				npgsqlConnection.ChangeDatabase(_dbName);
				NpgsqlTransaction npgsqlTransaction = npgsqlConnection.BeginTransaction();
				using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
				try
				{
					npgsqlCommand.Transaction = npgsqlTransaction;
					npgsqlCommand.Connection = npgsqlConnection;
					npgsqlCommand.CommandType = CommandType.Text;
					for (int i = 0; i < lstStr.Count; i++)
					{
						npgsqlCommand.Parameters.Clear();
						npgsqlCommand.CommandText = lstStr[i];
						npgsqlCommand.ExecuteNonQuery();
					}
					npgsqlTransaction.Commit();
				}
				catch (Exception ex)
				{
					npgsqlTransaction.Rollback();
					LOG.Error("执行查询出错，", ex);
					return false;
				}
			}
			catch (Exception ex2)
			{
				LOG.Error("执行查询出错，", ex2);
				return false;
			}
			return true;
		}

		public bool ActiveConnection()
		{
			if (_conn != null && _conn.State == ConnectionState.Open)
			{
				return true;
			}
			return Open();
		}

		public void Close()
		{
			try
			{
				if (_conn != null)
				{
					_conn.Close();
				}
				_conn = null;
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

		public void CreateDBIfNotExisted(string db)
		{
			NpgsqlDataReader npgsqlDataReader = ExecuteReader($"select datname from pg_catalog.pg_database where datname='{db}'");
			if (!npgsqlDataReader.HasRows)
			{
				string cmdText = $"\r\n                                    CREATE DATABASE {db}\r\n                                    WITH OWNER = postgres\r\n                                   ENCODING = 'UTF8'\r\n                                   TABLESPACE = pg_default\r\n                                   CONNECTION LIMIT = -1";
				ExecuteNonQuery(cmdText);
			}
			try
			{
				_conn.ChangeDatabase(db);
			}
			catch
			{
				_conn.Close();
				throw;
			}
		}

		public void CreateTableIndexIfNotExisted(string table, string index, string sql)
		{
			NpgsqlDataReader npgsqlDataReader = ExecuteReader("select* from pg_indexes where tablename='" + table + "' and indexname = '" + index + "'");
			if (!npgsqlDataReader.HasRows)
			{
				ExecuteNonQuery(sql);
			}
		}

		public void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey)
		{
			NpgsqlDataReader npgsqlDataReader = ExecuteReader($"select column_name from information_schema.columns where table_name = '{table}'");
			if (!npgsqlDataReader.HasRows)
			{
				string text = (addPID ? " \"PID\" serial NOT NULL," : "");
				foreach (KeyValuePair<string, Type> column in columns)
				{
					if (column.Value == typeof(int) || column.Value == typeof(ushort) || column.Value == typeof(short))
					{
						text += $"\"{column.Key}\" integer,";
					}
					else if (column.Value == typeof(double) || column.Value == typeof(float))
					{
						text += $"\"{column.Key}\" real,";
					}
					else if (column.Value == typeof(string))
					{
						text += $"\"{column.Key}\" text,";
					}
					else if (column.Value == typeof(DateTime))
					{
						text += $"\"{column.Key}\" timestamp without time zone,";
					}
					else if (column.Value == typeof(bool))
					{
						text += $"\"{column.Key}\" boolean,";
					}
					else if (column.Value == typeof(byte[]))
					{
						text += $"\"{column.Key}\" bytea,";
					}
				}
				if (string.IsNullOrEmpty(primaryKey))
				{
					if (text.LastIndexOf(',') == text.Length - 1)
					{
						text = text.Remove(text.Length - 1);
					}
				}
				else
				{
					text += $"CONSTRAINT \"{table}_pkey\" PRIMARY KEY (\"{primaryKey}\" )";
				}
				ExecuteNonQuery($"CREATE TABLE \"{table}\"({text})WITH ( OIDS=FALSE );");
			}
			else
			{
				CreateTableColumn(table, columns);
			}
		}

		public void CreateTableColumn(string tableName, Dictionary<string, Type> columns)
		{
			try
			{
				string cmdText = $"select column_name from information_schema.columns where table_name = '{tableName}';";
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
					foreach (KeyValuePair<string, Type> column in columns)
					{
						if (!list.Contains(column.Key))
						{
							text = ((!(column.Value == typeof(bool))) ? ((!(column.Value == typeof(double)) && !(column.Value == typeof(float))) ? ((!(column.Value == typeof(DateTime))) ? ((!(column.Value == typeof(int)) && !(column.Value == typeof(ushort)) && !(column.Value == typeof(short))) ? (text + string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "text")) : (text + string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "integer"))) : (text + string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "timestamp without time zone"))) : (text + string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "real"))) : (text + string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "Boolean")));
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
				_dbFailed = true;
			}
			catch (Exception ex2)
			{
				if (!_dbFailed)
				{
					LOG.Write(ex2);
					_dbFailed = true;
				}
			}
		}
	}
}
