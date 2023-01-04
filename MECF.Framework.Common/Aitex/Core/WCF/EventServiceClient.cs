using System;
using System.ServiceModel;
using Aitex.Core.RT.Event;
using Aitex.Core.WCF.Interface;

namespace Aitex.Core.WCF
{
	public class EventServiceClient : DuplexChannelServiceClientWrapper<IEventService>, IEventService
	{
		public event Action<EventItem> OnEvent;

		public EventServiceClient(EventServiceCallback callbackInstance)
			: base(new InstanceContext(callbackInstance), "Client_IEventService", "EventService")
		{
			callbackInstance.FireEvent += callbackInstance_FireEvent;
		}

		private void callbackInstance_FireEvent(EventItem obj)
		{
			if (this.OnEvent != null)
			{
				this.OnEvent(obj);
			}
		}

		public bool Register(Guid id)
		{
			bool ret = false;
			Invoke(delegate(IEventService svc)
			{
				ret = svc.Register(id);
			});
			return ret;
		}

		public void UnRegister(Guid id)
		{
			Invoke(delegate(IEventService svc)
			{
				svc.UnRegister(id);
			});
		}

		public int Heartbeat()
		{
			int result = -1;
			Invoke(delegate(IEventService svc)
			{
				result = svc.Heartbeat();
			});
			return result;
		}
	}
}
