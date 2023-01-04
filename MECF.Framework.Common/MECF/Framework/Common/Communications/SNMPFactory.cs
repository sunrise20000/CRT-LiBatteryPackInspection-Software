using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Aitex.Core.RT.Log;
using SnmpSharpNet;

namespace MECF.Framework.Common.Communications
{
	public class SNMPFactory : IDisposable
	{
		public string _address;

		public string _community;

		public int _versionNo;

		public UdpTarget target;

		public AgentParameters param;

		public bool EnableLog { get; set; }

		public string Address
		{
			get
			{
				return _address;
			}
			set
			{
				_address = value;
			}
		}

		public event Action<string, string> OnErrorHappened;

		public event Action<Dictionary<string, string>> OnDataDicChanged;

		public event Action<string, string> OnDataChanged;

		public event Action<string> OnDataWriteChanged;

		public event Action<TrapInfo> OnTrapChanged;

		public SNMPFactory(string address, string community, int VersionNo, string newline = "\r", bool isAsciiMode = true)
		{
			_address = address;
			_community = community;
			_versionNo = VersionNo;
			OctetString community2 = new OctetString(community);
			param = new AgentParameters(community2);
			param.Version = (SnmpVersion)_versionNo;
			IpAddress ipAddress = new IpAddress(Address);
			target = new UdpTarget((IPAddress)ipAddress, 161, 2000, 1);
		}

		public void Dispose()
		{
			Close();
		}

		public bool Open()
		{
			if (target != null)
			{
				Close();
			}
			try
			{
				IpAddress ipAddress = new IpAddress(Address);
				target = new UdpTarget((IPAddress)ipAddress, 161, 2000, 1);
			}
			catch (Exception ex)
			{
				string message = Address + "open failed，please check configuration." + ex.Message;
				LOG.Write(message);
				return false;
			}
			return true;
		}

		public bool IsOpen()
		{
			return target != null;
		}

		public bool Close()
		{
			if (target != null)
			{
				try
				{
					target.Close();
					target.Dispose();
				}
				catch (Exception ex)
				{
					string message = Address + "close failed." + ex.Message;
					LOG.Write(message);
					return false;
				}
			}
			return true;
		}

