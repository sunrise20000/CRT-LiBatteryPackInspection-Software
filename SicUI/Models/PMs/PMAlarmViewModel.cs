using System.Collections.Generic;
using Aitex.Core.Common.DeviceData;
using OpenSEMI.ClientBase;
using System;
using Aitex.Core.Utilities;
using Aitex.Core.UI.View.Common;
using System.Windows.Media;
using MECF.Framework.UI.Client.ClientBase;
using Aitex.Core.RT.Event;
using System.Linq;

namespace SicUI.Models.PMs
{
    public class PMAlarmViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        public bool IsPermission { get => this.Permission == 3; }

        [IgnorePropertyChange]
        public List<AlarmItem> AlarmEvents { get; set; }


        public PMAlarmViewModel()
        {
            Subscribe("System.LiveAlarmEvent");
        }

        protected override void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
        {
            if (data.ContainsKey("System.LiveAlarmEvent"))
                UpdateAlarmEvent((List<EventItem>)data["System.LiveAlarmEvent"]);
        }

        public void UpdateAlarmEvent(List<EventItem> evItems)
        {
            var alarmEvents = new List<AlarmItem>();
            foreach (EventItem item in evItems)
            {
                if (item.Source != SystemName)
                    continue;

                var it = new AlarmItem()
                {
                    Type = item.Level == EventLevel.Alarm ? "Alarm" : (item.Level == EventLevel.Information ? "Info" : "Warning"),
                    OccuringTime = item.OccuringTime.ToString(),//item.OccuringTime.ToString("HH:mm:ss"),
                    Description = item.Description,
                    EventEnum = item.EventEnum,
                    EventId = item.Id,
                    Explaination = item.Explaination,
                    Solution = item.Solution,
                };
                switch (item.Level)
                {
                    case EventLevel.Alarm: it.TextColor = Brushes.Red; break;
                    case EventLevel.Warning: it.TextColor = Brushes.Yellow; break;
                    default: it.TextColor = Brushes.White; break;
                }
                alarmEvents.Add(it);
            }
            if (AlarmEvents == null || (alarmEvents.Count != AlarmEvents.Count))
            {
                AlarmEvents = alarmEvents;
            }
            else
            {
                bool isEqual = true;
                if (alarmEvents.Count == AlarmEvents.Count)
                {
                    for (int i = 0; i < alarmEvents.Count; i++)
                    {
                        if (!alarmEvents[i].IsEqualTo(AlarmEvents[i]))
                        {
                            isEqual = false;
                            break;
                        }
                    }
                }
                if (!isEqual)
                    AlarmEvents = alarmEvents;
            }

 			AlarmEvents = AlarmEvents.OrderBy(p=>p.OccuringTime).ToList();
            NotifyOfPropertyChange("AlarmEvents");
        }
    }
}
