using Aitex.Core.Util;
using Aitex.Sorter.Common;
using SicRT.Modules.Schedulers;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Equipments.Systems;
using Mainframe.EFEMs;

namespace SicRT.Scheduler
{
    public class SchedulerTrayRobot : SchedulerModule
    {
        public override bool IsAvailable
        {
            get { return _trayRobot.IsIdle && _trayRobot.IsOnline && CheckTaskDone(); }
        }

        public override bool IsOnline
        {
            get { return _trayRobot.IsOnline; }

        }
        public override bool IsError
        {
            get { return _trayRobot.IsError; }

        }

        private TrayRobotModule _trayRobot = null;

        private Hand _taskHand;
        public SchedulerTrayRobot() : base(ModuleName.TrayRobot.ToString())
        {
            _trayRobot = (Singleton<EquipmentManager>.Instance.Modules[ModuleName.TrayRobot] as TrayRobotModule);
        }


        public bool IsReadyForPick(Hand blade)
        {
            return WaferManager.Instance.CheckNoWafer(ModuleName.TrayRobot, (int)blade);
        }

        public bool IsReadyForPlace(Hand blade)
        {
            return WaferManager.Instance.CheckHasWafer(ModuleName.TrayRobot, (int)blade);
        }


        public bool Pick(ModuleName target, int slot, Hand hand)
        {            
            if (_trayRobot.Pick(target, hand, slot))
            {
                _task = TaskType.Pick;
                _taskHand = hand;
                LogTaskStart(_task, $"{target}.{slot + 1}=>{Module}.{hand}");
            }
            return true;
        }

        public bool Place(ModuleName target, int slot, Hand hand)
        {            
            if (_trayRobot.Place(target, hand, slot))
            {
                _task = TaskType.Place;
                _taskHand = hand;
                LogTaskStart(_task, $"{Module}.{hand}=>{target}.{slot + 1}");
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
                    taskSucceed = WaferManager.Instance.CheckHasTray(ModuleName.TrayRobot, (int)_taskHand);
                    
                    break;
                case TaskType.Place:
                    taskSucceed = WaferManager.Instance.CheckNoTray(ModuleName.TrayRobot, (int)_taskHand);
                    break;
            }

            return SuperCheckTaskDone(taskSucceed, _trayRobot.IsIdle | _trayRobot.IsError);
        }
    }


}