		public bool SnmpGet(string oIds)
		{
			try
			{
				Pdu pdu = new Pdu(PduType.Get);
				pdu.VbList.Add(oIds);
				if (_versionNo == 0)
				{
					SnmpV1Packet snmpV1Packet = (SnmpV1Packet)target.Request(pdu, param);
					if (EnableLog)
					{
						LOG.Info($"Communication {Address} Send {oIds}.", isTraceOn: false);
					}
					if (snmpV1Packet != null)
					{
						Dictionary<string, string> dictionary = new Dictionary<string, string>();
						if (snmpV1Packet.Pdu.ErrorStatus != 0)
						{
							if (this.OnErrorHappened != null)
							{
								this.OnErrorHappened(snmpV1Packet.Pdu.ErrorStatus.ToString(), snmpV1Packet.Pdu.ErrorStatus.ToString());
							}
						}
						else
						{
							string text = null;
							foreach (Vb vb in snmpV1Packet.Pdu.VbList)
							{
								if (this.OnDataChanged != null)
								{
									dictionary.Add(vb.Oid.ToString(), vb.Value.ToString());
									if (EnableLog)
									{
										text = text + vb.Oid.ToString() + "(" + vb.Value.ToString() + ")\r\n";
									}
								}
							}
							if (EnableLog)
							{
								LOG.Info($"Communication {Address} Receive {text}.", isTraceOn: false);
							}
							this.OnDataDicChanged(dictionary);
						}
						return true;
					}
				}
				else if (_versionNo == 1)
				{
					SnmpV2Packet snmpV2Packet = (SnmpV2Packet)target.Request(pdu, param);
					if (EnableLog)
					{
						LOG.Info($"Communication {Address} Send {oIds}.", isTraceOn: false);
					}
					if (snmpV2Packet != null)
					{
						Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
						if (snmpV2Packet.Pdu.ErrorStatus != 0)
						{
							string text2 = null;
							if (this.OnErrorHappened != null)
							{
								this.OnErrorHappened(snmpV2Packet.Pdu.ErrorStatus.ToString(), snmpV2Packet.Pdu.ErrorStatus.ToString());
							}
							if (EnableLog)
							{
								LOG.Info($"Communication {Address} Receive {snmpV2Packet.Pdu.VbList.FirstOrDefault().Oid}({snmpV2Packet.Pdu.VbList.FirstOrDefault().Value}):ErrorStatus({snmpV2Packet.Pdu.ErrorStatus.ToString()}).", isTraceOn: false);
							}
							this.OnDataDicChanged(dictionary2);
						}
						else
						{
							string text3 = null;
							foreach (Vb vb2 in snmpV2Packet.Pdu.VbList)
							{
								if (this.OnDataChanged != null)
								{
									dictionary2.Add(vb2.Oid.ToString(), vb2.Value.ToString());
								}
								if (EnableLog)
								{
									text3 = text3 + vb2.Oid.ToString() + "(" + vb2.Value.ToString() + ")\r\n";
								}
							}
							if (EnableLog)
							{
								LOG.Info($"Communication {Address} Receive {text3}.", isTraceOn: false);
							}
							this.OnDataDicChanged(dictionary2);
						}
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				string text4 = ex.ToString();
				return false;
			}
			return false;
		}

		public bool SnmpGetNext(string oIds)
		{
			try
			{
				Pdu pdu = new Pdu(PduType.GetNext);
				pdu.VbList.Add(oIds);
				if (_versionNo == 0)
				{
					SnmpV1Packet snmpV1Packet = (SnmpV1Packet)target.Request(pdu, param);
					if (EnableLog)
					{
						LOG.Info($"Communication {Address} Send {oIds}.", isTraceOn: false);
					}
					if (snmpV1Packet != null)
					{
						Dictionary<string, string> dictionary = new Dictionary<string, string>();
						if (snmpV1Packet.Pdu.ErrorStatus != 0)
						{
							if (this.OnErrorHappened != null)
							{
								this.OnErrorHappened(snmpV1Packet.Pdu.ErrorStatus.ToString(), snmpV1Packet.Pdu.ErrorStatus.ToString());
							}
						}
						else
						{
							string text = null;
							foreach (Vb vb in snmpV1Packet.Pdu.VbList)
							{
								if (this.OnDataChanged != null)
								{
									dictionary.Add(vb.Oid.ToString(), vb.Value.ToString());
									if (EnableLog)
									{
										text = text + vb.Oid.ToString() + "(" + vb.Value.ToString() + ")\r\n";
									}
								}
							}
							if (EnableLog)
							{
								LOG.Info($"Communication {Address} Receive {text}.", isTraceOn: false);
							}
							this.OnDataDicChanged(dictionary);
						}
						return true;
					}
				}
				else if (_versionNo == 1)
				{
					SnmpV2Packet snmpV2Packet = (SnmpV2Packet)target.Request(pdu, param);
					if (EnableLog)
					{
						LOG.Info($"Communication {Address} Send {oIds}.", isTraceOn: false);
					}
					if (snmpV2Packet != null)
					{
						Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
						if (snmpV2Packet.Pdu.ErrorStatus != 0)
						{
							string text2 = null;
							if (this.OnErrorHappened != null)
							{
								this.OnErrorHappened(snmpV2Packet.Pdu.ErrorStatus.ToString(), snmpV2Packet.Pdu.ErrorStatus.ToString());
							}
							if (EnableLog)
							{
								LOG.Info($"Communication {Address} Receive {snmpV2Packet.Pdu.VbList.FirstOrDefault().Oid}({snmpV2Packet.Pdu.VbList.FirstOrDefault().Value}):ErrorStatus({snmpV2Packet.Pdu.ErrorStatus.ToString()}).", isTraceOn: false);
							}
							this.OnDataDicChanged(dictionary2);
						}
						else
						{
							string text3 = null;
							foreach (Vb vb2 in snmpV2Packet.Pdu.VbList)
							{
								if (this.OnDataChanged != null)
								{
									dictionary2.Add(vb2.Oid.ToString(), vb2.Value.ToString());
								}
								if (EnableLog)
								{
									text3 = text3 + vb2.Oid.ToString() + "(" + vb2.Value.ToString() + ")\r\n";
								}
							}
							if (EnableLog)
							{
								LOG.Info($"Communication {Address} Receive {text3}.", isTraceOn: false);
							}
							this.OnDataDicChanged(dictionary2);
						}
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				string text4 = ex.ToString();
				return false;
			}
			return false;
		}

		public bool SnmpGetList(List<string> oIds)
		{
			try
			{
				Pdu pdu = new Pdu(PduType.Get);
				foreach (string oId in oIds)
				{
					pdu.VbList.Add(oId);
				}
				if (_versionNo == 0)
				{
					SnmpV1Packet snmpV1Packet = (SnmpV1Packet)target.Request(pdu, param);
					if (snmpV1Packet != null)
					{
						Dictionary<string, string> dictionary = new Dictionary<string, string>();
						if (snmpV1Packet.Pdu.ErrorStatus != 0)
						{
							if (this.OnErrorHappened != null)
							{
								this.OnErrorHappened(snmpV1Packet.Pdu.ErrorStatus.ToString(), snmpV1Packet.Pdu.ErrorStatus.ToString());
							}
						}
						else
						{
							foreach (Vb vb in snmpV1Packet.Pdu.VbList)
							{
								dictionary.Add(vb.Oid.ToString(), vb.Value.ToString());
							}
							if (this.OnDataDicChanged != null)
							{
								this.OnDataDicChanged(dictionary);
							}
						}
						return true;
					}
				}
				else if (_versionNo == 1)
				{
					SnmpV2Packet snmpV2Packet = (SnmpV2Packet)target.Request(pdu, param);
					if (snmpV2Packet != null)
					{
						Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
						if (snmpV2Packet.Pdu.ErrorStatus != 0)
						{
							if (this.OnErrorHappened != null)
							{
								this.OnErrorHappened(snmpV2Packet.Pdu.ErrorStatus.ToString(), snmpV2Packet.Pdu.ErrorStatus.ToString());
							}
						}
						else
						{
							foreach (Vb vb2 in snmpV2Packet.Pdu.VbList)
							{
								dictionary2.Add(vb2.Oid.ToString(), vb2.Value.ToString());
							}
							if (this.OnDataDicChanged != null)
							{
								this.OnDataDicChanged(dictionary2);
							}
						}
						return true;
					}
				}
			}
			catch
			{
				return false;
			}
			return false;
		}

		public bool SnmpWalk(string oIds)
		{
			try
			{
				Oid oid = new Oid(oIds);
				Oid oid2 = (Oid)oid.Clone();
				Pdu pdu = new Pdu(PduType.GetBulk);
				pdu.NonRepeaters = 0;
				pdu.MaxRepetitions = 5;
				while (oid2 != null)
				{
					if (pdu.RequestId != 0)
					{
						pdu.RequestId++;
					}
					pdu.VbList.Clear();
					pdu.VbList.Add(oid2);
					SnmpV2Packet snmpV2Packet = (SnmpV2Packet)target.Request(pdu, param);
					if (EnableLog)
					{
						LOG.Info($"Communication {Address} Send {oIds}.", isTraceOn: false);
					}
					if (snmpV2Packet == null)
					{
						continue;
					}
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					if (snmpV2Packet.Pdu.ErrorStatus != 0)
					{
						if (this.OnErrorHappened != null)
						{
							this.OnErrorHappened(snmpV2Packet.Pdu.ErrorStatus.ToString(), snmpV2Packet.Pdu.ErrorStatus.ToString());
						}
						oid2 = null;
						if (EnableLog)
						{
							LOG.Info($"Communication {Address} Receive {snmpV2Packet.Pdu.VbList.FirstOrDefault().Oid}({snmpV2Packet.Pdu.VbList.FirstOrDefault().Value}):ErrorStatus({snmpV2Packet.Pdu.ErrorStatus.ToString()}).", isTraceOn: false);
						}
						this.OnDataDicChanged(dictionary);
						break;
					}
					string text = null;
					foreach (Vb vb in snmpV2Packet.Pdu.VbList)
					{
						if (oid.IsRootOf(vb.Oid))
						{
							if (!dictionary.ContainsKey(vb.Oid.ToString()))
							{
								dictionary.Add(vb.Oid.ToString(), vb.Value.ToString());
								if (EnableLog)
								{
									text = text + vb.Oid.ToString() + "(" + vb.Value.ToString() + ")\r\n";
								}
							}
							oid2 = vb.Oid;
						}
						else
						{
							oid2 = null;
						}
					}
					if (this.OnDataDicChanged != null)
					{
						this.OnDataDicChanged(dictionary);
					}
					if (EnableLog)
					{
						LOG.Info($"Communication {Address} Receive {text}.", isTraceOn: false);
					}
					return true;
				}
			}
			catch (Exception ex)
			{
				string text2 = ex.ToString();
				return false;
			}
			return false;
		}

		public bool SnmpSet(string oids, string type, string value)
		{
			Pdu pdu = new Pdu(PduType.Set);
			if (type == "Integer")
			{
				int val = Convert.ToInt32(value);
				pdu.VbList.Add(new Oid(oids), new Integer32(val));
			}
			else if (type == "UInteger")
			{
				uint val2 = Convert.ToUInt32(value);
				pdu.VbList.Add(new Oid(oids), new UInteger32(val2));
			}
			SnmpV2Packet snmpV2Packet;
			try
			{
				snmpV2Packet = target.Request(pdu, param) as SnmpV2Packet;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Request failed with exception: {ex.Message}");
				target.Close();
				return false;
			}
			if (snmpV2Packet == null)
			{
				if (this.OnErrorHappened != null)
				{
					this.OnErrorHappened(snmpV2Packet.Pdu.ErrorStatus.ToString(), snmpV2Packet.Pdu.ErrorStatus.ToString());
				}
				return false;
			}
			if (snmpV2Packet.Pdu.ErrorStatus != 0)
			{
				if (this.OnErrorHappened != null)
				{
					this.OnErrorHappened(snmpV2Packet.Pdu.ErrorStatus.ToString(), snmpV2Packet.Pdu.ErrorStatus.ToString());
				}
			}
			else
			{
				this.OnDataWriteChanged($"Set OID{oids}:Value{value} succeed");
			}
			return true;
		}

		public void GetTrap()
		{
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 162);
			EndPoint localEP = iPEndPoint;
			socket.Bind(localEP);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
			bool flag = true;
			int num = -1;
			while (flag)
			{
				byte[] buffer = new byte[16384];
				IPEndPoint iPEndPoint2 = new IPEndPoint(IPAddress.Any, 0);
				EndPoint remoteEP = iPEndPoint2;
				try
				{
					num = socket.ReceiveFrom(buffer, ref remoteEP);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Exception {0}", ex.Message);
					num = -1;
				}
				if (num <= 0)
				{
					continue;
				}
				TrapInfo trapInfo = new TrapInfo();
				if (SnmpPacket.GetProtocolVersion(buffer, num) == 0)
				{
					SnmpV1TrapPacket snmpV1TrapPacket = new SnmpV1TrapPacket();
					snmpV1TrapPacket.decode(buffer, num);
					foreach (Vb vb in snmpV1TrapPacket.Pdu.VbList)
					{
						trapInfo.OID = vb.Oid.ToString();
						trapInfo.ValueType = SnmpConstants.GetTypeName(vb.Value.Type);
						trapInfo.Value = vb.Value.ToString();
						if (this.OnTrapChanged != null)
						{
							this.OnTrapChanged(trapInfo);
						}
					}
					continue;
				}
				SnmpV2Packet snmpV2Packet = new SnmpV2Packet();
				snmpV2Packet.decode(buffer, num);
				if (snmpV2Packet.Pdu.Type != PduType.V2Trap)
				{
					continue;
				}
				foreach (Vb vb2 in snmpV2Packet.Pdu.VbList)
				{
					trapInfo.OID = vb2.Oid.ToString();
					trapInfo.ValueType = SnmpConstants.GetTypeName(vb2.Value.Type);
					trapInfo.Value = vb2.Value.ToString();
					if (this.OnTrapChanged != null)
					{
						this.OnTrapChanged(trapInfo);
					}
				}
			}
		}
	}
}
