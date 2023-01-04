using System.Xml.Serialization;

namespace Aitex.Core.Util
{
	public class F_TRIG
	{
		public bool CLK
		{
			get
			{
				return M;
			}
			set
			{
				if (M != value && !value)
				{
					Q = true;
				}
				else
				{
					Q = false;
				}
				M = value;
			}
		}

		public bool RST
		{
			set
			{
				Q = false;
				M = true;
			}
		}

		[XmlIgnore]
		public bool Q { get; private set; }

		[XmlIgnore]
		public bool M { get; private set; }

		public F_TRIG()
		{
			Q = false;
			M = true;
		}
	}
}
