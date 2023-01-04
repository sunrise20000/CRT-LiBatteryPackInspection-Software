using System;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.WCF.Interface;

namespace Aitex.Core.WCF
{
	public class EventClient : Singleton<EventClient>
	{
		private IEventService _service;

		private Retry _retryConnectRT = new Retry();

		private FixSizeQueue<EventItem> _queue = new FixSizeQueue<EventItem>(500);

		private PeriodicJob _fireJob;

		private Guid _guid = Guid.NewGuid();

		private R_TRIG _trigDisconnect = new R_TRIG();

		private DeviceTimer _timer = new DeviceTimer();

		public bool InProcess { get; set; }

		public IEventService Service
		{
			get
			{
				if (_service == null)
				{
					if (InProcess)
					{
						_service = Singleton<EventManager>.Instance.Service;
					}
					else
					{
						_service = new EventServiceClient(new EventServiceCallback());
					}
					_service.OnEvent += _service_OnEvent;
				}
				return _service;
			}
		}

		public bool IsConnectedWithRT => _retryConnectRT.Result;

		public event Action<EventItem> OnEvent;

		public event Action OnDisconnectedWithRT;

		public bool ConnectRT()
		{
			_retryConnectRT.Result = InProcess || Service.Register(_guid);
			return _retryConnectRT.IsSucceeded;
		}

		public void Start()
		{
			if (_fireJob == null)
			{
				_fireJob = new PeriodicJob(500, FireEvent, "UIEvent", isStartNow: true);
			}
		}

		public void Stop()
		{
			if (!InProcess && _retryConnectRT.IsSucceeded)
			{
				Service.UnRegister(_guid);
			}
			if (_fireJob != null)
			{
				_fireJob.Stop();
			}
		}

		private bool FireEvent()
		{
			_retryConnectRT.Result = InProcess || Service.Register(_guid);
			if (_retryConnectRT.IsSucceeded)
			{
				_service_OnEvent(new EventItem
				{
					OccuringTime = DateTime.Now,
					Description = "Connected with RT",
					Id = 0,
					Level = EventLevel.Information,
					Type = EventType.EventUI_Notify
				});
			}
			if (_retryConnectRT.IsErrored)
			{
				_service_OnEvent(new EventItem
				{
					OccuringTime = DateTime.Now,
					Description = "Disconnect from RT",
					Id = 0,
					Level = EventLevel.Information,
					Type = EventType.EventUI_Notify
				});
				_timer.Start(0.0);
			}
			if (_timer.GetElapseTime() > 2000.0 && !_retryConnectRT.Result && this.OnDisconnectedWithRT != null)
			{
				this.OnDisconnectedWithRT();
			}
			if (this.OnEvent != null)
			{
				EventItem obj;
				while (_queue.TryDequeue(out obj))
				{
					this.OnEvent(obj);
				}
			}
			return true;
		}

		private void _service_OnEvent(EventItem ev)
		{
			_queue.Enqueue(ev);
		}
	}
}
