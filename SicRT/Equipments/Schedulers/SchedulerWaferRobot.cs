using Aitex.Core.Util;
using Aitex.Sorter.Common;
using SicRT.Modules.Schedulers;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Equipments.Systems;
using Mainframe.EFEMs;

namespace SicRT.Scheduler
{
    public class SchedulerWaferRobot : SchedulerModule
    {
        public override bool IsAvailable
        {
            get { return _waferRobot.IsIdle && _waferRobot.IsOnline && CheckTaskDone(); }
        }

        public override bool IsOnline
        {
            get { return _waferRobot.IsOnline; }

        }
        public override bool IsError
        {
            get { return _waferRobot.IsError; }

        }

        public ModuleName? Target { get; private set; }

        private WaferRobotModule _waferRobot = null;

        private Hand _taskHand;

        private Hand _taskSwapPickHand;
        private Hand _taskSwapPlaceHand;

        public SchedulerWaferRobot() : base(ModuleName.WaferRobot.ToString())
        {
            _waferRobot = (Singleton<EquipmentManager>.Instance.Modules[ModuleName.WaferRobot] as WaferRobotModule);
        }


        public bool IsReadyForPick(Hand blade)
        {
            return WaferManager.Instance.CheckNoWafer(ModuleName.WaferRobot, (int)blade);
        }

        public bool IsReadyForPlace(Hand blade)
        {
            return WaferManager.Instance.CheckHasWafer(ModuleName.WaferRobot, (int)blade);
        }


        public bool Pick(ModuleName target, int slot, Hand hand)
        {
            if (_waferRobot.Pick(target, hand, slot))
            {
                Target = target;
                _task = TaskType.Pick;
                _taskHand = hand;
                LogTaskStart(_task, $"{target}.WaferRobot.{slot + 1}=>{Module}.{hand}");
            }
            return true;
        }

        public bool Place(ModuleName target, int slot, Hand hand)
        {
            if (_waferRobot.Place(target, hand, slot))
            {
                Target = target;
                _task = TaskType.Place;
                _taskHand = hand;
                LogTaskStart(_task, $"{Module}.WaferRobot.{hand}=>{target}.{slot + 1}");
            }
            return true;
        }

        public bool Map(ModuleName target)
        {
            if (_waferRobot.Map(target))
            {
                _task = TaskType.Map;
                LogTaskStart(_task, $"{Module}.WaferRobot Map");
            }
            return true;
        }

        public bool Goto(ModuleName chamber, int slot, Hand hand)
        {
            return true;
        }

        public bool Monitor()
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
                case TaskType.Pick:
                    taskSucceed = WaferManager.Instance.CheckHasWafer(ModuleName.WaferRobot, (int)_taskHand);
                    break;
                case TaskType.Place:
                    taskSucceed = WaferManager.Instance.CheckNoWafer(ModuleName.WaferRobot, (int)_taskHand);
                    
                    break;
            }

            return SuperCheckTaskDone(taskSucceed, _waferRobot.IsIdle | _waferRobot.IsError);
        }
    }


}
