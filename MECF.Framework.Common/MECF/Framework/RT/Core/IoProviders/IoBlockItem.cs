using System;

namespace MECF.Framework.RT.Core.IoProviders
{
	public class IoBlockItem
	{
		public int Offset { get; set; }

		public int Size { get; set; }

		public IoType Type { get; set; }

		public Type AIOType { get; set; }
	}
}
