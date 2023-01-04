using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Aitex.Core.Utilities
{
	public static class Network
	{
		private const string IpPrefix = "192.18.";

		private static string _localIP;

		public static string LocalIP
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_localIP))
				{
					string hostName = Dns.GetHostName();
					IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
					IPAddress[] addressList = hostEntry.AddressList;
					Func<IPAddress, bool> predicateIPV4 = (IPAddress ip) => ip.AddressFamily == AddressFamily.InterNetwork;
					Func<IPAddress, bool> predicate = (IPAddress ip) => predicateIPV4(ip) && ip.ToString().StartsWith("192.18.");
					_localIP = (addressList.Any(predicate) ? addressList.First(predicate).ToString() : (addressList.Any(predicateIPV4) ? addressList.First(predicateIPV4).ToString() : ((addressList.Length != 0) ? addressList[0] : new IPAddress(0L)).ToString()));
				}
				return _localIP;
			}
		}
	}
}
