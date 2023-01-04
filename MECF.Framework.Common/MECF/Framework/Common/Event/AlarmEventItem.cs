using System;
using System.Runtime.Serialization;
using Aitex.Core.RT.Event;

namespace MECF.Framework.Common.Event
{
	[Serializable]
	[DataContract]
	public class AlarmEventItem : EventItem
	{
		private IAlarmHandler _alarmHandler;

		private bool _ignoreAlarm;

		public Func<bool> ResetChecker { get; set; }

		public AlarmEventItem(string source, string name, string description, Func<bool> resetChecker, IAlarmHandler handler)
			: base(source, name, description, EventLevel.Alarm, EventType.EventUI_Notify)
		{
			ResetChecker = resetChecker;
			base.IsAcknowledged = true;
			_alarmHandler = handler;
		}

		public void SetIgnoreError(bool ignore)
		{
			if (_ignoreAlarm == ignore)
			{
				return;
			}
			_ignoreAlarm = ignore;
			if (ignore)
			{
				EV.PostWarningLog(base.Source, base.Source + " " + base.EventEnum + " error will be ignored");
				if (base.IsTriggered)
				{
					base.IsAcknowledged = true;
					if (_alarmHandler != null)
					{
						_alarmHandler.AlarmStateChanged(this);
					}
				}
			}
			else
			{
				Reset();
			}
		}

		public void Reset()
		{
			if (!_ignoreAlarm && base.IsTriggered && (ResetChecker == null || ResetChecker()))
			{
				EV.PostInfoLog(base.Source, base.Source + " " + base.EventEnum + " is cleared");
				base.IsAcknowledged = true;
				if (_alarmHandler != null)
				{
					_alarmHandler.AlarmStateChanged(this);
				}
			}
		}

		public void Set()
		{
			Set(null);
		}

		public void Set(string error)
		{
			if (!_ignoreAlarm && !base.IsTriggered)
			{
				if (!string.IsNullOrEmpty(error))
				{
					base.Description = error;
				}
				base.IsAcknowledged = false;
				base.OccuringTime = DateTime.Now;
				if (_alarmHandler != null)
				{
					_alarmHandler.AlarmStateChanged(this);
				}
			}
		}
	}
}
