using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Windows;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;

namespace Aitex.Core.WCF
{
	public class ServiceClientWrapper<T>
	{
		private ChannelFactory<T> _factory;

		private T _proxy;

		private bool _isInError = false;

		private string _serviceName;

		private Retry _retryConnect = new Retry();

		public bool ActionFailed => _isInError;

		public ServiceClientWrapper(string endpointConfigurationName, string name)
		{
			_serviceName = name;
			try
			{
				_factory = new ChannelFactory<T>(endpointConfigurationName);
			}
			catch (Exception ex)
			{
				MessageBox.Show("不能创建Service " + name + "，检查配置：" + endpointConfigurationName);
				LOG.Error("不能创建Service " + name + "，检查配置：" + endpointConfigurationName, ex);
			}
		}

		public ServiceClientWrapper(string endpointConfigurationName, string name, EndpointAddress address)
		{
			_serviceName = name;
			try
			{
				_factory = new ChannelFactory<T>(endpointConfigurationName, address);
			}
			catch (Exception ex)
			{
				MessageBox.Show("不能创建Service " + name + "，检查配置：" + endpointConfigurationName);
				LOG.Error("不能创建Service " + name + "，检查配置：" + endpointConfigurationName, ex);
			}
		}

		public void Invoke(Action<T> action)
		{
			if (_factory == null)
			{
				return;
			}
			if (_proxy == null)
			{
				_proxy = _factory.CreateChannel();
			}
			for (int i = 0; i < 2; i++)
			{
				if (Do(action))
				{
					break;
				}
				Thread.Sleep(10);
			}
		}

		private bool Do(Action<T> action)
		{
			if (_proxy != null && ((IClientChannel)(object)_proxy).State == CommunicationState.Faulted)
			{
				((IClientChannel)(object)_proxy).Abort();
				_proxy = _factory.CreateChannel();
			}
			try
			{
				action(_proxy);
				if (_isInError)
				{
					_isInError = false;
					LOG.Info(_serviceName + " 服务恢复", isTraceOn: false);
				}
				return true;
			}
			catch (EndpointNotFoundException ex)
			{
				if (!_isInError)
				{
					_isInError = true;
					LOG.Error(_serviceName + " 连接已经断开.", ex);
				}
			}
			catch (ProtocolException ex2)
			{
				if (!_isInError)
				{
					_isInError = true;
					LOG.Error(_serviceName + " 服务程序异常.", ex2);
				}
			}
			catch (Exception ex3)
			{
				if (!_isInError)
				{
					_isInError = true;
					LOG.Error(_serviceName + " 服务异常", ex3);
				}
			}
			((IClientChannel)(object)_proxy).Abort();
			_proxy = _factory.CreateChannel();
			return false;
		}

		private Binding CreateBinding(string binding)
		{
			Binding result = null;
			if (binding.ToLower() == "basichttpbinding")
			{
				BasicHttpBinding basicHttpBinding = new BasicHttpBinding();
				basicHttpBinding.MaxBufferSize = int.MaxValue;
				basicHttpBinding.MaxBufferPoolSize = 2147483647L;
				basicHttpBinding.MaxReceivedMessageSize = 2147483647L;
				basicHttpBinding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
				basicHttpBinding.CloseTimeout = new TimeSpan(0, 10, 0);
				basicHttpBinding.OpenTimeout = new TimeSpan(0, 10, 0);
				basicHttpBinding.ReceiveTimeout = new TimeSpan(0, 10, 0);
				basicHttpBinding.SendTimeout = new TimeSpan(0, 10, 0);
				result = basicHttpBinding;
			}
			else if (binding.ToLower() == "netnamedpipebinding")
			{
				NetNamedPipeBinding netNamedPipeBinding = new NetNamedPipeBinding();
				netNamedPipeBinding.MaxReceivedMessageSize = 65535000L;
				result = netNamedPipeBinding;
			}
			else if (binding.ToLower() == "nettcpbinding")
			{
				NetTcpBinding netTcpBinding = new NetTcpBinding();
				netTcpBinding.MaxReceivedMessageSize = 65535000L;
				netTcpBinding.Security.Mode = SecurityMode.None;
				result = netTcpBinding;
			}
			else if (binding.ToLower() == "wsdualhttpbinding")
			{
				WSDualHttpBinding wSDualHttpBinding = new WSDualHttpBinding();
				wSDualHttpBinding.MaxReceivedMessageSize = 65535000L;
				result = wSDualHttpBinding;
			}
			else if (!(binding.ToLower() == "webhttpbinding"))
			{
				if (binding.ToLower() == "wsfederationhttpbinding")
				{
					WSFederationHttpBinding wSFederationHttpBinding = new WSFederationHttpBinding();
					wSFederationHttpBinding.MaxReceivedMessageSize = 65535000L;
					result = wSFederationHttpBinding;
				}
				else if (binding.ToLower() == "wshttpbinding")
				{
					WSHttpBinding wSHttpBinding = new WSHttpBinding(SecurityMode.None);
					wSHttpBinding.MaxReceivedMessageSize = 65535000L;
					wSHttpBinding.Security.Message.ClientCredentialType = MessageCredentialType.Windows;
					wSHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
					result = wSHttpBinding;
				}
			}
			return result;
		}
	}
}
