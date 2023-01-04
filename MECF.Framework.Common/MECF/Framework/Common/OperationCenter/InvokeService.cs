using System;
using System.ServiceModel;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.OperationCenter
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class InvokeService : IInvokeService
	{
		public void DoOperation(string operationName, params object[] args)
		{
			try
			{
				OP.DoOperation(operationName, args);
			}
			catch (Exception ex)
			{
				LOG.Error($"调用{operationName}，碰到未处理的WCF操作异常", ex);
			}
		}
	}
}
