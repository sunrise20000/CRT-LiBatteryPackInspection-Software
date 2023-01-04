using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Aitex.Common.Util;
using Aitex.Core.Account;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Account.Extends;

namespace MECF.Framework.Common.Account
{
	public class AccountExManager : Singleton<AccountExManager>
	{
		private Dictionary<string, AccountEx> _loginUserList = new Dictionary<string, AccountEx>();

		private string _scAccountFile = PathManager.GetCfgDir() + "Account//Account.xml";

		private string _scAccountLocalFile = PathManager.GetCfgDir() + "Account//_Account.xml";

		public RoleLoader RoleLoader { get; private set; }

		public void Initialize(bool enableService)
		{
			if (!File.Exists(_scAccountLocalFile))
			{
				if (!File.Exists(_scAccountFile))
				{
					throw new ApplicationException("Can not initialize account configuration file, " + _scAccountFile);
				}
				File.Copy(_scAccountFile, _scAccountLocalFile);
				Thread.Sleep(10);
			}
			RoleLoader = new RoleLoader(_scAccountLocalFile);
			RoleLoader.Load();
			if (enableService)
			{
				Singleton<WcfServiceManager>.Instance.Initialize(new Type[1] { typeof(AccountService) });
			}
		}

		public LoginResult AuthLogin(string userid, string password, string role)
		{
			LoginResult loginResult = new LoginResult
			{
				ActSucc = true,
				SessionId = Guid.NewGuid().ToString()
			};
			try
			{
				List<AccountEx> accountList = RoleLoader.AccountList;
				AccountEx accountEx = accountList.FirstOrDefault((AccountEx f) => f.LoginName == userid);
				if (accountEx == null)
				{
					loginResult.ActSucc = false;
					loginResult.Description = AuthorizeResult.NoMatchUser.ToString();
				}
				else
				{
					foreach (AccountEx item in accountList)
					{
						if (item.LoginName == userid && item.Password == password)
						{
							foreach (string roleID in item.RoleIDs)
							{
								if (roleID == role || userid == "AMTEadmin")
								{
									loginResult.ActSucc = true;
									loginResult.Description = AuthorizeResult.None.ToString();
									foreach (KeyValuePair<string, AccountEx> loginUser in _loginUserList)
									{
										if (!SC.ContainsItem("System.AllowMultiClientLogin") || !SC.GetValue<bool>("System.AllowMultiClientLogin") || !(loginUser.Value.LoginName != userid))
										{
											EV.PostKickoutMessage(loginUser.Value.LoginId);
										}
									}
									_loginUserList[userid] = item;
									_loginUserList[userid].LoginId = loginResult.SessionId;
									return loginResult;
								}
								loginResult.ActSucc = false;
								loginResult.Description = AuthorizeResult.NoMatchRole.ToString();
							}
							return loginResult;
						}
						loginResult.ActSucc = false;
						loginResult.Description = AuthorizeResult.WrongPwd.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Error(ex.Message, ex);
			}
			return loginResult;
		}

		internal void Logout(string accountId, string loginId)
		{
			foreach (string key in _loginUserList.Keys)
			{
				if (accountId == key && _loginUserList[key].LoginId == loginId)
				{
					_loginUserList.Remove(key);
					break;
				}
			}
		}
	}
}
