namespace Aitex.Core.RT.PLC
{
	public interface IDataBuffer<T, V> where T : struct where V : struct
	{
		T Input { get; set; }

		V Output { get; set; }

		void UpdateDI(bool[] values);

		void UpdateAI(float[] values);

		void UpdateDO(bool[] values);

		void UpdateAO(float[] values);
	}
}
