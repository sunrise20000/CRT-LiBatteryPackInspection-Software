using System;
using System.Xml.Serialization;

namespace Aitex.Core.RT.IOCore
{
	[Serializable]
	public class AO_ITEM
	{
		[XmlAttribute]
		public int Index;

		[XmlAttribute]
		public string Name = " ";

		[XmlAttribute]
		public string Addr;

		[XmlAttribute]
		public string Description = "";
	}
}
