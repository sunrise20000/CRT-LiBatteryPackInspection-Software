using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Fsm;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using System;
using MECF.Framework.RT.Core;

namespace Mainframe.EFEMs
{
    public abstract class EFEMModuleBase : OfflineTimeoutNotifiableModuleBase, IModuleDevice
    {
        private int _slot = 1;

        public EFEMModuleBase(int slot)
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

    }
}

