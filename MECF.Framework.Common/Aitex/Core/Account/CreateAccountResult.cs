using System;

namespace Aitex.Core.Account
{
	[Serializable]
	public class CreateAccountResult
	{
		public bool ActSucc { get; set; }

		public string Description { get; set; }
	}
}
