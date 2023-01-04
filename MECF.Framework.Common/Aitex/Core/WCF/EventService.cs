using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.WCF.Interface;

namespace Aitex.Core.WCF
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class EventService : IEventService
	{
		private ConcurrentDictionary<Guid, IEventServiceCallback> _callbackClientList = new ConcurrentDictionary<Guid, IEventServiceCallback>();

		private int _counter;

		public event Action<EventItem> OnEvent;

		public void FireEvent(EventItem obj)
		{
			Guid[] array = _callbackClientList.Keys.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					_callbackClientList[array[i]].SendEvent(obj);
				}
				catch (Exception ex)
				{
					LOG.Error($"给客户端{array[i]}发送事件失败，客户端被删除.", ex);
					_callbackClientList.TryRemove(array[i], out var _);
				}
			}
			if (this.OnEvent != null)
			{
				this.OnEvent(obj);
			}
		}

		public bool Register(Guid id)
		{
			try
			{
				if (!_callbackClientList.ContainsKey(id))
				{
					LOG.Info($"客户端{id.ToString()}已连接", isTraceOn: true);
				}
				_callbackClientList[id] = OperationContext.Current.GetCallbackChannel<IEventServiceCallback>();
			}
			catch (Exception ex)
			{
				LOG.Error("监听事件发生错误", ex);
			}
			return true;
		}

		public void UnRegister(Guid id)
		{
			if (!_callbackClientList.ContainsKey(id))
			{
				LOG.Info($"客户端{id.ToString()}断开连接", isTraceOn: true);
			}
			_callbackClientList.TryRemove(id, out var _);
		}

		public int Heartbeat()
		{
			return _counter++ % 100000;
		}
	}
}
