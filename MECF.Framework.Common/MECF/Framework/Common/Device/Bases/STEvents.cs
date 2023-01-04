using System.Collections.Generic;
using System.Xml.Serialization;

namespace MECF.Framework.Common.Device.Bases
{
	public class STEvents
	{
		[XmlElement(ElementName = "STEvent")]
		public List<STEvent> Events;
	}
}
