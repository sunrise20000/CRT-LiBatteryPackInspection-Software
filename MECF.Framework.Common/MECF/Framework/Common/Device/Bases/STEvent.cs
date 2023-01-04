using System.Xml.Serialization;

namespace MECF.Framework.Common.Device.Bases
{
	public class STEvent
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute]
		public string Red { get; set; }

		[XmlAttribute]
		public string Yellow { get; set; }

		[XmlAttribute]
		public string Green { get; set; }

		[XmlAttribute]
		public string Blue { get; set; }

		[XmlAttribute]
		public string Buzzer { get; set; }

		[XmlAttribute]
		public string White { get; set; }

		[XmlAttribute]
		public string Buzzer1 { get; set; }

		[XmlAttribute]
		public string Buzzer2 { get; set; }

		[XmlAttribute]
		public string Buzzer3 { get; set; }

		[XmlAttribute]
		public string Buzzer4 { get; set; }

		[XmlAttribute]
		public string Buzzer5 { get; set; }
	}
}
