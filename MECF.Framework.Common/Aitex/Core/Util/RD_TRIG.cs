using System.Xml.Serialization;

namespace Aitex.Core.Util
{
	public class RD_TRIG
	{
		public bool CLK
		{
			get
			{
				return M;
			}
			set
			{
				if (M != value)
				{
					R = value;
					T = !value;
				}
				else
				{
					R = false;
					T = false;
				}
				M = value;
			}
		}

		public bool RST
		{
			set
			{
				R = false;
				T = false;
				M = false;
			}
		}

		[XmlIgnore]
		public bool R { get; private set; }

		[XmlIgnore]
		public bool T { get; private set; }

		[XmlIgnore]
		public bool M { get; private set; }

		public RD_TRIG()
		{
			R = false;
			T = false;
			M = false;
		}
	}
}
