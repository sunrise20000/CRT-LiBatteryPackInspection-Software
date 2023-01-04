using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MECF.Framework.Common.Communications.Tcp.Buffer;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Server.APM
{
	public sealed class TcpSocketServerConfiguration
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

		public int PendingConnectionBacklog { get; set; }

		public bool AllowNatTraversal { get; set; }

		public bool SslEnabled { get; set; }

		public X509Certificate2 SslServerCertificate { get; set; }

		public EncryptionPolicy SslEncryptionPolicy { get; set; }

		public SslProtocols SslEnabledProtocols { get; set; }

		public bool SslClientCertificateRequired { get; set; }

		public bool SslCheckCertificateRevocation { get; set; }

		public bool SslPolicyErrorsBypassed { get; set; }

		public TimeSpan ConnectTimeout { get; set; }

		public IFrameBuilder FrameBuilder { get; set; }

		public TcpSocketServerConfiguration()
			: this(new SegmentBufferManager(1024, 8192, 1, allowedToCreateMemory: true))
		{
		}

		public TcpSocketServerConfiguration(ISegmentBufferManager bufferManager)
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
			PendingConnectionBacklog = 200;
			AllowNatTraversal = true;
			SslEnabled = false;
			SslServerCertificate = null;
			SslEncryptionPolicy = EncryptionPolicy.RequireEncryption;
			SslEnabledProtocols = SslProtocols.Default;
			SslClientCertificateRequired = true;
			SslCheckCertificateRevocation = false;
			SslPolicyErrorsBypassed = false;
			ConnectTimeout = TimeSpan.FromSeconds(15.0);
			FrameBuilder = new LengthPrefixedFrameBuilder();
		}
	}
}
