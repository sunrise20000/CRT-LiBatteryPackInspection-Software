using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using SicRT.Equipments.Systems;
using Mainframe.Aligners;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerFeeder : SchedulerModule
    {
        public override bool IsAvailable => _feeder.IsIdle && _feeder.IsOnline && CheckTaskDone();

        public override bool IsOnline => _feeder.IsOnline;

        public override bool IsError => _feeder.IsError;

        public bool HasFed { get; private set; }


        private FeederModuleBase _feeder = null;

        private int _entityTaskToken = (int)FSM_MSG.NONE;

        public SchedulerFeeder(ModuleName moduleName) : base(moduleName)
        {
            _feeder = Singleton<EquipmentManager>.Instance.Modules[moduleName] as FeederModule;
        }

        public bool Monitor()
        {
            return true;
        }

        public bool Feed()
        {
            _task = TaskType.Feeding;
            _entityTaskToken = _feeder.InvokeFeed();
            LogTaskStart(_task, $"{Module} Feeding");
            HasFed = true;
            return _entityTaskToken != (int)FSM_MSG.NONE;
        }

        public void ResetFedStatus()
        {
            HasFed = false;
        }

        public bool CheckTaskDone()
        {
            var taskSucceed = false;
            switch (_task)
            {
                case TaskType.None:
                    taskSucceed = true;
                    break;
                case TaskType.Feeding:
                    taskSucceed = _feeder.CheckAcked(_entityTaskToken);
                    break;
            }

            return SuperCheckTaskDone(taskSucceed, _feeder.IsIdle | _feeder.IsError);
        }

        public override bool IsReadyForPick(ModuleName robot, int slot)
        {
            return true;
        }
    }
}