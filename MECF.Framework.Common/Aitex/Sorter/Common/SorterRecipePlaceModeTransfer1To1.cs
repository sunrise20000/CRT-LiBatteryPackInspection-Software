using System.ComponentModel;

namespace Aitex.Sorter.Common
{
	public enum SorterRecipePlaceModeTransfer1To1
	{
		[Description("From Bottom")]
		FromBottom = 0,
		[Description("From Top")]
		FromTop = 1,
		[Description("Same Slot")]
		SameSlot = 2,
		[Description("Odd Slot From Bottom")]
		OddFromBotton = 3,
		[Description("Odd Slot From Top")]
		OddFromTop = 4,
		[Description("Even Slot From Bottom")]
		EvenFromBotton = 5,
		[Description("Even Slot From Top")]
		EvenFromTop = 6,
		[Description("Identify Slot by laser mark")]
		IdentifySlotByLaserMark = 7,
		[Description("To Opposite Postion")]
		ToOppositePosition = 8,
		[Description("To Fixed Slot")]
		ToFixedSlot = 9
	}
}
