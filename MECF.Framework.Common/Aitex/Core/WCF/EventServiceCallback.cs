using System;
using System.ServiceModel;
using Aitex.Core.RT.Event;
using Aitex.Core.WCF.Interface;

namespace Aitex.Core.WCF
{
	[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class EventServiceCallback : IEventServiceCallback
	{
		public event Action<EventItem> FireEvent;

		public void SendEvent(EventItem ev)
		{
			if (this.FireEvent != null)
			{
				this.FireEvent(ev);
			}
		}
	}
}
