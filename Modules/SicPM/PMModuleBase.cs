using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using System.Collections.Generic;
using MECF.Framework.RT.Core;

namespace SicPM
{
    public abstract class PMModuleBase : OfflineTimeoutNotifiableModuleBase, ITransferTarget, IModuleDevice
    {
        private int _slot = 1;
        public PMModuleBase(int slot)
        {
            _slot = slot;
        }

        public override bool Initialize()
        {
            WaferManager.Instance.SubscribeLocation(Module, _slot);

            return base.Initialize();
        }

        public abstract double ChamberPressure { get; }

        public abstract bool IsIdle { get; }

        public abstract bool IsProcessIdle { get; }

        public abstract bool CheckAcked(int entityTaskToken);

        //Initialize
        public abstract bool Home();

        //Transfer
        public abstract bool IsPrepareTransferReady(ModuleName robot, EnumTransferType pick, int slot);
        public abstract int InvokePrepareTransfer(ModuleName robot, EnumTransferType type, int slot);
        public abstract bool InvokeCheckHeaterDisable();
        public abstract int InvokeSetHeatDisable();

        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }

        public abstract bool IsService { get; }

        public abstract bool Home(out string reason);

        //ITransferTarget
        public abstract bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);

        //Process
        public abstract int InvokeProcess(string recipeName, bool isCleanRecipe, bool withWafer);
        public abstract int InvokeCleanProcess(string recipeName);
        public abstract bool IsProcessed();

        //Pump
        public abstract bool PreparePump(out string reason);
        public abstract bool CheckPreparePump();
        public abstract bool SlowPump(int tvPosition, out string reason);
        public abstract bool FastPump(int tvPosition, out string reason);
        public abstract bool TurnOnPump(out string reason);
        public abstract bool CheckPumpIsOn();
        public abstract bool ShutDownPump(out string reason);
        public abstract bool AbortPump();

        //Vent
        public abstract bool PrepareVent(out string reason);
        public abstract bool CheckPrepareVent();
        public abstract bool Vent(out string reason);
        public abstract bool StopVent(out string reason);

        public abstract bool CheckSlitValveClose();
 
        //Lid
        public abstract bool CheckLidClosed();


        // PreProcess
        public abstract bool CheckPreProcessCondition(Dictionary<string, string> recipeCommands, out string reason);

        // 2704
        public abstract bool CloseHeaterEnable(out string reason);

        public abstract bool EnableHeater(bool enable, out string reason);

        public abstract bool CheckHeaterEnable();

        public abstract bool CheckPlacetoPMTemp();

        public abstract bool SetRotationEnable(bool enable, out string reason);

        public abstract bool CheckRotationEnable();
    }
}
