namespace Aitex.Core.RT.IOCore
{
	public class DIAccessor : IOAccessor<bool>
	{
		private IOAccessor<bool> rawAccessor;

		public bool RawData
		{
			get
			{
				return rawAccessor.Value;
			}
			set
			{
				rawAccessor.Value = value;
			}
		}

		public DIAccessor(string name, int index, bool[] values, bool[] raws)
			: base(name, index, values)
		{
			rawAccessor = new IOAccessor<bool>(name, index, raws);
		}
	}
}
