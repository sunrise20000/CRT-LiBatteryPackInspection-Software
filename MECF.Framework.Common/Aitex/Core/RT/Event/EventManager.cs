using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.FAServices;

namespace Aitex.Core.RT.Event
{
	public class EventManager : ICommonEvent
	{
		private FixSizeQueue<EventItem> _eventQueue;

		private FixSizeQueue<EventItem> _alarmQueue;

		private PeriodicJob _eventJob;

		private EventDBWriter _eventDB;

		private EventLogWriter _writerToLog;

		private EventMailWriter _writerToMail;

		private EventService _eventService;

		private ServiceHost _eventServiceHost;

		private Dictionary<string, EventItem> _eventDic = new Dictionary<string, EventItem>();

		private const string INFORMATION_EVENT = "INFORMATION_EVENT";

		private const string WARNING_EVENT = "WARNING_EVENT";

		private const string ALARM_EVENT = "ALARM_EVENT";

		private List<AlarmEventItem> _alarms;

		public EventService Service => _eventService;

		public List<VIDItem> VidEventList
		{
			get
			{
				List<VIDItem> list = new List<VIDItem>();
				foreach (KeyValuePair<string, EventItem> item in _eventDic)
				{
					list.Add(new VIDItem
					{
						DataType = "",
						Description = item.Value.Description,
						Index = 0,
						Name = item.Key,
						Unit = ""
					});
				}
				return list;
			}
		}

		public List<VIDItem> VidAlarmList
		{
			get
			{
				List<VIDItem> list = new List<VIDItem>();
				foreach (AlarmEventItem alarm in _alarms)
				{
					list.Add(new VIDItem
					{
						DataType = "",
						Description = alarm.Description,
						Index = 0,
						Name = alarm.EventEnum,
						Unit = ""
					});
				}
				return list;
			}
		}

		public event Action<EventItem> FireEvent;

		public event Action<EventItem> OnAlarmEvent;

		public event Action<EventItem> OnEvent;

