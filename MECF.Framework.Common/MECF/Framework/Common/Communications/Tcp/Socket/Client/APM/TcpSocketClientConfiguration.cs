using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MECF.Framework.Common.Communications.Tcp.Buffer;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Client.APM
{
	public sealed class TcpSocketClientConfiguration
	{
		public ISegmentBufferManager BufferManager { get; set; }

		public int ReceiveBufferSize { get; set; }

		public int SendBufferSize { get; set; }

		public TimeSpan ReceiveTimeout { get; set; }

		public TimeSpan SendTimeout { get; set; }

		public bool NoDelay { get; set; }

		public LingerOption LingerState { get; set; }

		public bool KeepAlive { get; set; }

		public TimeSpan KeepAliveInterval { get; set; }

		public bool ReuseAddress { get; set; }

		public bool SslEnabled { get; set; }

		public string SslTargetHost { get; set; }

		public X509CertificateCollection SslClientCertificates { get; set; }

		public EncryptionPolicy SslEncryptionPolicy { get; set; }

		public SslProtocols SslEnabledProtocols { get; set; }

		public bool SslCheckCertificateRevocation { get; set; }

		public bool SslPolicyErrorsBypassed { get; set; }

		public TimeSpan ConnectTimeout { get; set; }

		public IFrameBuilder FrameBuilder { get; set; }

		public TcpSocketClientConfiguration()
			: this(new SegmentBufferManager(100, 8192, 1, allowedToCreateMemory: true))
		{
		}

		public TcpSocketClientConfiguration(ISegmentBufferManager bufferManager)
		{
			BufferManager = bufferManager;
			ReceiveBufferSize = 8192;
			SendBufferSize = 8192;
			ReceiveTimeout = TimeSpan.Zero;
			SendTimeout = TimeSpan.Zero;
			NoDelay = true;
			LingerState = new LingerOption(enable: false, 0);
			KeepAlive = false;
			KeepAliveInterval = TimeSpan.FromSeconds(5.0);
			ReuseAddress = false;
			SslEnabled = false;
			SslTargetHost = null;
			SslClientCertificates = new X509CertificateCollection();
			SslEncryptionPolicy = EncryptionPolicy.RequireEncryption;
			SslEnabledProtocols = SslProtocols.Default;
			SslCheckCertificateRevocation = false;
			SslPolicyErrorsBypassed = false;
			ConnectTimeout = TimeSpan.FromSeconds(15.0);
			FrameBuilder = new LineBasedFrameBuilder(LineDelimiter.CRLF);
		}
	}
}
