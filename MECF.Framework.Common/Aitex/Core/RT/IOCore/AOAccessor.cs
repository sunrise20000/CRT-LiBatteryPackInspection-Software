using Aitex.Core.Util;
using MECF.Framework.RT.Core.IoProviders;

namespace Aitex.Core.RT.IOCore
{
	public class AOAccessor : IOAccessor<short>
	{
		protected float[] _floatValues;

		public float FloatValue
		{
			get
			{
				return _floatValues[index];
			}
			set
			{
				Singleton<IoProviderManager>.Instance.GetProvider(base.Provider)?.SetValueFloat(this, value);
				_floatValues[index] = value;
			}
		}

		public AOAccessor(string name, int index, short[] values, float[] floatValues)
			: base(name, index, values)
		{
			setter = SetValueSafe;
			_floatValues = floatValues;
		}

		private void SetValueSafe(int index, short value)
		{
			values[index] = value;
			Singleton<IoProviderManager>.Instance.GetProvider(base.Provider)?.SetValue(this, value);
		}
	}
}
