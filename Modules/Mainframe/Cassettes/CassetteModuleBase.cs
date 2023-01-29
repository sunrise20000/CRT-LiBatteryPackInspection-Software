using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.Core;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;

namespace Mainframe.Cassettes
{
    public abstract class CassetteModuleBase : OfflineTimeoutNotifiableModuleBase, ITransferTarget, IModuleDevice
    {
        private int _slot = 1;

        public CassetteModuleBase(int slot)
        {
            _slot = slot;
        }

        public override bool Initialize()
        {
            if (Module == ModuleName.CassBL.ToString())
            {
                WaferManager.Instance.SubscribeLocation(Module, _slot, false, true);
            }
            else
            {
                WaferManager.Instance.SubscribeLocation(Module, _slot, true, false);
            }
            CarrierManager.Instance.SubscribeLocation(Module, 1);

            return base.Initialize();
        }

        public abstract bool IsIdle { get; }

        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }
        public abstract bool Home(out string reason);
        public abstract void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForMap(ModuleName robot, Hand blade, out string reason);
        public abstract bool CheckAcked(int entityTaskToken);

       
    }
}
