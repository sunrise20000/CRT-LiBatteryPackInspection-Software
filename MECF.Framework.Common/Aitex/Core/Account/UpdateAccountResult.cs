using System;

namespace Aitex.Core.Account
{
	[Serializable]
	public class UpdateAccountResult
	{
		public bool ActSucc { get; set; }

		public string Description { get; set; }
	}
}
