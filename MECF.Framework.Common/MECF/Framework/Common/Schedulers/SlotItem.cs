using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Schedulers
{
	public class SlotItem
	{
		public ModuleName Module { get; set; }

		public int Slot { get; set; }

		public SlotItem(ModuleName module, int slot)
		{
			Module = module;
			Slot = slot;
		}
	}
}
