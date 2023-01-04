using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Schedulers
{
	public class MoveItem
	{
		public ModuleName SourceModule;

		public int SourceSlot;

		public ModuleName DestinationModule;

		public int DestinationSlot;

		public Hand RobotHand;

		public MoveItem(ModuleName sourceModule, int sourceSlot, ModuleName destinationModule, int destinationSlot, Hand robotHand)
		{
			SourceModule = sourceModule;
			SourceSlot = sourceSlot;
			DestinationModule = destinationModule;
			DestinationSlot = destinationSlot;
			RobotHand = robotHand;
		}
	}
}
