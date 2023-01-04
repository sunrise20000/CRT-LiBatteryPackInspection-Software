using System;

namespace Aitex.Core.Account
{
	[Serializable]
	public class LoginResult
	{
		public bool ActSucc { get; set; }

		public string SessionId { get; set; }

		public Account AccountInfo { get; set; }

		public string Description { get; set; }
	}
}
