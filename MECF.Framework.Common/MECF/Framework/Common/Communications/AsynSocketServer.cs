using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.Communications
{
	public class AsynSocketServer : IDisposable
	{
		public delegate void ErrorHandler(string args);

		public delegate void MessageHandler(string message);

		public delegate void BinaryMessageHandler(byte[] message);

		private static object _locker = new object();

		private string _ip;

		private int _port;

		private bool _isAscii;

		private static int _bufferSize = 1021;

		private Socket _clientSocket;

		private Socket _serverSocket;

		private byte[] _buffer;

		public string NewLine { get; set; }

		public bool NeedLog { get; set; } = true;


		public bool IsConnected => _serverSocket != null;

		public event ErrorHandler OnErrorHappened;

		public event MessageHandler OnDataChanged;

		public event BinaryMessageHandler OnBinaryDataChanged;

		public AsynSocketServer(string ip, int port, bool isAscii, string newline = "\r")
		{
			_ip = ip;
			_port = port;
			_isAscii = isAscii;
			_serverSocket = null;
			NewLine = newline;
		}

		~AsynSocketServer()
		{
			Dispose();
		}

		public void Start()
		{
			if (_serverSocket == null)
			{
				IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(_ip), _port);
				_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_serverSocket.Bind(localEP);
				_serverSocket.Listen(10);
				AsyncAccept(_serverSocket);
			}
		}

		private void AsyncAccept(Socket serverSocket)
		{
			serverSocket.BeginAccept(delegate(IAsyncResult asyncResult)
			{
				_clientSocket = serverSocket.EndAccept(asyncResult);
				LOG.Info($"Received client {_clientSocket.RemoteEndPoint} connect request", isTraceOn: false);
				AsyncReveive();
			}, null);
		}

		private void AsyncReveive()
		{
			_buffer = new byte[_bufferSize];
			try
			{
				_clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, null);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string text = $"TCP Socket recevice data failed：{ex.Message}";
				LOG.Error($"Communication  {_ip}:{_port:D} {text}.");
				this.OnErrorHappened(text);
			}
		}

		private void ReceiveCallback(IAsyncResult asyncResult)
		{
			int num = _clientSocket.EndReceive(asyncResult);
			if (num > 0)
			{
				if (_isAscii)
				{
					string @string = Encoding.ASCII.GetString(_buffer, 0, num);
					LOG.Info($"Client message:{@string}", isTraceOn: false);
					this.OnDataChanged(@string);
				}
				else
				{
					byte[] array = new byte[num];
					for (int i = 0; i < num; i++)
					{
						array[i] = _buffer[i];
					}
					LOG.Info("Client message: " + string.Join(" ", array) + ".", isTraceOn: false);
					this.OnBinaryDataChanged(array);
				}
			}
			_buffer = new byte[_bufferSize];
			_clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, _clientSocket);
		}

		public void Write(string sendMessage)
		{
			if (_clientSocket == null || sendMessage == string.Empty)
			{
				return;
			}
			byte[] data = new byte[1024];
			data = Encoding.ASCII.GetBytes(sendMessage);
			try
			{
				_clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, delegate(IAsyncResult asyncResult)
				{
					int num = _clientSocket.EndSend(asyncResult);
					LOG.Info($"Communication {_ip}:{_port:D} Send {data}.", isTraceOn: false);
				}, null);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string text = $"TCP连接发生错误：{ex.Message}";
				LOG.Error($"Communication  {_ip}:{_port:D} {text}.");
				this.OnErrorHappened(text);
			}
		}

		public void Write(byte[] data)
		{
			if (_clientSocket == null || data.Count() == 0)
			{
				return;
			}
			try
			{
				_clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, delegate(IAsyncResult asyncResult)
				{
					int num = _clientSocket.EndSend(asyncResult);
					LOG.Info($"Communication {_ip}:{_port:D} Send {data}.", isTraceOn: false);
				}, null);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string text = $"TCP连接发生错误：{ex.Message}";
				LOG.Error($"Communication  {_ip}:{_port:D} {text}.");
				this.OnErrorHappened(text);
			}
		}

		public void Dispose()
		{
			try
			{
				if (_serverSocket != null)
				{
					if (IsConnected)
					{
						_serverSocket.Shutdown(SocketShutdown.Both);
					}
					_serverSocket.Close();
					_serverSocket.Dispose();
					_serverSocket = null;
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string args = $"释放socket资源失败:{ex.Message}";
				this.OnErrorHappened(args);
			}
		}
	}
}
