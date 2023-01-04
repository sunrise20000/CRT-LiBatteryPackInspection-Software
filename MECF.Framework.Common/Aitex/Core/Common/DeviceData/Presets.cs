using System;

namespace Aitex.Core.Common.DeviceData
{
	[Serializable]
	public struct Presets
	{
		public byte PreNo;

		public byte TraSum;

		public float LoadData;

		public float TuneData;

		public float[,] TraData;

		public void Init()
		{
			TraData = new float[3, 2];
			PreNo = 0;
			TraSum = 0;
			LoadData = 0f;
			TuneData = 0f;
		}
	}
}
