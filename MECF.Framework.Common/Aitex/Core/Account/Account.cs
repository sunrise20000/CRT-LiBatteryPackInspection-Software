using System;
using Aitex.Core.Util;

namespace Aitex.Core.Account
{
	[Serializable]
	public class Account
	{
		public string AccountId { get; set; }

		public string Role { get; set; }

		public string RealName { get; set; }

		public string Touxian { get; set; }

		public string Company { get; set; }

		public string Department { get; set; }

		public bool AccountStatus { get; set; }

		public string Email { get; set; }

		public string Telephone { get; set; }

		public string Description { get; set; }

		public string LastLoginTime { get; set; }

		public string LastAccountUpdateTime { get; set; }

		public string AccountCreationTime { get; set; }

		public SerializableDictionary<string, ViewPermission> Permission { get; set; }

		public string Md5Pwd { get; set; }

		public string LoginIP { get; set; }

		public string LoginId { get; set; }
	}
}
