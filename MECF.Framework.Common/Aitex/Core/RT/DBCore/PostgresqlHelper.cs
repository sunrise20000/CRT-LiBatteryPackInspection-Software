using System;
using System.Data;
using Npgsql;

namespace Aitex.Core.RT.DBCore
{
	public class PostgresqlHelper
	{
		public static string ConnectionString { get; set; }

		public static NpgsqlConnection GetConnection()
		{
			if (string.IsNullOrWhiteSpace(ConnectionString))
			{
				throw new ArgumentNullException("PostgresqlConn");
			}
			return new NpgsqlConnection(ConnectionString);
		}

		private static void PrepareCommand(NpgsqlCommand cmd, NpgsqlConnection conn, string cmdText, params object[] p)
		{
			if (conn.State != ConnectionState.Open)
			{
				conn.Open();
			}
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

		public static DataSet ExecuteDataset(string cmdText, params object[] p)
		{
			DataSet dataSet = new DataSet();
			using (NpgsqlConnection conn = GetConnection())
			{
				using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
				PrepareCommand(npgsqlCommand, conn, cmdText, p);
				NpgsqlDataAdapter npgsqlDataAdapter = new NpgsqlDataAdapter(npgsqlCommand);
				npgsqlDataAdapter.Fill(dataSet);
			}
			return dataSet;
		}

		public static DataRow ExecuteDataRow(string cmdText, params object[] p)
		{
			DataSet dataSet = ExecuteDataset(cmdText, p);
			if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
			{
				return dataSet.Tables[0].Rows[0];
			}
			return null;
		}

		public static int ExecuteNonQuery(string cmdText, params object[] p)
		{
			using NpgsqlConnection conn = GetConnection();
			using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
			PrepareCommand(npgsqlCommand, conn, cmdText, p);
			return npgsqlCommand.ExecuteNonQuery();
		}

		public static NpgsqlDataReader ExecuteReader(string cmdText, params object[] p)
		{
			NpgsqlConnection connection = GetConnection();
			NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
			try
			{
				PrepareCommand(npgsqlCommand, connection, cmdText, p);
				return npgsqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
			}
			catch
			{
				connection.Close();
				throw;
			}
		}

		public static object ExecuteScalar(string cmdText, params object[] p)
		{
			using NpgsqlConnection conn = GetConnection();
			using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
			PrepareCommand(npgsqlCommand, conn, cmdText, p);
			return npgsqlCommand.ExecuteScalar();
		}

		public static DataSet ExecutePager(ref int recordCount, int pageIndex, int pageSize, string cmdText, string countText, params object[] p)
		{
			if (recordCount < 0)
			{
				recordCount = int.Parse(ExecuteScalar(countText, p).ToString());
			}
			DataSet dataSet = new DataSet();
			using (NpgsqlConnection conn = GetConnection())
			{
				using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
				PrepareCommand(npgsqlCommand, conn, cmdText, p);
				NpgsqlDataAdapter npgsqlDataAdapter = new NpgsqlDataAdapter(npgsqlCommand);
				npgsqlDataAdapter.Fill(dataSet, (pageIndex - 1) * pageSize, pageSize, "result");
			}
			return dataSet;
		}
	}
}
