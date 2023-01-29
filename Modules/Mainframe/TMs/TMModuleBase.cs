using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.Core;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;


namespace Mainframe.TMs
{
    public abstract class TMModuleBase : OfflineTimeoutNotifiableModuleBase, ITransferRobot, IModuleDevice
    {
        private int _slot = 1;

        public TMModuleBase(int slot)
        {
            _slot = slot;
        }

        public override bool Initialize()
        {
            WaferManager.Instance.SubscribeLocation(Module, _slot);

            return base.Initialize();
        }

        public abstract bool IsIdle { get; }

        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }
        public abstract bool Home(out string reason);
        

        public abstract bool CheckAcked(int entityTaskToken);
        public virtual bool Pick(ModuleName target, Hand blade, int targetSlot, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool Place(ModuleName target, Hand blade, int targetSlot, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool PickAndPlace(ModuleName pickTarget, Hand pickHand, int pickSlot, ModuleName placeTarget, Hand placeHand,
            int placeSlot, out string reason)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool Goto(ModuleName target, Hand blade, int targetSlot, out string reason)
        {
            throw new System.NotImplementedException();
        }
    }
}
