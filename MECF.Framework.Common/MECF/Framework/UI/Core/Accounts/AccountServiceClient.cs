using System.Collections.Generic;
using Aitex.Core.Account;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Account.Extends;

namespace MECF.Framework.UI.Core.Accounts
{
	public class AccountServiceClient : ServiceClientWrapper<IAccountService>, IAccountService
	{
		public AccountServiceClient()
			: base("Client_IAccountService", "AccountService")
		{
		}

		public LoginResult Login(string accountId, string accountPwd)
		{
			LoginResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.Login(accountId, accountPwd);
			});
			return result;
		}

		LoginResult IAccountService.Login(string accountId, string password)
		{
			return Login(accountId, password);
		}

		public void Logout(string accountId)
		{
			Invoke(delegate(IAccountService svc)
			{
				svc.Logout(accountId);
			});
		}

		public GetAccountInfoResult GetAccountInfo(string accountId)
		{
			GetAccountInfoResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAccountInfo(accountId);
			});
			return result;
		}

		public void RegisterViews(List<string> views)
		{
			Invoke(delegate(IAccountService svc)
			{
				svc.RegisterViews(views);
			});
		}

		public ChangePwdResult ChangePassword(string accountId, string newPassword)
		{
			ChangePwdResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.ChangePassword(accountId, newPassword);
			});
			return result;
		}

		public CreateAccountResult CreateAccount(Account newAccount)
		{
			CreateAccountResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.CreateAccount(newAccount);
			});
			return result;
		}

		public DeleteAccountResult DeleteAccount(string accountId)
		{
			DeleteAccountResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.DeleteAccount(accountId);
			});
			return result;
		}

		public UpdateAccountResult UpdateAccount(Account account)
		{
			UpdateAccountResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.UpdateAccount(account);
			});
			return result;
		}

		public GetAccountListResult GetAccountList()
		{
			GetAccountListResult result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAccountList();
			});
			return result;
		}

		public List<Account> GetLoginUsers()
		{
			List<Account> result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetLoginUsers();
			});
			return result;
		}

		public void KickUserOut(string accountId, string kickoutReason)
		{
			Invoke(delegate(IAccountService svc)
			{
				svc.KickUserOut(accountId, kickoutReason);
			});
		}

		public SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission()
		{
			SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAllRolesPermission();
			});
			return result;
		}

		public bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data)
		{
			bool result = false;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.SaveAllRolesPermission(data);
			});
			return result;
		}

		public SerializableDictionary<string, string> GetAllViewList()
		{
			SerializableDictionary<string, string> result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAllViewList();
			});
			return result;
		}

		public IEnumerable<string> GetAllRoles()
		{
			IEnumerable<string> result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAllRoles();
			});
			return result;
		}

		public void CheckAlive(string accountId)
		{
			Invoke(delegate(IAccountService svc)
			{
				svc.CheckAlive(accountId);
			});
		}

		public string GetProcessViewPermission()
		{
			string result = null;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetProcessViewPermission();
			});
			return result;
		}

		public bool SaveProcessViewPermission(string viewXML)
		{
			bool result = false;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.SaveProcessViewPermission(viewXML);
			});
			return result;
		}

		public List<Role> GetAllRoleList()
		{
			List<Role> result = new List<Role>();
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAllRoleList();
			});
			return result;
		}

		public List<AccountEx> GetAllAccountExList()
		{
			List<AccountEx> result = new List<AccountEx>();
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAllAccountExList();
			});
			return result;
		}

		public List<Role> GetRoles()
		{
			List<Role> result = new List<Role>();
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetRoles();
			});
			return result;
		}

		public List<AccountEx> GetAccounts()
		{
			List<AccountEx> result = new List<AccountEx>();
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetAccounts();
			});
			return result;
		}

		public bool UpdateRole(Role p_newRole)
		{
			bool result = false;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.UpdateRole(p_newRole);
			});
			return result;
		}

		public bool DeleteRole(string p_strRoleID)
		{
			bool result = false;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.DeleteRole(p_strRoleID);
			});
			return result;
		}

		public List<AppMenu> GetMenusByRole(string roleid, List<AppMenu> menulist)
		{
			List<AppMenu> result = new List<AppMenu>();
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetMenusByRole(roleid, menulist);
			});
			return result;
		}

		public int GetMenuPermission(string roleId, string menuName)
		{
			int result = 0;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.GetMenuPermission(roleId, menuName);
			});
			return result;
		}

		public bool UpdateAccountEx(AccountEx account)
		{
			bool result = false;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.UpdateAccountEx(account);
			});
			return result;
		}

		public bool DeleteAccountEx(string accountId)
		{
			bool result = false;
			Invoke(delegate(IAccountService svc)
			{
				result = svc.DeleteAccountEx(accountId);
			});
			return result;
		}

		public LoginResult LoginEx(string accountId, string password, string role)
		{
			LoginResult result = new LoginResult();
			Invoke(delegate(IAccountService svc)
			{
				result = svc.LoginEx(accountId, password, role);
			});
			return result;
		}

		public void LogoutEx(string accountId, string loginId)
		{
			Invoke(delegate(IAccountService svc)
			{
				svc.LogoutEx(accountId, loginId);
			});
		}
	}
}
