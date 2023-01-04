using System;
using System.Runtime.Serialization;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Event
{
	[Serializable]
	[DataContract]
	public class EventItem
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string EventEnum { get; set; }

		[DataMember]
		public EventType Type { get; set; }

		[DataMember]
		public EventLevel Level { get; set; }

		[DataMember]
		public string Source { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public DateTime OccuringTime { get; set; }

		[DataMember]
		public bool IsAcknowledged { get; set; }

		[DataMember]
		public string Explaination { get; set; }

		[DataMember]
		public string Solution { get; set; }

		[DataMember]
		public string GlobalDescription_en { get; set; }

		[DataMember]
		public string GlobalDescription_zh { get; set; }

		[DataMember]
		public SerializableDictionary<string, string> DVID { get; set; }

		public SerializableDictionary<string, object> ObjDVID { get; set; }

		public bool IsTriggered => !IsAcknowledged;

		public EventItem Clone()
		{
			EventItem eventItem = new EventItem();
			eventItem.Description = Description;
			eventItem.EventEnum = EventEnum;
			eventItem.Explaination = Explaination;
			eventItem.Id = Id;
			eventItem.IsAcknowledged = IsAcknowledged;
			eventItem.Level = Level;
			eventItem.OccuringTime = OccuringTime;
			eventItem.Solution = Solution;
			eventItem.Source = Source;
			eventItem.Type = Type;
			eventItem.GlobalDescription_en = GlobalDescription_en;
			eventItem.GlobalDescription_zh = GlobalDescription_zh;
			eventItem.DVID = DVID;
			eventItem.ObjDVID = ObjDVID;
			return eventItem;
		}

		public EventItem Clone(SerializableDictionary<string, object> objDvid)
		{
			EventItem eventItem = new EventItem();
			eventItem.Description = Description;
			eventItem.EventEnum = EventEnum;
			eventItem.Explaination = Explaination;
			eventItem.Id = Id;
			eventItem.IsAcknowledged = IsAcknowledged;
			eventItem.Level = Level;
			eventItem.OccuringTime = OccuringTime;
			eventItem.Solution = Solution;
			eventItem.Source = Source;
			eventItem.Type = Type;
			eventItem.GlobalDescription_en = GlobalDescription_en;
			eventItem.GlobalDescription_zh = GlobalDescription_zh;
			eventItem.DVID = DVID;
			eventItem.ObjDVID = objDvid;
			return eventItem;
		}

		public EventItem()
		{
		}

		public EventItem(string name)
			: this(0, "System", name, "", EventLevel.Information, EventType.EventUI_Notify)
		{
		}

		public EventItem(string name, string description)
			: this(0, "System", name, description, EventLevel.Information, EventType.EventUI_Notify)
		{
		}

		public EventItem(string source, string name, string description)
			: this(0, source, name, description, EventLevel.Information, EventType.EventUI_Notify)
		{
		}

		public EventItem(string name, EventType type, EventLevel level)
			: this(0, "System", name, "", level, type)
		{
		}

		public EventItem(string source, string name, string description, EventLevel level, EventType type)
			: this(0, source, name, description, level, type)
		{
		}

		public EventItem(int id, string source, string name, string description, EventLevel level, EventType type)
		{
			EventEnum = name;
			Type = type;
			Level = level;
			Source = source;
			Description = description;
			Id = id;
		}
	}
}
