namespace Aitex.Core.RT.IOCore
{
	public class AIAccessor : IOAccessor<short>
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
				_floatValues[index] = value;
			}
		}

		public AIAccessor(string name, int index, short[] values, float[] floatValues)
			: base(name, index, values)
		{
			_floatValues = floatValues;
		}
	}
}
