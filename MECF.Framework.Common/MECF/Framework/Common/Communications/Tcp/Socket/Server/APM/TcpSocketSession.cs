using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications.Tcp.Buffer;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Server.APM
{
	public sealed class TcpSocketSession
	{
		private TcpClient _tcpClient;

		private readonly TcpSocketServerConfiguration _configuration;

		private readonly ISegmentBufferManager _bufferManager;

		private readonly TcpSocketServer _server;

		private readonly string _sessionKey;

		private Stream _stream;

		private ArraySegment<byte> _receiveBuffer = default(ArraySegment<byte>);

		private int _receiveBufferOffset = 0;

		private IPEndPoint _remoteEndPoint;

		private IPEndPoint _localEndPoint;

		private int _state;

		private const int _none = 0;

		private const int _connecting = 1;

		private const int _connected = 2;

		private const int _disposed = 5;

		public string SessionKey => _sessionKey;

		public DateTime StartTime { get; private set; }

		public TimeSpan ConnectTimeout => _configuration.ConnectTimeout;

		private bool Connected => _tcpClient != null && _tcpClient.Connected;

		public IPEndPoint RemoteEndPoint => Connected ? ((IPEndPoint)_tcpClient.Client.RemoteEndPoint) : _remoteEndPoint;

		public IPEndPoint LocalEndPoint => Connected ? ((IPEndPoint)_tcpClient.Client.LocalEndPoint) : _localEndPoint;

		public System.Net.Sockets.Socket Socket => Connected ? _tcpClient.Client : null;

		public Stream Stream => _stream;

		public TcpSocketServer Server => _server;

		public TcpSocketConnectionState State => _state switch
		{
			0 => TcpSocketConnectionState.None, 
			1 => TcpSocketConnectionState.Connecting, 
			2 => TcpSocketConnectionState.Connected, 
			5 => TcpSocketConnectionState.Closed, 
			_ => TcpSocketConnectionState.Closed, 
		};

		public TcpSocketSession(TcpClient tcpClient, TcpSocketServerConfiguration configuration, ISegmentBufferManager bufferManager, TcpSocketServer server)
		{
			if (tcpClient == null)
			{
				throw new ArgumentNullException("tcpClient");
			}
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			if (bufferManager == null)
			{
				throw new ArgumentNullException("bufferManager");
			}
			if (server == null)
			{
				throw new ArgumentNullException("server");
			}
			_tcpClient = tcpClient;
			_configuration = configuration;
			_bufferManager = bufferManager;
			_server = server;
			_sessionKey = Guid.NewGuid().ToString();
			StartTime = DateTime.UtcNow;
			SetSocketOptions();
			_remoteEndPoint = RemoteEndPoint;
			_localEndPoint = LocalEndPoint;
		}

		public override string ToString()
		{
			return $"SessionKey[{SessionKey}], RemoteEndPoint[{RemoteEndPoint}], LocalEndPoint[{LocalEndPoint}]";
		}

		internal void Start()
		{
			switch (Interlocked.CompareExchange(ref _state, 1, 0))
			{
			case 5:
				throw new ObjectDisposedException("This tcp socket session has been disposed when connecting.");
			default:
				throw new InvalidOperationException("This tcp socket session is in invalid state when connecting.");
			case 0:
				try
				{
					_stream = NegotiateStream(_tcpClient.GetStream());
					if (_receiveBuffer == default(ArraySegment<byte>))
					{
						_receiveBuffer = _bufferManager.BorrowBuffer();
					}
					_receiveBufferOffset = 0;
					if (Interlocked.CompareExchange(ref _state, 2, 1) != 1)
					{
						Close();
						throw new ObjectDisposedException("This tcp socket session has been disposed after connected.");
					}
					bool flag = false;
					try
					{
						_server.RaiseClientConnected(this);
					}
					catch (Exception ex)
					{
						flag = true;
						HandleUserSideError(ex);
					}
					if (!flag)
					{
						ContinueReadBuffer();
					}
					else
					{
						Close();
					}
					break;
				}
				catch (Exception ex2)
				{
					LOG.Error(ex2.Message, ex2);
					Close();
					break;
				}
			}
		}

		public void Close()
		{
			if (Interlocked.Exchange(ref _state, 5) == 5)
			{
				return;
			}
			Clean();
			try
			{
				_server.RaiseClientDisconnected(this);
			}
			catch (Exception ex)
			{
				HandleUserSideError(ex);
			}
		}

		private void Clean()
		{
			try
			{
				try
				{
					if (_stream != null)
					{
						_stream.Dispose();
					}
				}
				catch
				{
				}
				try
				{
					if (_tcpClient != null)
					{
						_tcpClient.Close();
					}
				}
				catch
				{
				}
			}
			catch
			{
			}
			finally
			{
				_stream = null;
				_tcpClient = null;
			}
			if (_receiveBuffer != default(ArraySegment<byte>))
			{
				_configuration.BufferManager.ReturnBuffer(_receiveBuffer);
			}
			_receiveBuffer = default(ArraySegment<byte>);
			_receiveBufferOffset = 0;
		}

		private void SetSocketOptions()
		{
			_tcpClient.ReceiveBufferSize = _configuration.ReceiveBufferSize;
			_tcpClient.SendBufferSize = _configuration.SendBufferSize;
			_tcpClient.ReceiveTimeout = (int)_configuration.ReceiveTimeout.TotalMilliseconds;
			_tcpClient.SendTimeout = (int)_configuration.SendTimeout.TotalMilliseconds;
			_tcpClient.NoDelay = _configuration.NoDelay;
			_tcpClient.LingerState = _configuration.LingerState;
			if (_configuration.KeepAlive)
			{
				_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, (int)_configuration.KeepAliveInterval.TotalMilliseconds);
			}
			_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _configuration.ReuseAddress);
		}

		private Stream NegotiateStream(Stream stream)
		{
			if (!_configuration.SslEnabled)
			{
				return stream;
			}
			RemoteCertificateValidationCallback userCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
			{
				if (sslPolicyErrors == SslPolicyErrors.None)
				{
					return true;
				}
				if (_configuration.SslPolicyErrorsBypassed)
				{
					return true;
				}
				LOG.Error($"Session [{this}] error occurred when validating remote certificate: [{RemoteEndPoint}], [{sslPolicyErrors}].");
				return false;
			};
			SslStream sslStream = new SslStream(stream, leaveInnerStreamOpen: false, userCertificateValidationCallback, null, _configuration.SslEncryptionPolicy);
			IAsyncResult asyncResult = null;
			asyncResult = (_configuration.SslClientCertificateRequired ? sslStream.BeginAuthenticateAsServer(_configuration.SslServerCertificate, _configuration.SslClientCertificateRequired, _configuration.SslEnabledProtocols, _configuration.SslCheckCertificateRevocation, null, _tcpClient) : sslStream.BeginAuthenticateAsServer(_configuration.SslServerCertificate, null, _tcpClient));
			if (!asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout))
			{
				Close();
				throw new TimeoutException($"Negotiate SSL/TSL with remote [{RemoteEndPoint}] timeout [{ConnectTimeout}].");
			}
			LOG.Info($"Ssl Stream: SslProtocol[{sslStream.SslProtocol}], IsServer[{sslStream.IsServer}], IsAuthenticated[{sslStream.IsAuthenticated}], IsEncrypted[{sslStream.IsEncrypted}], IsSigned[{sslStream.IsSigned}], IsMutuallyAuthenticated[{sslStream.IsMutuallyAuthenticated}], HashAlgorithm[{sslStream.HashAlgorithm}], HashStrength[{sslStream.HashStrength}], KeyExchangeAlgorithm[{sslStream.KeyExchangeAlgorithm}], KeyExchangeStrength[{sslStream.KeyExchangeStrength}], CipherAlgorithm[{sslStream.CipherAlgorithm}], CipherStrength[{sslStream.CipherStrength}].", isTraceOn: false);
			return sslStream;
		}

		private void ContinueReadBuffer()
		{
			try
			{
				_stream.BeginRead(_receiveBuffer.Array, _receiveBuffer.Offset + _receiveBufferOffset, _receiveBuffer.Count - _receiveBufferOffset, HandleDataReceived, _stream);
			}
			catch (Exception ex)
			{
				if (!CloseIfShould(ex))
				{
					throw;
				}
			}
		}

		private void HandleDataReceived(IAsyncResult ar)
		{
			if (State != TcpSocketConnectionState.Connected)
			{
				return;
			}
			try
			{
				if (_stream != null)
				{
					int num = 0;
					try
					{
						num = _stream.EndRead(ar);
					}
					catch (Exception)
					{
						num = 0;
					}
					if (num == 0)
					{
						Close();
						return;
					}
					ReceiveBuffer(num);
					ContinueReadBuffer();
				}
			}
			catch (Exception ex2)
			{
				if (!CloseIfShould(ex2))
				{
					LOG.Write(ex2);
				}
			}
		}

		private void ReceiveBuffer(int receiveCount)
		{
			int num = 0;
			SegmentBufferDeflector.ReplaceBuffer(_bufferManager, ref _receiveBuffer, ref _receiveBufferOffset, receiveCount);
			while (true)
			{
				int frameLength = 0;
				byte[] payload = null;
				int payloadOffset = 0;
				int payloadCount = 0;
				if (!_configuration.FrameBuilder.Decoder.TryDecodeFrame(_receiveBuffer.Array, _receiveBuffer.Offset + num, _receiveBufferOffset - num, out frameLength, out payload, out payloadOffset, out payloadCount))
				{
					break;
				}
				try
				{
					_server.RaiseClientDataReceived(this, payload, payloadOffset, payloadCount);
				}
				catch (Exception ex)
				{
					HandleUserSideError(ex);
				}
				finally
				{
					num += frameLength;
				}
			}
			try
			{
				SegmentBufferDeflector.ShiftBuffer(_bufferManager, num, ref _receiveBuffer, ref _receiveBufferOffset);
			}
			catch (ArgumentOutOfRangeException)
			{
			}
		}

		private bool IsSocketTimeOut(Exception ex)
		{
			return ex is IOException && ex.InnerException != null && ex.InnerException is SocketException && (ex.InnerException as SocketException).SocketErrorCode == SocketError.TimedOut;
		}

		private bool CloseIfShould(Exception ex)
		{
			if (ex is ObjectDisposedException || ex is InvalidOperationException || ex is SocketException || ex is IOException || ex is NullReferenceException)
			{
				if (ex is SocketException)
				{
					LOG.Error($"Session [{this}] exception occurred, [{ex.Message}].", ex);
				}
				Close();
				return true;
			}
			return false;
		}

		private void HandleUserSideError(Exception ex)
		{
			LOG.Error($"Session [{this}] error occurred in user side [{ex.Message}].", ex);
		}

		public void Send(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			Send(data, 0, data.Length);
		}

		public void Send(byte[] data, int offset, int count)
		{
			BufferValidator.ValidateBuffer(data, offset, count, "data");
			if (State != TcpSocketConnectionState.Connected)
			{
				throw new InvalidProgramException("This session has been closed.");
			}
			try
			{
				_configuration.FrameBuilder.Encoder.EncodeFrame(data, offset, count, out var frameBuffer, out var frameBufferOffset, out var frameBufferLength);
				_stream.Write(frameBuffer, frameBufferOffset, frameBufferLength);
			}
			catch (Exception ex)
			{
				if (IsSocketTimeOut(ex))
				{
					LOG.Error(ex.Message, ex);
				}
				else if (!CloseIfShould(ex))
				{
					throw;
				}
			}
		}

		public void BeginSend(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			BeginSend(data, 0, data.Length);
		}

		public void BeginSend(byte[] data, int offset, int count)
		{
			BufferValidator.ValidateBuffer(data, offset, count, "data");
			if (State != TcpSocketConnectionState.Connected)
			{
				throw new InvalidProgramException("This session has been closed.");
			}
			try
			{
				_configuration.FrameBuilder.Encoder.EncodeFrame(data, offset, count, out var frameBuffer, out var frameBufferOffset, out var frameBufferLength);
				_stream.BeginWrite(frameBuffer, frameBufferOffset, frameBufferLength, HandleDataWritten, _stream);
			}
			catch (Exception ex)
			{
				if (IsSocketTimeOut(ex))
				{
					LOG.Error(ex.Message, ex);
				}
				else if (!CloseIfShould(ex))
				{
					throw;
				}
			}
		}

		private void HandleDataWritten(IAsyncResult ar)
		{
			try
			{
				_stream.EndWrite(ar);
			}
			catch (Exception ex)
			{
				if (!CloseIfShould(ex))
				{
					throw;
				}
			}
		}

		public IAsyncResult BeginSend(byte[] data, AsyncCallback callback, object state)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			return BeginSend(data, 0, data.Length, callback, state);
		}

		public IAsyncResult BeginSend(byte[] data, int offset, int count, AsyncCallback callback, object state)
		{
			BufferValidator.ValidateBuffer(data, offset, count, "data");
			if (State != TcpSocketConnectionState.Connected)
			{
				throw new InvalidProgramException("This session has been closed.");
			}
			try
			{
				_configuration.FrameBuilder.Encoder.EncodeFrame(data, offset, count, out var frameBuffer, out var frameBufferOffset, out var frameBufferLength);
				return _stream.BeginWrite(frameBuffer, frameBufferOffset, frameBufferLength, callback, state);
			}
			catch (Exception ex)
			{
				if (IsSocketTimeOut(ex))
				{
					LOG.Error(ex.Message, ex);
				}
				else if (!CloseIfShould(ex))
				{
					throw;
				}
				throw;
			}
		}

		public void EndSend(IAsyncResult asyncResult)
		{
			HandleDataWritten(asyncResult);
		}
	}
}
