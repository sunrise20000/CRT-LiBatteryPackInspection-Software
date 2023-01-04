using System;

namespace Aitex.Core.RT.IOCore
{
	[Serializable]
	public class IO_DEFINE
	{
		public DI_ITEM[] Dig_In;

		public DO_ITEM[] Dig_Out;

		public AI_ITEM[] Ana_In;

		public AO_ITEM[] Ana_Out;

		public IO_DEFINE()
		{
			Dig_In = new DI_ITEM[64];
			Dig_Out = new DO_ITEM[64];
			Ana_In = new AI_ITEM[64];
			Ana_Out = new AO_ITEM[64];
			for (int i = 0; i < 64; i++)
			{
				Dig_In[i] = new DI_ITEM
				{
					Index = i
				};
				Dig_Out[i] = new DO_ITEM
				{
					Index = i
				};
			}
			for (int j = 0; j < 64; j++)
			{
				Ana_In[j] = new AI_ITEM
				{
					Index = j
				};
				Ana_Out[j] = new AO_ITEM
				{
					Index = j
				};
			}
		}
	}
}
