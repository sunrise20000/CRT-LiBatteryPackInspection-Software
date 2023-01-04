using Aitex.Core.Account;
using Aitex.Core.Util;

namespace MECF.Framework.UI.Core.Accounts
{
	public class AccountClient : Singleton<AccountClient>
	{
		private IAccountService _service;

		public bool InProcess { get; set; }

		public Account CurrentUser { get; set; }

		public IAccountService Service
		{
			get
			{
				if (_service == null)
				{
					if (InProcess)
					{
						_service = new AccountService();
					}
					else
					{
						_service = new AccountServiceClient();
					}
				}
				return _service;
			}
		}
	}
}
