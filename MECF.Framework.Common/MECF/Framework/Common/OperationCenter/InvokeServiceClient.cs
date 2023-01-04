using Aitex.Core.WCF;

namespace MECF.Framework.Common.OperationCenter
{
	public class InvokeServiceClient : ServiceClientWrapper<IInvokeService>, IInvokeService
	{
		public InvokeServiceClient()
			: base("Client_IInvokeService", "InvokeService")
		{
		}

		public void DoOperation(string operationName, params object[] args)
		{
			Invoke(delegate(IInvokeService svc)
			{
				svc.DoOperation(operationName, args);
			});
		}
	}
}
