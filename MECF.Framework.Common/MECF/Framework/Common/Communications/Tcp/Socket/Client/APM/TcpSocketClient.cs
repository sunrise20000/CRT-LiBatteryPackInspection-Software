using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications.Tcp.Buffer;
using MECF.Framework.Common.Communications.Tcp.Socket.Client.APM.EventArgs;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Client.APM
{
	public class TcpSocketClient : IDisposable
	{
		private TcpClient _tcpClient;

		private readonly TcpSocketClientConfiguration _configuration;

		private readonly IPEndPoint _remoteEndPoint;

		private readonly IPEndPoint _localEndPoint;

		private Stream _stream;

		private ArraySegment<byte> _receiveBuffer = default(ArraySegment<byte>);

		private int _receiveBufferOffset = 0;

		private int _state;

		private const int _none = 0;

		private const int _connecting = 1;

		private const int _connected = 2;

		private const int _closed = 5;

		public TimeSpan ConnectTimeout => _configuration.ConnectTimeout;

		private bool Connected => _tcpClient != null && _tcpClient.Client.Connected;

		public IPEndPoint RemoteEndPoint => Connected ? ((IPEndPoint)_tcpClient.Client.RemoteEndPoint) : _remoteEndPoint;

		public IPEndPoint LocalEndPoint => Connected ? ((IPEndPoint)_tcpClient.Client.LocalEndPoint) : _localEndPoint;

		public TcpSocketConnectionState State => _state switch
		{
			0 => TcpSocketConnectionState.None, 
			1 => TcpSocketConnectionState.Connecting, 
			2 => TcpSocketConnectionState.Connected, 
			5 => TcpSocketConnectionState.Closed, 
			_ => TcpSocketConnectionState.Closed, 
		};

		public event EventHandler<TcpServerConnectedEventArgs> ServerConnected;

		public event EventHandler<TcpServerDisconnectedEventArgs> ServerDisconnected;

		public event EventHandler<TcpServerDataReceivedEventArgs> ServerDataReceived;

		public TcpSocketClient(IPAddress remoteAddress, int remotePort, IPAddress localAddress, int localPort, TcpSocketClientConfiguration configuration = null)
			: this(new IPEndPoint(remoteAddress, remotePort), new IPEndPoint(localAddress, localPort), configuration)
		{
		}

		public TcpSocketClient(IPAddress remoteAddress, int remotePort, IPEndPoint localEP = null, TcpSocketClientConfiguration configuration = null)
			: this(new IPEndPoint(remoteAddress, remotePort), localEP, configuration)
		{
		}

		public TcpSocketClient(IPEndPoint remoteEP, TcpSocketClientConfiguration configuration = null)
			: this(remoteEP, null, configuration)
		{
		}

		public TcpSocketClient(IPEndPoint remoteEP, IPEndPoint localEP, TcpSocketClientConfiguration configuration = null)
		{
			if (remoteEP == null)
			{
				throw new ArgumentNullException("remoteEP");
			}
			_remoteEndPoint = remoteEP;
			_localEndPoint = localEP;
			_configuration = configuration ?? new TcpSocketClientConfiguration();
			if (_configuration.BufferManager == null)
			{
				throw new InvalidProgramException("The buffer manager in configuration cannot be null.");
			}
			if (_configuration.FrameBuilder == null)
			{
				throw new InvalidProgramException("The frame handler in configuration cannot be null.");
			}
		}

		public override string ToString()
		{
			return $"RemoteEndPoint[{RemoteEndPoint}], LocalEndPoint[{LocalEndPoint}]";
		}

		public bool IsConnected()
		{
			if (Connected)
			{
				if (_tcpClient.Client.Poll(0, SelectMode.SelectWrite) && !_tcpClient.Client.Poll(0, SelectMode.SelectError))
				{
					byte[] buffer = new byte[1];
					if (_tcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
					{
						return false;
					}
					return true;
				}
				return false;
			}
			return false;
		}

		public void Connect()
		{
			int num = Interlocked.Exchange(ref _state, 1);
			if (num != 0 && num != 5)
			{
				Close(shallNotifyUserSide: false);
				throw new InvalidOperationException("This tcp socket client is in invalid state when connecting.");
			}
			Clean();
			_tcpClient = ((_localEndPoint != null) ? new TcpClient(_localEndPoint) : new TcpClient(_remoteEndPoint.Address.AddressFamily));
			SetSocketOptions();
			IAsyncResult asyncResult = _tcpClient.BeginConnect(_remoteEndPoint.Address, _remoteEndPoint.Port, null, _tcpClient);
			if (!asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout))
			{
				Close(shallNotifyUserSide: false);
				throw new TimeoutException($"Connect to [{_remoteEndPoint}] timeout [{ConnectTimeout}].");
			}
			try
			{
				_tcpClient.EndConnect(asyncResult);
			}
			catch (Exception ex)
			{
				Close(shallNotifyUserSide: false);
				throw ex;
			}
			if (Interlocked.CompareExchange(ref _state, 2, 1) != 1)
			{
				Close(shallNotifyUserSide: false);
				throw new InvalidOperationException("This tcp socket client is in invalid state when connected.");
			}
			if (_receiveBuffer == default(ArraySegment<byte>))
			{
				_receiveBuffer = _configuration.BufferManager.BorrowBuffer();
			}
			_receiveBufferOffset = 0;
			HandleTcpServerConnected();
		}

		public void Close()
		{
			Close(shallNotifyUserSide: true);
		}

		private void Close(bool shallNotifyUserSide)
		{
			if (Interlocked.Exchange(ref _state, 5) == 5)
			{
				return;
			}
			Clean();
			if (!shallNotifyUserSide)
			{
				return;
			}
			try
			{
				RaiseServerDisconnected();
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

		private void HandleTcpServerConnected()
		{
			try
			{
				_stream = NegotiateStream(_tcpClient.GetStream());
				bool flag = false;
				try
				{
					RaiseServerConnected();
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
			}
			catch (Exception ex2)
			{
				LOG.Error(ex2.Message, ex2);
				Close();
			}
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
				LOG.Write($"Error occurred when validating remote certificate: [{RemoteEndPoint}], [{sslPolicyErrors}].");
				return false;
			};
			SslStream sslStream = new SslStream(stream, leaveInnerStreamOpen: false, userCertificateValidationCallback, null, _configuration.SslEncryptionPolicy);
			IAsyncResult asyncResult = null;
			asyncResult = ((_configuration.SslClientCertificates != null && _configuration.SslClientCertificates.Count != 0) ? sslStream.BeginAuthenticateAsClient(_configuration.SslTargetHost, _configuration.SslClientCertificates, _configuration.SslEnabledProtocols, _configuration.SslCheckCertificateRevocation, null, _tcpClient) : sslStream.BeginAuthenticateAsClient(_configuration.SslTargetHost, null, _tcpClient));
			if (!asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout))
			{
				Close(shallNotifyUserSide: false);
				throw new TimeoutException($"Negotiate SSL/TSL with remote [{RemoteEndPoint}] timeout [{ConnectTimeout}].");
			}
			sslStream.EndAuthenticateAsClient(asyncResult);
			LOG.Write($"Ssl Stream: SslProtocol[{sslStream.SslProtocol}], IsServer[{sslStream.IsServer}], IsAuthenticated[{sslStream.IsAuthenticated}], IsEncrypted[{sslStream.IsEncrypted}], IsSigned[{sslStream.IsSigned}], IsMutuallyAuthenticated[{sslStream.IsMutuallyAuthenticated}], HashAlgorithm[{sslStream.HashAlgorithm}], HashStrength[{sslStream.HashStrength}], KeyExchangeAlgorithm[{sslStream.KeyExchangeAlgorithm}], KeyExchangeStrength[{sslStream.KeyExchangeStrength}], CipherAlgorithm[{sslStream.CipherAlgorithm}], CipherStrength[{sslStream.CipherStrength}].");
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
					throw;
				}
			}
		}

		private void ReceiveBuffer(int receiveCount)
		{
			int num = 0;
			SegmentBufferDeflector.ReplaceBuffer(_configuration.BufferManager, ref _receiveBuffer, ref _receiveBufferOffset, receiveCount);
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
					RaiseServerDataReceived(payload, payloadOffset, payloadCount);
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
				SegmentBufferDeflector.ShiftBuffer(_configuration.BufferManager, num, ref _receiveBuffer, ref _receiveBufferOffset);
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
				LOG.Error(ex.Message, ex);
				Close();
				return true;
			}
			return false;
		}

		private void HandleUserSideError(Exception ex)
		{
			LOG.Error($"Client [{this}] error occurred in user side [{ex.Message}].", ex);
		}

		public bool Send(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			return Send(data, 0, data.Length);
		}

		public bool Send(byte[] data, int offset, int count)
		{
			BufferValidator.ValidateBuffer(data, offset, count, "data");
			if (State != TcpSocketConnectionState.Connected)
			{
				return false;
			}
			try
			{
				_configuration.FrameBuilder.Encoder.EncodeFrame(data, offset, count, out var frameBuffer, out var frameBufferOffset, out var frameBufferLength);
				_stream.Write(frameBuffer, frameBufferOffset, frameBufferLength);
				return true;
			}
			catch (Exception ex)
			{
				if (IsSocketTimeOut(ex))
				{
					LOG.Error(ex.Message, ex);
					return false;
				}
				if (!CloseIfShould(ex))
				{
					return false;
				}
			}
			return false;
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
				throw new InvalidProgramException("This client has not connected to server.");
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
				throw new InvalidProgramException("This client has not connected to server.");
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

		private void RaiseServerConnected()
		{
			if (this.ServerConnected != null)
			{
				this.ServerConnected(this, new TcpServerConnectedEventArgs(RemoteEndPoint, LocalEndPoint));
			}
		}

		private void RaiseServerDisconnected()
		{
			if (this.ServerDisconnected != null)
			{
				this.ServerDisconnected(this, new TcpServerDisconnectedEventArgs(_remoteEndPoint, _localEndPoint));
			}
		}

		private void RaiseServerDataReceived(byte[] data, int dataOffset, int dataLength)
		{
			if (this.ServerDataReceived != null)
			{
				this.ServerDataReceived(this, new TcpServerDataReceivedEventArgs(this, data, dataOffset, dataLength));
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					Close();
				}
				catch (Exception ex)
				{
					LOG.Error(ex.Message, ex);
				}
			}
		}
	}
}
