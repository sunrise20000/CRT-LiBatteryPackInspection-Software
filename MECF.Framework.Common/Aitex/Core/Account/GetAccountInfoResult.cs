using System;

namespace Aitex.Core.Account
{
	[Serializable]
	public class GetAccountInfoResult
	{
		public bool ActSuccess { get; set; }

		public Account AccountInfo { get; set; }

		public string Description { get; set; }
	}
}
