using System;
using System.Xml.Serialization;

namespace Aitex.Core.RT.IOCore
{
	[Serializable]
	public class DO_ITEM
	{
		[XmlAttribute]
		public int Index;

		[XmlAttribute]
		public string Addr;

		[XmlAttribute]
		public string Name = " ";

		[XmlAttribute]
		public string Description = "";
	}
}
