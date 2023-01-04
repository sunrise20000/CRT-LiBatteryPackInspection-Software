using System.ComponentModel;

namespace Aitex.Sorter.Common
{
	public enum SorterRecipeType
	{
		[Description("Transfer 1 To 1")]
		Transfer1To1 = 0,
		[Description("Transfer N To 1")]
		TransferNTo1 = 1,
		[Description("Transfer N To N")]
		TransferNToN = 2,
		Pack = 3,
		Order = 4,
		Align = 5,
		[Description("Read Wafer Id")]
		ReadWaferId = 6,
		[Description("Host Usage")]
		HostNToN = 7
	}
}
