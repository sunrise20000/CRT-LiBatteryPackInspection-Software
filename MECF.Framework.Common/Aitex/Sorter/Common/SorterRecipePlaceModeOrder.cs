using System.ComponentModel;

namespace Aitex.Sorter.Common
{
	public enum SorterRecipePlaceModeOrder
	{
		Forward = 0,
		[Description("Forward Pack")]
		ForwardPack = 1,
		Reverse = 2,
		[Description("Reverse Pack")]
		ReversePack = 3
	}
}
