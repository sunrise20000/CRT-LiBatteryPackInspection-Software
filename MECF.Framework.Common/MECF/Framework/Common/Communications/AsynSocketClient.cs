using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Communications
{
	public class AsynSocketClient : IDisposable
	{
		public delegate void ErrorHandler(TCPErrorEventArgs args);

		public delegate void MessageHandler(string message);

		public delegate void BinaryMessageHandler(byte[] message);

		public class ClientStateObject
		{
			public Socket workSocket = null;

			public static int BufferSize = 256;

			public byte[] buffer = new byte[BufferSize];

			public StringBuilder sb = new StringBuilder();

			public ClientStateObject(int bufferSize = 256)
			{
				BufferSize = bufferSize;
				buffer = new byte[bufferSize];
			}
		}

		private static object _locker = new object();

		private Socket _socket;

		private string _ip;

		private int _port;

		private string _address;

		private int _bufferSize = 256;

		private bool _isEndConnect = false;

		private bool _isAsciiMode;

		public string NewLine { get; set; }

		public bool NeedLog { get; set; } = true;


		public bool IsConnected => _socket != null && _socket.Connected && _isEndConnect;

		public bool IsHstConnected => _socket != null && IsSocketConnected(_socket);

		public event ErrorHandler OnErrorHappened;

		public event MessageHandler OnDataChanged;

		public event BinaryMessageHandler OnBinaryDataChanged;

		private bool IsSocketConnected(Socket client)
		{
			try
			{
				byte[] buffer = new byte[1];
				int num = client.Send(buffer);
				if (num == 1)
				{
					return true;
				}
				return false;
			}
			catch (SocketException ex)
			{
				LOG.Write(ex.Message);
				return false;
			}
		}

		public AsynSocketClient(string address, bool isAsciiMode, string newline = "\r")
		{
			_socket = null;
			NewLine = newline;
			_address = address;
			_isAsciiMode = isAsciiMode;
		}

		public AsynSocketClient(string address, int bufferSize, string newline = "\r")
		{
			_socket = null;
			NewLine = newline;
			_address = address;
			_bufferSize = bufferSize;
		}

		~AsynSocketClient()
		{
			Dispose();
		}

		public void Connect()
		{
			try
			{
				_ip = _address.Split(':')[0];
				_port = int.Parse(_address.Split(':')[1]);
				IPAddress address = IPAddress.Parse(_ip);
				IPEndPoint remoteEP = new IPEndPoint(address, _port);
				lock (_locker)
				{
					_isEndConnect = false;
					Dispose();
					if (NeedLog)
					{
						LOG.Info($"Start new socket of {_address}.", isTraceOn: false);
					}
					if (_socket == null)
					{
						_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					}
					_socket.BeginConnect(remoteEP, ConnectCallback, _socket);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				throw new Exception(ex.ToString());
			}
		}

		public bool Poll()
		{
			try
			{
				return _socket.Poll(1, SelectMode.SelectRead);
			}
			catch (Exception)
			{
				return true;
			}
		}

		private void ConnectCallback(IAsyncResult ar)
		{
			try
			{
				if (NeedLog)
				{
					LOG.Info($"ConnectCallback {_address}", isTraceOn: false);
				}
				Socket socket = (Socket)ar.AsyncState;
				if (socket.Connected)
				{
					socket.EndConnect(ar);
					_isEndConnect = true;
					if (NeedLog)
					{
						LOG.Info($"EndConnect", isTraceOn: false);
					}
					EV.PostMessage(ModuleName.Robot.ToString(), EventEnum.TCPConnSucess, _ip, _port.ToString());
					Receive(_socket);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string message = $"Communication  {_ip}:{_port:D} {ex}.";
				LOG.Error(message);
				Thread.Sleep(1000);
				Connect();
			}
		}

		private void Receive(Socket client)
		{
			try
			{
				ClientStateObject clientStateObject = new ClientStateObject(_bufferSize);
				clientStateObject.workSocket = client;
				client.BeginReceive(clientStateObject.buffer, 0, ClientStateObject.BufferSize, SocketFlags.None, ReceiveCallback, clientStateObject);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string text = $"TCP连接发生错误：{ex.Message}";
				LOG.Error($"Communication  {_ip}:{_port:D} {text}.");
				this.OnErrorHappened(new TCPErrorEventArgs(text));
			}
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				if (!IsConnected)
				{
					return;
				}
				ClientStateObject clientStateObject = (ClientStateObject)ar.AsyncState;
				Socket workSocket = clientStateObject.workSocket;
				if (workSocket == null || !workSocket.Connected)
				{
					return;
				}
				int num = workSocket.EndReceive(ar);
				if (num <= 0)
				{
					return;
				}
				clientStateObject.sb.Append(Encoding.ASCII.GetString(clientStateObject.buffer, 0, num));
				string @string = Encoding.ASCII.GetString(clientStateObject.buffer, 0, num);
				if (!_isAsciiMode)
				{
					byte[] array = new byte[num];
					for (int i = 0; i < num; i++)
					{
						array[i] = clientStateObject.buffer[i];
					}
					if (NeedLog)
					{
						LOG.Info(string.Format("Communication {0}:{1:D} receive {2}.", _ip, _port, string.Join(" ", Array.ConvertAll(array, (byte x) => x.ToString("X2")))), isTraceOn: false);
						LOG.Info($"Communication {_ip}:{_port:D} receive {Encoding.ASCII.GetString(array)} in ASCII.", isTraceOn: false);
					}
					this.OnBinaryDataChanged(array);
					clientStateObject.sb.Clear();
				}
				else if (clientStateObject.sb.Length > NewLine.Length && clientStateObject.sb.ToString().Substring(clientStateObject.sb.Length - NewLine.Length).Equals(NewLine))
				{
					string text = clientStateObject.sb.ToString();
					if (NeedLog)
					{
						LOG.Info($"Communication {_ip}:{_port:D} receive {text.TrimEnd('\n').TrimEnd('\r')}.", isTraceOn: false);
						LOG.Info(string.Format("Communication {0}:{1:D} receive {2}. in BIN", _ip, _port, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(text), (byte x) => x.ToString("X2")))), isTraceOn: false);
					}
					this.OnDataChanged(clientStateObject.sb.ToString());
					clientStateObject.sb.Clear();
				}
				workSocket.BeginReceive(clientStateObject.buffer, 0, ClientStateObject.BufferSize, SocketFlags.None, ReceiveCallback, clientStateObject);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string text2 = $"TCP Socket recevice data failed：{ex.Message}";
				LOG.Error($"Communication  {_ip}:{_port:D} {text2}.");
				this.OnErrorHappened(new TCPErrorEventArgs(text2));
			}
		}

		public bool Write(string data)
		{
			try
			{
				lock (_locker)
				{
					byte[] bytes = Encoding.ASCII.GetBytes(data);
					_socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, _socket);
					if (NeedLog)
					{
						LOG.Info($"Communication {_ip}:{_port:D} Send {data}.", isTraceOn: false);
						string arg = string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(data), (byte x) => x.ToString("X2")));
						LOG.Info($"Communication {_ip}:{_port:D} Send {arg} in Bin.", isTraceOn: false);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				LOG.Info($"Communication {_ip}:{_port:D} Send {data}. failed", isTraceOn: false);
				string reason = $"Send command failed:{ex.Message}";
				this.OnErrorHappened(new TCPErrorEventArgs(reason));
			}
			return false;
		}

		public bool Write(byte[] byteData)
		{
			try
			{
				lock (_locker)
				{
					_socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, SendCallback, _socket);
					if (NeedLog)
					{
						string arg = string.Join(" ", Array.ConvertAll(byteData, (byte x) => x.ToString("X2")));
						LOG.Info($"Communication {_ip}:{_port:D} Send {arg}.", isTraceOn: false);
						LOG.Info($"Communication {_ip}:{_port:D} Send {Encoding.ASCII.GetString(byteData)} in ASCII.", isTraceOn: false);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string arg2 = string.Join(" ", Array.ConvertAll(byteData, (byte x) => x.ToString("X2")));
				LOG.Info($"Communication {_ip}:{_port:D} Send {arg2}. failed", isTraceOn: false);
				string reason = $"Send command failed:{ex.Message}";
				this.OnErrorHappened(new TCPErrorEventArgs(reason));
			}
			return false;
		}

		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket socket = (Socket)ar.AsyncState;
				int num = socket.EndSend(ar);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string reason = $"Send command failed:{ex.Message}";
				this.OnErrorHappened(new TCPErrorEventArgs(reason));
			}
		}

		public void Dispose()
		{
			try
			{
				if (_socket != null)
				{
					if (NeedLog)
					{
						LOG.Info($"Dispose current socket of {_address}", isTraceOn: false);
					}
					if (IsConnected)
					{
						_socket.Shutdown(SocketShutdown.Both);
					}
					_socket.Close();
					_socket.Dispose();
					_socket = null;
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string reason = $"释放socket资源失败:{ex.Message}";
				this.OnErrorHappened(new TCPErrorEventArgs(reason));
			}
		}

		public void Close()
		{
			try
			{
				if (_socket != null)
				{
					if (NeedLog)
					{
						LOG.Info($"Close current socket of {_address}", isTraceOn: false);
					}
					if (IsConnected)
					{
						_socket.Shutdown(SocketShutdown.Both);
					}
					_socket.Close();
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				string reason = $"Close socket资源失败:{ex.Message}";
				this.OnErrorHappened(new TCPErrorEventArgs(reason));
			}
		}
	}
}
