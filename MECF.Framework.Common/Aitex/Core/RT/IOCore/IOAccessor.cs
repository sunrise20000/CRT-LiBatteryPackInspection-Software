#define DEBUG
using System.Diagnostics;

namespace Aitex.Core.RT.IOCore
{
	public class IOAccessor<T>
	{
		protected string name;

		protected string addr;

		protected int index;

		protected T[] values;

		protected SetValue<T> setter = null;

		protected GetValue<T> getter = null;

		public T Value
		{
			get
			{
				return getter(index);
			}
			set
			{
				setter(index, value);
			}
		}

		public T[] Buffer => values;

		public string Name => name;

		public int Index => index;

		public int BlockOffset { get; set; }

		public string Addr
		{
			get
			{
				return addr;
			}
			set
			{
				addr = value;
			}
		}

		public string Provider { get; set; }

		public bool Visible { get; set; }

		public string Description { get; set; }

		public string StringIndex => name.Substring(0, 2) + "-" + index;

		public int IoTableIndex { get; set; }

		public IOAccessor(string name, int index, T[] values)
		{
			this.name = name;
			this.index = index;
			this.values = values;
			getter = GetValue;
			setter = SetValue;
		}

		private T GetValue(int index)
		{
			Debug.Assert(values != null && index >= 0 && index < values.Length);
			return values[index];
		}

		private void SetValue(int index, T value)
		{
			Debug.Assert(values != null && index >= 0 && index < values.Length);
			values[index] = value;
		}
	}
}
