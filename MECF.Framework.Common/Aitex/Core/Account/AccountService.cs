using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Key;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Account;
using MECF.Framework.Common.Account.Extends;

namespace Aitex.Core.Account
{
	public sealed class AccountService : IAccountService
	{
		public string Module = "System";

		public LoginResult Login(string accountId, string accountPwd)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not login");
				return new LoginResult
				{
					ActSucc = false,
					Description = "Software is expired"
				};
			}
			EV.PostInfoLog(Module, $"User {accountId} try to login System.");
			return AccountManager.Login(accountId, accountPwd);
		}

		public void Logout(string accountId)
		{
			EV.PostInfoLog(Module, $"User {accountId} logout sytem.");
			try
			{
			}
			catch
			{
			}
			AccountManager.Logout(accountId);
		}

		public GetAccountInfoResult GetAccountInfo(string accountId)
		{
			return AccountManager.GetAccountInfo(accountId);
		}

		public void RegisterViews(List<string> views)
		{
			AccountManager.RegisterViews(views);
		}

		public ChangePwdResult ChangePassword(string accountId, string newPassword)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return new ChangePwdResult();
			}
			EV.PostInfoLog(Module, $"Change user {accountId} password.");
			LOG.Write($"Account operation, change user {accountId} password.");
			return AccountManager.ChangePassword(accountId, newPassword);
		}

		public CreateAccountResult CreateAccount(Account newAccount)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return new CreateAccountResult();
			}
			EV.PostInfoLog(Module, $"Create account{newAccount}.");
			LOG.Write($"Account operation, Create user {newAccount.AccountId}.");
			return AccountManager.CreateAccount(newAccount);
		}

		public DeleteAccountResult DeleteAccount(string accountId)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return new DeleteAccountResult();
			}
			EV.PostInfoLog(Module, $"Delete account {accountId}.");
			return AccountManager.DeleteAccount(accountId);
		}

		public UpdateAccountResult UpdateAccount(Account account)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return new UpdateAccountResult();
			}
			EV.PostInfoLog(Module, $"Update {account} account information.");
			return AccountManager.UpdateAccount(account);
		}

		public GetAccountListResult GetAccountList()
		{
			return AccountManager.GetAccountList();
		}

		public List<Account> GetLoginUsers()
		{
			return AccountManager.GetLoginUserList();
		}

		public void KickUserOut(string accountId, string kickoutReason)
		{
			EV.PostInfoLog(Module, $"Force user {accountId} logout system，reason：{kickoutReason}.");
			try
			{
			}
			catch
			{
			}
			AccountManager.Kickout(accountId, kickoutReason);
		}

		public SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission()
		{
			return AccountManager.GetAllRolesPermission();
		}

		public bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			return AccountManager.SaveAllRolesPermission(data);
		}

		public SerializableDictionary<string, string> GetAllViewList()
		{
			return AccountManager.GetAllViewList();
		}

		public IEnumerable<string> GetAllRoles()
		{
			return AccountManager.GetAllRoles();
		}

		public void CheckAlive(string accountId)
		{
			AccountManager.CheckAlive(accountId);
		}

		public string GetProcessViewPermission()
		{
			string filename = Path.Combine(PathManager.GetCfgDir(), "RolePermission.xml");
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(filename);
				return xmlDocument.InnerXml;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return "<Aitex></Aitex>";
			}
			finally
			{
			}
		}

		public bool SaveProcessViewPermission(string viewXML)
		{
			try
			{
				string filename = Path.Combine(PathManager.GetCfgDir(), "RolePermission.xml");
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(viewXML);
				XmlTextWriter xmlTextWriter = new XmlTextWriter(filename, null);
				xmlTextWriter.Formatting = Formatting.Indented;
				xmlDocument.Save(xmlTextWriter);
				xmlTextWriter.Close();
				return true;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return false;
			}
		}

		public List<Role> GetAllRoleList()
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.RoleList;
		}

		public List<AccountEx> GetAllAccountExList()
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.AccountList;
		}

		public List<Role> GetRoles()
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.GetRoles();
		}

		public List<AccountEx> GetAccounts()
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.GetAccounts();
		}

		public bool UpdateRole(Role role)
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.UpdateRole(role);
		}

		public bool DeleteRole(string roleId)
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.DeleteRole(roleId);
		}

		public List<AppMenu> GetMenusByRole(string roleId, List<AppMenu> lstMenu)
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.GetMenusByRole(roleId, lstMenu);
		}

		public int GetMenuPermission(string roleId, string menuName)
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.GetMenuPermission(roleId, menuName);
		}

		public bool UpdateAccountEx(AccountEx account)
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.UpdateAccount(account);
		}

		public bool DeleteAccountEx(string accountId)
		{
			return Singleton<AccountExManager>.Instance.RoleLoader.DeleteAccount(accountId);
		}

		public LoginResult LoginEx(string accountId, string password, string role)
		{
			return Singleton<AccountExManager>.Instance.AuthLogin(accountId, password, role);
		}

		public void LogoutEx(string accountId, string loginId)
		{
			Singleton<AccountExManager>.Instance.Logout(accountId, loginId);
		}
	}
}
