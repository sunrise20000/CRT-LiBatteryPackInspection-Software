using System;
using System.Collections.Generic;

namespace Aitex.Core.Account
{
	[Serializable]
	public class GetAccountListResult
	{
		public bool ActSuccess { get; set; }

		public IEnumerable<Account> AccountList { get; set; }

		public string Description { get; set; }
	}
}
