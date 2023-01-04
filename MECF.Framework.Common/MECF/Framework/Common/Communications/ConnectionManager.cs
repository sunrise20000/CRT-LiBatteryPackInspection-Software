using System.Collections.Generic;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Communications
{
	public class ConnectionManager : Singleton<ConnectionManager>
	{
		private Dictionary<string, IConnection> _dicConnections = new Dictionary<string, IConnection>();

		private List<NotifiableConnectionItem> _connections = new List<NotifiableConnectionItem>();

		public List<NotifiableConnectionItem> ConnectionList
		{
			get
			{
				foreach (NotifiableConnectionItem connection in _connections)
				{
					connection.IsConnected = _dicConnections[connection.Name].IsConnected;
				}
				return _connections;
			}
		}

		public void Initialize()
		{
			OP.Subscribe("Connection.Connect", delegate(string cmd, object[] args)
			{
				Connect((string)args[0]);
				return true;
			});
			OP.Subscribe("Connection.Disconnect", delegate(string cmd, object[] args)
			{
				Disconnect((string)args[0]);
				return true;
			});
			DATA.Subscribe("Connection.List", () => ConnectionList);
		}

		public void Terminate()
		{
		}

		public void Subscribe(string name, IConnection conn)
		{
			if (!string.IsNullOrEmpty(name) && conn != null)
			{
				_connections.Add(new NotifiableConnectionItem
				{
					Address = conn.Address,
					Name = name
				});
				_dicConnections[name] = conn;
			}
		}

		public void Connect(string name)
		{
			if (_dicConnections.ContainsKey(name) && _dicConnections[name] != null)
			{
				_dicConnections[name].Connect();
			}
		}

		public void Disconnect(string name)
		{
			if (_dicConnections.ContainsKey(name) && _dicConnections[name] != null)
			{
				_dicConnections[name].Disconnect();
			}
		}
	}
}
