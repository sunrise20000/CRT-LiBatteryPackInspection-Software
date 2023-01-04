using System.Timers;
using Aitex.Core.RT.Event;
using Aitex.Core.Utilities;

namespace Aitex.Core.Account
{
	public static class Authorization
	{
		private static Timer _timer;

		private static AuthorizationStatusEnum _status;

		public static string Module { get; set; }

		public static AuthorizationStatusEnum Status => _status;

		public static string AuthorizedAccount { get; private set; }

		public static string AuthorizedIP { get; private set; }

		public static string AuthorizingAccount { get; private set; }

		public static string AuthorizingIP { get; private set; }

		static Authorization()
		{
			_timer = new Timer();
			_status = AuthorizationStatusEnum.NoAuthorization;
			Module = "System";
			AuthorizedAccount = string.Empty;
			AuthorizedIP = string.Empty;
			AuthorizingAccount = string.Empty;
			AuthorizingIP = string.Empty;
			_timer = new Timer(180000.0);
			_timer.AutoReset = false;
			_timer.Elapsed += Timer_Elapsed;
		}

		private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (_status == AuthorizationStatusEnum.Authorizing)
			{
				Grant(isGranted: true);
			}
		}

		public static bool IsAuthorizedAccount(string accountId, string ip)
		{
			if (string.IsNullOrWhiteSpace(AuthorizedAccount))
			{
				EV.PostMessage(Module, EventEnum.AccountWithoutAuthorization, accountId);
				return false;
			}
			if (ip == Network.LocalIP && AuthorizedIP == Network.LocalIP)
			{
				return true;
			}
			if (AuthorizedAccount != accountId)
			{
				EV.PostMessage(Module, EventEnum.AccountWithoutAuthorization, accountId);
				return false;
			}
			return true;
		}

		private static bool CanAutoAuthorize(string accountId, string ip)
		{
			return string.IsNullOrWhiteSpace(AuthorizedAccount) || ip == Network.LocalIP || accountId == AuthorizedAccount;
		}

		public static void Request(string accountId, string ip)
		{
			EV.PostMessage(Module, EventEnum.OperationAuthorization, $"{accountId} 在申请操控权");
			if (CanAutoAuthorize(accountId, ip))
			{
				AuthorizedAccount = accountId;
				AuthorizedIP = ip;
				EV.PostMessage(Module, EventEnum.OperationAuthorization, $"{AuthorizedAccount} 获得操控权");
				_status = AuthorizationStatusEnum.Granted;
				Update();
			}
			else
			{
				AuthorizingAccount = accountId;
				AuthorizingIP = ip;
				_status = AuthorizationStatusEnum.Authorizing;
				Update();
				_timer.Start();
			}
		}

		public static void Abort()
		{
			_timer.Stop();
			_status = AuthorizationStatusEnum.NoAuthorization;
			AuthorizingAccount = string.Empty;
			AuthorizingIP = string.Empty;
			Update();
		}

		public static void Grant(bool isGranted)
		{
			_timer.Stop();
			if (_status != AuthorizationStatusEnum.Granted)
			{
				if (isGranted)
				{
					AuthorizedAccount = AuthorizingAccount;
					AuthorizedIP = AuthorizingIP;
					_status = AuthorizationStatusEnum.Granted;
					EV.PostMessage(Module, EventEnum.OperationAuthorization, $"{AuthorizedAccount} 获得操控权");
				}
				else
				{
					_status = AuthorizationStatusEnum.Rejected;
					EV.PostMessage(Module, EventEnum.OperationAuthorization, $"{AuthorizedAccount} 拒绝转交操控权");
				}
				Update();
			}
		}

		public static void Exit(string accountId)
		{
			if (accountId == AuthorizedAccount)
			{
				AuthorizedAccount = string.Empty;
				AuthorizedIP = string.Empty;
				Abort();
			}
			else if (accountId == AuthorizingAccount)
			{
				Abort();
			}
		}

		private static void Update()
		{
		}
	}
}