		public void Initialize(string commonEventListXmlFile, bool needCreateService = true, bool needSaveDB = true, bool needMailOut = false, string localEventListXmlFile = null)
		{
			if (needSaveDB)
			{
				_eventDB = new EventDBWriter();
				try
				{
					_eventDB.Initialize();
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			}
			_writerToLog = new EventLogWriter();
			if (needMailOut)
			{
				_writerToMail = new EventMailWriter();
			}
			_eventService = new EventService();
			if (needCreateService)
			{
				try
				{
					_eventServiceHost = new ServiceHost(_eventService);
					_eventServiceHost.Open();
				}
				catch (Exception ex2)
				{
					throw new ApplicationException("创建Event服务失败，" + ex2.Message);
				}
			}
			_eventQueue = new FixSizeQueue<EventItem>(1000);
			_alarmQueue = new FixSizeQueue<EventItem>(1000);
			_alarms = new List<AlarmEventItem>(1000);
			_eventJob = new PeriodicJob(100, PeriodicRun, "EventPeriodicJob", isStartNow: true);
			try
			{
				EventDefine eventDefine = CustomXmlSerializer.Deserialize<EventDefine>(new FileInfo(commonEventListXmlFile));
				foreach (EventItem item in eventDefine.Items)
				{
					_eventDic[item.EventEnum] = item;
				}
			}
			catch (ArgumentNullException)
			{
				throw new ApplicationException("初始化EventManager没有设置Event列表文件");
			}
			catch (FileNotFoundException ex4)
			{
				throw new ApplicationException("没有找到Event列表文件，" + ex4.Message);
			}
			catch (Exception ex5)
			{
				throw new ApplicationException("EventDefine文件格式不对，" + commonEventListXmlFile + ",\r\n" + ex5.Message);
			}
			try
			{
				if (!string.IsNullOrEmpty(localEventListXmlFile))
				{
					EventDefine eventDefine2 = CustomXmlSerializer.Deserialize<EventDefine>(new FileInfo(localEventListXmlFile));
					foreach (EventItem item2 in eventDefine2.Items)
					{
						_eventDic[item2.EventEnum] = item2;
					}
				}
			}
			catch (ArgumentNullException)
			{
				throw new ApplicationException("初始化EventManager没有设置Event列表文件");
			}
			catch (FileNotFoundException ex7)
			{
				throw new ApplicationException("没有找到Event列表文件，" + ex7.Message);
			}
			catch (Exception ex8)
			{
				throw new ApplicationException("EventDefine文件格式不对，" + localEventListXmlFile + ",\r\n" + ex8.Message);
			}
			Subscribe(new EventItem("INFORMATION_EVENT", EventType.EventUI_Notify, EventLevel.Information));
			Subscribe(new EventItem("WARNING_EVENT", EventType.EventUI_Notify, EventLevel.Warning));
			Subscribe(new EventItem("ALARM_EVENT", EventType.EventUI_Notify, EventLevel.Alarm));
			EV.InnerEventManager = this;
		}

		public void SubscribeOperationAndData()
		{
			OP.Subscribe("System.ResetAlarm", InvokeResetAlarm);
			DATA.Subscribe("System.ActiveAlarm", () => _alarms.FindAll((AlarmEventItem x) => !x.IsAcknowledged));
			DATA.Subscribe("System.HasActiveAlarm", () => _alarms.FirstOrDefault((AlarmEventItem x) => !x.IsAcknowledged) != null);
		}

		private bool InvokeResetAlarm(string arg1, object[] arg2)
		{
			ClearAlarmEvent();
			if (arg2 != null && arg2.Length >= 2)
			{
				_alarms.FirstOrDefault((AlarmEventItem x) => x.Source == (string)arg2[0] && x.EventEnum == (string)arg2[1])?.Reset();
			}
			return true;
		}

		public void Terminate()
		{
			if (_eventJob != null)
			{
				_eventJob.Stop();
				_eventJob = null;
			}
			if (_eventServiceHost != null)
			{
				_eventServiceHost.Close();
				_eventServiceHost = null;
			}
		}

		public void WriteEvent(string eventName)
		{
			if (!_eventDic.ContainsKey(eventName))
			{
				LOG.Write("Event name not registered, " + eventName);
			}
			else
			{
				WriteEvent(_eventDic[eventName].Source, eventName);
			}
		}

		public void WriteEvent(string module, string eventName, string message)
		{
			if (!_eventDic.ContainsKey(eventName))
			{
				LOG.Write("Event name not registered, " + eventName);
				return;
			}
			EventItem eventItem = _eventDic[eventName].Clone();
			eventItem.Source = module;
			eventItem.Description = message;
			eventItem.OccuringTime = DateTime.Now;
			_eventQueue.Enqueue(eventItem);
			if (eventItem.Level == EventLevel.Alarm || eventItem.Level == EventLevel.Warning)
			{
				_alarmQueue.Enqueue(eventItem);
				if (this.OnAlarmEvent != null)
				{
					this.OnAlarmEvent(eventItem);
				}
			}
			if (this.OnEvent != null)
			{
				this.OnEvent(eventItem);
			}
			_writerToLog.WriteEvent(eventItem);
		}

		public void WriteEvent(string eventName, SerializableDictionary<string, string> dvid)
		{
			if (!_eventDic.ContainsKey(eventName))
			{
				LOG.Write("Event name not registered, " + eventName);
			}
			else
			{
				WriteEvent(_eventDic[eventName].Source, eventName, dvid);
			}
		}

		public void WriteEvent(string eventName, SerializableDictionary<string, object> dvid)
		{
			if (!_eventDic.ContainsKey(eventName))
			{
				LOG.Error("Event name not registered, " + eventName);
				return;
			}
			EventItem eventItem = _eventDic[eventName].Clone(dvid);
			eventItem.OccuringTime = DateTime.Now;
			ProceedReceivedEvent(eventItem);
		}

		public void WriteEvent(string module, string eventName, params object[] args)
		{
			EventItem eventItem = _eventDic[eventName].Clone();
			eventItem.Source = module;
			if (_eventDic[eventName].Description == null)
			{
				return;
			}
			eventItem.Description = string.Format(_eventDic[eventName].Description, args);
			if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_en))
			{
				eventItem.GlobalDescription_en = string.Format(_eventDic[eventName].GlobalDescription_en, args);
			}
			if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_zh))
			{
				eventItem.GlobalDescription_zh = string.Format(_eventDic[eventName].GlobalDescription_zh, args);
			}
			eventItem.OccuringTime = DateTime.Now;
			_eventQueue.Enqueue(eventItem);
			if (eventItem.Level == EventLevel.Alarm || eventItem.Level == EventLevel.Warning)
			{
				_alarmQueue.Enqueue(eventItem);
				if (this.OnAlarmEvent != null)
				{
					this.OnAlarmEvent(eventItem);
				}
			}
			if (this.OnEvent != null)
			{
				this.OnEvent(eventItem);
			}
			_writerToLog.WriteEvent(eventItem);
		}

		public void WriteEvent(string module, string eventName, SerializableDictionary<string, string> dvid, params object[] args)
		{
			EventItem eventItem = _eventDic[eventName].Clone();
			eventItem.Source = module;
			eventItem.Description = string.Format(_eventDic[eventName].Description, args);
			if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_en))
			{
				eventItem.GlobalDescription_en = string.Format(_eventDic[eventName].GlobalDescription_en, args);
			}
			if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_zh))
			{
				eventItem.GlobalDescription_zh = string.Format(_eventDic[eventName].GlobalDescription_zh, args);
			}
			eventItem.OccuringTime = DateTime.Now;
			eventItem.DVID = dvid;
			_eventQueue.Enqueue(eventItem);
			if (eventItem.Level == EventLevel.Alarm || eventItem.Level == EventLevel.Warning)
			{
				_alarmQueue.Enqueue(eventItem);
				if (this.OnAlarmEvent != null)
				{
					this.OnAlarmEvent(eventItem);
				}
			}
			if (this.OnEvent != null)
			{
				this.OnEvent(eventItem);
			}
			_writerToLog.WriteEvent(eventItem);
		}

		private void ProceedReceivedEvent(EventItem item)
		{
			_eventQueue.Enqueue(item);
			if (item.Level == EventLevel.Alarm || item.Level == EventLevel.Warning)
			{
				_alarmQueue.Enqueue(item);
				if (this.OnAlarmEvent != null)
				{
					this.OnAlarmEvent(item);
				}
			}
			if (this.OnEvent != null)
			{
				this.OnEvent(item);
			}
			_writerToLog.WriteEvent(item);
		}

		public void PostNotificationMessage(string message)
		{
			EventItem eventItem = new EventItem
			{
				Type = EventType.UIMessage_Notify,
				Description = message,
				OccuringTime = DateTime.Now
			};
			_eventQueue.Enqueue(eventItem);
			_writerToLog.WriteEvent(eventItem);
		}

		public void PostPopDialogMessage(EventLevel level, string title, string message)
		{
			EventItem eventItem = new EventItem
			{
				Type = EventType.Dialog_Nofity,
				Description = title,
				Explaination = message,
				OccuringTime = DateTime.Now,
				Level = level
			};
			_eventQueue.Enqueue(eventItem);
			_writerToLog.WriteEvent(eventItem);
		}

		public void PostKickoutMessage(string message)
		{
			EventItem eventItem = new EventItem
			{
				Type = EventType.KickOut_Notify,
				Description = message,
				OccuringTime = DateTime.Now
			};
			_eventQueue.Enqueue(eventItem);
			_writerToLog.WriteEvent(eventItem);
		}

		public void PostSoundMessage(string message)
		{
			EventItem eventItem = new EventItem
			{
				Type = EventType.Sound_Notify,
				Description = message,
				OccuringTime = DateTime.Now
			};
			_eventQueue.Enqueue(eventItem);
			_writerToLog.WriteEvent(eventItem);
		}

		private bool PeriodicRun()
		{
			EventItem obj;
			while (_eventQueue.TryDequeue(out obj))
			{
				try
				{
					if (_eventDB != null)
					{
						_eventDB.WriteEvent(obj);
					}
					if (_writerToMail != null)
					{
						_writerToMail.WriteEvent(obj);
					}
					if (_eventService != null)
					{
						_eventService.FireEvent(obj);
					}
					if (this.FireEvent != null)
					{
						this.FireEvent(obj);
					}
				}
				catch (Exception ex)
				{
					LOG.Error("Failed to post event", ex);
				}
			}
			return true;
		}

		public List<EventItem> GetAlarmEvent()
		{
			return _alarmQueue.ToList();
		}

		public void ClearAlarmEvent()
		{
			_alarmQueue.Clear();
		}

		public List<EventItem> QueryDBEvent(string sql)
		{
			return _eventDB.QueryDBEvent(sql);
		}

		public void Subscribe(EventItem item)
		{
			if (!_eventDic.ContainsKey(item.EventEnum))
			{
				_eventDic[item.EventEnum] = item;
				if (item is AlarmEventItem)
				{
					_alarms.Add(item as AlarmEventItem);
				}
			}
		}

		public void PostInfoLog(string module, string message)
		{
			WriteEvent(module, "INFORMATION_EVENT", message);
		}

		public void PostWarningLog(string module, string message)
		{
			WriteEvent(module, "WARNING_EVENT", message);
		}

		public void PostAlarmLog(string module, string message)
		{
			WriteEvent(module, "ALARM_EVENT", message);
		}
	}
}
