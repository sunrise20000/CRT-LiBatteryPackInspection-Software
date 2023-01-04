using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using SicRT.Scheduler;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Equipments;
using SicRT.Equipments.Systems;
using Mainframe.Cassettes;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerCassette : SchedulerModule
    {
        public override bool IsAvailable
        {
            get { return _cass.IsOnline; }

        }
        public override bool IsOnline
        {
            get { return _cass.IsOnline; }

        }
        public override bool IsError
        {
            get { return _cass.IsError; }

        }

        private CassetteModule _cass = null;

        private int _entityTaskToken = (int)FSM_MSG.NONE;

        public SchedulerCassette(ModuleName module) : base(module.ToString())
        {
            _module = module.ToString();
            _cass = Singleton<EquipmentManager>.Instance.Modules[module] as CassetteModule;
        }

        public bool Monitor()
        {
            return true;
        }

        public override bool IsReadyForPlace(ModuleName robot, int slot)
        {
            return WaferManager.Instance.CheckNoWafer(_cass.Module, slot) && WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(_cass.Module), slot);
        }

        public override bool IsReadyForPick(ModuleName robot, int slot)
        {
            return true;
        }


        public bool CheckTaskDone()
        {
            var taskSucceed = false;
            switch (_task)
            {
                case TaskType.None:
                    taskSucceed = true;
                    break;
            }

            return SuperCheckTaskDone(taskSucceed, _cass.IsIdle | _cass.IsError);
        }
    }
}