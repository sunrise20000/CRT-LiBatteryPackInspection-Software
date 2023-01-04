using Aitex.Core.Util;

namespace MECF.Framework.Common.OperationCenter
{
	public class InvokeClient : Singleton<InvokeClient>
	{
		private IInvokeService _service;

		public bool InProcess { get; set; }

		public IInvokeService Service
		{
			get
			{
				if (_service == null)
				{
					if (InProcess)
					{
						_service = new InvokeService();
					}
					else
					{
						_service = new InvokeServiceClient();
					}
				}
				return _service;
			}
		}
	}
}
