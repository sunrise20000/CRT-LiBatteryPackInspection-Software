using System;

namespace Aitex.Sorter.Common
{
	[Flags]
	public enum MoveOption
	{
		None = 0,
		Align = 1,
		ReadID = 2,
		ReadID2 = 4,
		Reader1 = 8,
		Reader2 = 0x10,
		LoadLock1 = 0x20,
		LoadLock2 = 0x40,
		LoadLock3 = 0x80,
		LoadLock4 = 0x100,
		Buffer = 0x200,
		Turnover = 0x400,
		LoadLock5 = 0x800,
		LoadLock6 = 0x1000,
		LoadLock7 = 0x2000,
		LoadLock8 = 0x4000,
		CoolingBuffer1 = 0x8000,
		CoolingBuffer2 = 0x10000,
		Aligner1 = 0x20000,
		Aligner2 = 0x40000
	}
}
