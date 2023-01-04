using System;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.Account.Extends
{
	[Serializable]
	public class UserContext : NotifiableItem
	{
		private string _loginName;

		private string _rolename;

		public string Address { get; set; }

		public string HostName { get; set; }

		public string LoginName
		{
			get
			{
				return _loginName;
			}
			set
			{
				_loginName = value;
				InvokePropertyChanged("LoginName");
			}
		}

		public string RoleName
		{
			get
			{
				return _rolename;
			}
			set
			{
				_rolename = value;
				InvokePropertyChanged("RoleName");
			}
		}

		public string RoleID { get; set; }

		public string LoginId { get; set; }

		public Role Role { get; set; }

		public DateTime LoginTime { get; set; }

		public DateTime LastAccessTime { get; set; }

		public int Token { get; set; }

		public string Language { get; private set; }

		public bool IsLogin { get; set; }
	}
}
