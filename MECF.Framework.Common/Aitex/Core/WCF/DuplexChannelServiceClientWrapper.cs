using System;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using Aitex.Core.RT.Log;

namespace Aitex.Core.WCF
{
	public class DuplexChannelServiceClientWrapper<T>
	{
		private DuplexChannelFactory<T> _factory;

		private T _proxy;

		private bool _isInError = false;

		private string _serviceName;

		private InstanceContext _callbackInstance;

		public DuplexChannelServiceClientWrapper(InstanceContext callbackInstance, string endpointConfigurationName, string name)
		{
			_serviceName = name;
			_callbackInstance = callbackInstance;
			try
			{
				_factory = new DuplexChannelFactory<T>(_callbackInstance, endpointConfigurationName);
			}
			catch (Exception ex)
			{
				MessageBox.Show("不能创建Service " + name + "，检查配置：" + endpointConfigurationName + ex.Message);
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
	}
}
