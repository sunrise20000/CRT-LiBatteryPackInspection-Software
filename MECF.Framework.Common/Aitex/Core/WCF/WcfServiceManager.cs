using System;
using System.Collections.Generic;
using System.ServiceModel;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.WCF
{
	public class WcfServiceManager : Singleton<WcfServiceManager>
	{
		private List<ServiceHost> _serviceHost = new List<ServiceHost>();

		public void Initialize(Type[] serviceType)
		{
			foreach (Type type in serviceType)
			{
				try
				{
					ServiceHost serviceHost = new ServiceHost(type);
					serviceHost.Open();
					_serviceHost.Add(serviceHost);
				}
				catch (Exception ex)
				{
					LOG.Error($"未能打开WCF服务'{ex.Message}'.\n请检查配置后重新启动程序.");
					throw new ApplicationException(string.Format("初始化{0}服务失败，", type.ToString(), ex.Message));
				}
			}
		}

		public void Terminate()
		{
			foreach (ServiceHost item in _serviceHost)
			{
				try
				{
					item.Abort();
					item.Close();
				}
				catch (Exception ex)
				{
					LOG.Error($"关闭'{item.Description.Name}'服务失败", ex);
				}
			}
		}
	}
}
