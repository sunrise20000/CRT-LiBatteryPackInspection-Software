using System;
using System.IO.Ports;
using System.Text;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.Communications
{
	public class AsyncSerialPort : IDisposable
	{
		private static object _locker = new object();

		protected SerialPort _port;

		private string _buff = "";

		private bool _isAsciiMode;

		private bool _isLineBased;

		public string PortName
		{
			get
			{
				return _port.PortName;
			}
			set
			{
				_port.PortName = value;
			}
		}

		public bool EnableLog { get; set; }

		public bool IsAsciiMode
		{
			get
			{
				return _isAsciiMode;
			}
			set
			{
				_isAsciiMode = value;
			}
		}

		public event Action<string> OnErrorHappened;

		public event Action<string> OnDataChanged;

		public event Action<byte[]> OnBinaryDataChanged;

		public AsyncSerialPort(string name, int baudRate, int dataBits, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline = "\r", bool isAsciiMode = true)
		{
			_isAsciiMode = isAsciiMode;
			_isLineBased = !string.IsNullOrEmpty(newline);
			_port = new SerialPort();
			_port.PortName = name;
			_port.BaudRate = baudRate;
			_port.DataBits = dataBits;
			_port.Parity = parity;
			_port.StopBits = stopBits;
			_port.RtsEnable = false;
			_port.DtrEnable = false;
			_port.ReadTimeout = 1000;
			_port.WriteTimeout = 1000;
			if (!string.IsNullOrEmpty(newline))
			{
				_port.NewLine = newline;
			}
			_port.Handshake = Handshake.None;
			_port.DataReceived += DataReceived;
			_port.ErrorReceived += ErrorReceived;
		}

		public void Dispose()
		{
			Close();
		}

		public bool Open()
		{
			if (_port.IsOpen)
			{
				Close();
			}
			try
			{
				_port.Open();
				_port.DiscardInBuffer();
				_port.DiscardOutBuffer();
				_buff = "";
			}
			catch (Exception ex)
			{
				string message = _port.PortName + " port open failed，please check configuration." + ex.Message;
				LOG.Write(message);
				return false;
			}
			return true;
		}

		public bool IsOpen()
		{
			return _port.IsOpen;
		}

		public bool Close()
		{
			if (_port.IsOpen)
			{
				try
				{
					_port.Close();
				}
				catch (Exception ex)
				{
					string reason = _port.PortName + " port close failed." + ex.Message;
					ProcessError(reason);
					return false;
				}
			}
			return true;
		}

		public bool Write(string msg)
		{
			try
			{
				lock (_locker)
				{
					if (_port.IsOpen)
					{
						_port.Write(msg);
						if (EnableLog)
						{
							LOG.Info($"Communication {_port.PortName} Send {msg} succeeded.", isTraceOn: false);
							LOG.Info(string.Format("Communication {0} Send {1} succeeded.", _port.PortName, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(msg), (byte x) => x.ToString("X2")))), isTraceOn: false);
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				string text = $"Communication {_port.PortName} Send {msg} failed. {ex.Message}.";
				LOG.Info(text, isTraceOn: false);
				ProcessError(text);
				return false;
			}
		}

		public bool Write(byte[] msg)
		{
			try
			{
				lock (_locker)
				{
					if (_port.IsOpen)
					{
						_port.Write(msg, 0, msg.Length);
						if (EnableLog)
						{
							LOG.Info(string.Format("Communication {0} Send {1} succeeded.", _port.PortName, string.Join(" ", Array.ConvertAll(msg, (byte x) => x.ToString("X2")))), isTraceOn: false);
							LOG.Info($"Communication {_port.PortName} Send {Encoding.ASCII.GetString(msg)} succeeded.", isTraceOn: false);
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				string text = string.Format("Communication {0} Send {1} failed. {2}.", _port.PortName, string.Join(" ", Array.ConvertAll(msg, (byte x) => x.ToString("X2"))), ex.Message);
				LOG.Info(text, isTraceOn: false);
				ProcessError(text);
				return false;
			}
		}

		public void DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (!_port.IsOpen)
			{
				LOG.Write("discard " + _port.PortName + " received data, but port not open");
			}
			else if (_isAsciiMode)
			{
				AsciiDataReceived();
			}
			else
			{
				BinaryDataReceived();
			}
		}

		private void AsciiDataReceived()
		{
			string text = _port.ReadExisting();
			if (_isLineBased)
			{
				_buff += text;
				int num = _buff.LastIndexOf(_port.NewLine, StringComparison.Ordinal);
				if (num <= 0)
				{
					return;
				}
				num += _port.NewLine.Length;
				string text2 = _buff.Substring(0, num);
				_buff = _buff.Substring(num);
				if (EnableLog)
				{
					LOG.Info($"Communication {_port.PortName} Receive {text2}.", isTraceOn: false);
					LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(text2), (byte x) => x.ToString("X2")))), isTraceOn: false);
				}
				if (this.OnDataChanged != null)
				{
					this.OnDataChanged(text2);
				}
				return;
			}
			string text3 = text;
			if (EnableLog)
			{
				LOG.Info($"Communication {_port.PortName} Receive {text3}.", isTraceOn: false);
				LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(text3), (byte x) => x.ToString("X2")))), isTraceOn: false);
			}
			if (this.OnDataChanged != null)
			{
				this.OnDataChanged(text3);
			}
		}

		public void BinaryDataReceived()
		{
			byte[] array = new byte[_port.BytesToRead];
			int num = _port.Read(array, 0, array.Length);
			if (num == 0)
			{
				return;
			}
			byte[] array2 = new byte[num];
			Buffer.BlockCopy(array, 0, array2, 0, num);
			if (EnableLog)
			{
				StringBuilder str = new StringBuilder(512);
				Array.ForEach(array2, delegate(byte x)
				{
					str.Append(x.ToString("X2") + " ");
				});
				LOG.Info($"Communication {_port.PortName} Receive {str}.", isTraceOn: false);
				LOG.Info($"Communication {_port.PortName} Receive {Encoding.ASCII.GetString(array2)}.", isTraceOn: false);
			}
			if (this.OnBinaryDataChanged != null)
			{
				this.OnBinaryDataChanged(array2);
			}
		}

		private void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
		{
			string text = $"Communication {_port.PortName} {e.EventType.ToString()}.";
			LOG.Error(text);
			ProcessError(text);
		}

		public void ClearPortBuffer()
		{
			_port.DiscardInBuffer();
			_port.DiscardOutBuffer();
		}

		private void ProcessError(string reason)
		{
			if (this.OnErrorHappened != null)
			{
				this.OnErrorHappened(reason);
			}
		}
	}
}
