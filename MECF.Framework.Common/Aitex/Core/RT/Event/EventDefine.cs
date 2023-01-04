using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Aitex.Core.RT.Event
{
	[Serializable]
	public class EventDefine
	{
		[XmlElement("EventDefinition")]
		public List<EventItem> Items { get; set; }
	}
}
