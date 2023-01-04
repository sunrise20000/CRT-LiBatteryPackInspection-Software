using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Server.APM
{
	public class TcpSocketServer
	{
		private TcpListener _listener;

		private readonly ConcurrentDictionary<string, TcpSocketSession> _sessions = new ConcurrentDictionary<string, TcpSocketSession>();

		private readonly TcpSocketServerConfiguration _configuration;

		private readonly object _opsLock = new object();

		private bool _isListening = false;

		public IPEndPoint ListenedEndPoint { get; set; }

		public bool IsListening => _isListening;

		public int SessionCount => _sessions.Count;

		public event EventHandler<TcpClientConnectedEventArgs> ClientConnected;

		public event EventHandler<TcpClientDisconnectedEventArgs> ClientDisconnected;

		public event EventHandler<TcpClientDataReceivedEventArgs> ClientDataReceived;

		public TcpSocketServer(int listenedPort, TcpSocketServerConfiguration configuration = null)
			: this(IPAddress.Any, listenedPort, configuration)
		{
		}

		public TcpSocketServer(IPAddress listenedAddress, int listenedPort, TcpSocketServerConfiguration configuration = null)
			: this(new IPEndPoint(listenedAddress, listenedPort), configuration)
		{
		}

		public TcpSocketServer(IPEndPoint listenedEndPoint, TcpSocketServerConfiguration configuration = null)
		{
			if (listenedEndPoint == null)
			{
				throw new ArgumentNullException("listenedEndPoint");
			}
			ListenedEndPoint = listenedEndPoint;
			_configuration = configuration ?? new TcpSocketServerConfiguration();
			if (_configuration.BufferManager == null)
			{
				throw new InvalidProgramException("The buffer manager in configuration cannot be null.");
			}
			if (_configuration.FrameBuilder == null)
			{
				throw new InvalidProgramException("The frame handler in configuration cannot be null.");
			}
		}

		public void Listen()
		{
			lock (_opsLock)
			{
				if (!_isListening)
				{
					_listener = new TcpListener(ListenedEndPoint);
					_isListening = true;
					_listener.Start(_configuration.PendingConnectionBacklog);
					ContinueAcceptSession(_listener);
				}
			}
		}

		public void Shutdown()
		{
			lock (_opsLock)
			{
				if (!_isListening)
				{
					return;
				}
				try
				{
					_isListening = false;
					_listener.Stop();
					foreach (TcpSocketSession value in _sessions.Values)
					{
						CloseSession(value);
					}
					_sessions.Clear();
					_listener = null;
				}
				catch (Exception ex)
				{
					if (!ShouldThrow(ex))
					{
						LOG.Error(ex.Message, ex);
						return;
					}
					throw;
				}
			}
		}

		public bool Pending()
		{
			lock (_opsLock)
			{
				if (!_isListening)
				{
					throw new InvalidOperationException("The server is not listening.");
				}
				return _listener.Pending();
			}
		}

		private void SetSocketOptions()
		{
			_listener.AllowNatTraversal(_configuration.AllowNatTraversal);
			_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _configuration.ReuseAddress);
		}

		private void ContinueAcceptSession(TcpListener listener)
		{
			try
			{
				listener.BeginAcceptTcpClient(HandleTcpClientAccepted, listener);
			}
			catch (Exception ex)
			{
				if (!ShouldThrow(ex))
				{
					LOG.Error(ex.Message, ex);
					return;
				}
				throw;
			}
		}

		private void HandleTcpClientAccepted(IAsyncResult ar)
		{
			if (!_isListening)
			{
				return;
			}
			try
			{
				TcpListener tcpListener = (TcpListener)ar.AsyncState;
				TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
				if (!tcpClient.Connected)
				{
					return;
				}
				TcpSocketSession tcpSocketSession = new TcpSocketSession(tcpClient, _configuration, _configuration.BufferManager, this);
				bool flag = false;
				try
				{
					_sessions.AddOrUpdate(tcpSocketSession.SessionKey, tcpSocketSession, (string n, TcpSocketSession o) => o);
					tcpSocketSession.Start();
					flag = true;
				}
				catch (Exception ex)
				{
					LOG.Error(ex.Message, ex);
				}
				if (flag)
				{
					ContinueAcceptSession(tcpListener);
				}
				else
				{
					CloseSession(tcpSocketSession);
				}
			}
			catch (Exception ex2)
			{
				if (!ShouldThrow(ex2))
				{
					LOG.Error(ex2.Message, ex2);
					return;
				}
				throw;
			}
		}

		private void CloseSession(TcpSocketSession session)
		{
			_sessions.TryRemove(session.SessionKey, out var _);
			session?.Close();
		}

		private bool ShouldThrow(Exception ex)
		{
			if (ex is ObjectDisposedException || ex is InvalidOperationException || ex is SocketException || ex is IOException)
			{
				return false;
			}
			return false;
		}

		private void GuardRunning()
		{
			if (!_isListening)
			{
				throw new InvalidProgramException("This tcp server has not been started yet.");
			}
		}

		public void SendTo(string sessionKey, byte[] data)
		{
			GuardRunning();
			if (string.IsNullOrEmpty(sessionKey))
			{
				throw new ArgumentNullException("sessionKey");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			SendTo(sessionKey, data, 0, data.Length);
		}

		public void SendTo(string sessionKey, byte[] data, int offset, int count)
		{
			GuardRunning();
			if (string.IsNullOrEmpty(sessionKey))
			{
				throw new ArgumentNullException("sessionKey");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(sessionKey, out value))
			{
				value.Send(data, offset, count);
			}
			else
			{
				LOG.Warning($"Cannot find session [{sessionKey}].");
			}
		}

		public void SendTo(TcpSocketSession session, byte[] data)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			SendTo(session, data, 0, data.Length);
		}

		public void SendTo(TcpSocketSession session, byte[] data, int offset, int count)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(session.SessionKey, out value))
			{
				session.Send(data, offset, count);
			}
			else
			{
				LOG.Warning($"Cannot find session [{session}].");
			}
		}

		public void BeginSendTo(string sessionKey, byte[] data)
		{
			GuardRunning();
			if (string.IsNullOrEmpty(sessionKey))
			{
				throw new ArgumentNullException("sessionKey");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			BeginSendTo(sessionKey, data, 0, data.Length);
		}

		public void BeginSendTo(string sessionKey, byte[] data, int offset, int count)
		{
			GuardRunning();
			if (string.IsNullOrEmpty(sessionKey))
			{
				throw new ArgumentNullException("sessionKey");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(sessionKey, out value))
			{
				value.BeginSend(data, offset, count);
			}
			else
			{
				LOG.Warning($"Cannot find session [{sessionKey}].");
			}
		}

		public void BeginSendTo(TcpSocketSession session, byte[] data)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			BeginSendTo(session, data, 0, data.Length);
		}

		public void BeginSendTo(TcpSocketSession session, byte[] data, int offset, int count)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(session.SessionKey, out value))
			{
				session.BeginSend(data, offset, count);
			}
			else
			{
				LOG.Warning($"Cannot find session [{session}].");
			}
		}

		public IAsyncResult BeginSendTo(string sessionKey, byte[] data, AsyncCallback callback, object state)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			return BeginSendTo(sessionKey, data, 0, data.Length, callback, state);
		}

		public IAsyncResult BeginSendTo(string sessionKey, byte[] data, int offset, int count, AsyncCallback callback, object state)
		{
			GuardRunning();
			if (string.IsNullOrEmpty(sessionKey))
			{
				throw new ArgumentNullException("sessionKey");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(sessionKey, out value))
			{
				return value.BeginSend(data, offset, count, callback, state);
			}
			LOG.Warning($"Cannot find session [{sessionKey}].");
			return null;
		}

		public IAsyncResult BeginSendTo(TcpSocketSession session, byte[] data, AsyncCallback callback, object state)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			return BeginSendTo(session, data, 0, data.Length, callback, state);
		}

		public IAsyncResult BeginSendTo(TcpSocketSession session, byte[] data, int offset, int count, AsyncCallback callback, object state)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(session.SessionKey, out value))
			{
				return session.BeginSend(data, offset, count, callback, state);
			}
			LOG.Warning($"Cannot find session [{session}].");
			return null;
		}

		public void EndSendTo(string sessionKey, IAsyncResult asyncResult)
		{
			GuardRunning();
			if (string.IsNullOrEmpty(sessionKey))
			{
				throw new ArgumentNullException("sessionKey");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(sessionKey, out value))
			{
				value.EndSend(asyncResult);
			}
		}

		public void EndSendTo(TcpSocketSession session, IAsyncResult asyncResult)
		{
			GuardRunning();
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(session.SessionKey, out value))
			{
				session.EndSend(asyncResult);
			}
		}

		public void Broadcast(byte[] data)
		{
			GuardRunning();
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			Broadcast(data, 0, data.Length);
		}

		public void Broadcast(byte[] data, int offset, int count)
		{
			GuardRunning();
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			foreach (TcpSocketSession value in _sessions.Values)
			{
				value.Send(data, offset, count);
			}
		}

		public void BeginBroadcast(byte[] data)
		{
			GuardRunning();
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			BeginBroadcast(data, 0, data.Length);
		}

		public void BeginBroadcast(byte[] data, int offset, int count)
		{
			GuardRunning();
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			foreach (TcpSocketSession value in _sessions.Values)
			{
				value.BeginSend(data, offset, count);
			}
		}

		public bool HasSession(string sessionKey)
		{
			return _sessions.ContainsKey(sessionKey);
		}

		public TcpSocketSession GetSession(string sessionKey)
		{
			TcpSocketSession value = null;
			_sessions.TryGetValue(sessionKey, out value);
			return value;
		}

		public void CloseSession(string sessionKey)
		{
			TcpSocketSession value = null;
			if (_sessions.TryGetValue(sessionKey, out value))
			{
				value.Close();
			}
		}

		internal void RaiseClientConnected(TcpSocketSession session)
		{
			try
			{
				if (this.ClientConnected != null)
				{
					this.ClientConnected(this, new TcpClientConnectedEventArgs(session));
				}
			}
			catch (Exception ex)
			{
				HandleUserSideError(session, ex);
			}
		}

		internal void RaiseClientDisconnected(TcpSocketSession session)
		{
			try
			{
				if (this.ClientDisconnected != null)
				{
					this.ClientDisconnected(this, new TcpClientDisconnectedEventArgs(session));
				}
			}
			catch (Exception ex)
			{
				HandleUserSideError(session, ex);
			}
			finally
			{
				_sessions.TryRemove(session.SessionKey, out var _);
			}
		}

		internal void RaiseClientDataReceived(TcpSocketSession session, byte[] data, int dataOffset, int dataLength)
		{
			try
			{
				if (this.ClientDataReceived != null)
				{
					this.ClientDataReceived(this, new TcpClientDataReceivedEventArgs(session, data, dataOffset, dataLength));
				}
			}
			catch (Exception ex)
			{
				HandleUserSideError(session, ex);
			}
		}

		private void HandleUserSideError(TcpSocketSession session, Exception ex)
		{
			LOG.Error($"Session [{session}] error occurred in user side [{ex.Message}].", ex);
		}
	}
}
