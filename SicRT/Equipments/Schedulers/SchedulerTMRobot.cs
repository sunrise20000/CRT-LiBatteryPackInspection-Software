using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using SicRT.Modules;
using SicRT.Modules.Schedulers;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Equipments.Systems;
using Mainframe.TMs;
using Aitex.Core.RT.Fsm;

namespace SicRT.Scheduler
{
	public class SchedulerTMRobot : SchedulerModule
	{
        private int _fsmTask = (int)FSM_MSG.NONE;

        public override bool IsAvailable
		{
			get { return _tmRobot.IsIdle && _tmRobot.IsOnline && CheckTaskDone(); }
		}

		public override bool IsOnline
		{
			get { return _tmRobot.IsOnline; }

		}
		public override bool IsError
		{
			get { return _tmRobot.IsError; }

		}

		private TMModuleBase _tmRobot = null;

		private Hand _taskHand;

		private ModuleName _target;
        public ModuleName Target => _target;

		public bool IsInPumping { get => ((_task == TaskType.Pick || _task == TaskType.Place) && (_target == ModuleName.UnLoad || _target == ModuleName.LoadLock || _target == ModuleName.Load)); }

		public SchedulerTMRobot() : base(ModuleName.TMRobot.ToString())
		{
			_tmRobot = Singleton<EquipmentManager>.Instance.Modules[ModuleName.TM] as TMModuleBase;
		}


		public bool IsReadyForPick(Hand blade)
		{
			return WaferManager.Instance.CheckNoWafer(ModuleName.TMRobot, (int)blade);
		}

		public bool IsReadyForPlace(Hand blade)
		{
			return WaferManager.Instance.CheckHasWafer(ModuleName.TMRobot, (int)blade);
		}

        public bool Purge(int loopCount, int pumpDelayInSec)
        {
            if (loopCount <= 0)
            {
                LogTaskStart(_task, $"[{Module}] Purge ignored");
                return true;
            }

            _fsmTask = _tmRobot.Purge(loopCount, pumpDelayInSec);
            if (_fsmTask != (int)FSM_MSG.NONE)
            {
                _task = TaskType.Purge;
                LogTaskStart(_task, $"[{Module}] Start to Purge");
            }
			
            return _fsmTask != (int)FSM_MSG.NONE;
        }


		public bool Pick(ModuleName target, int slot, Hand hand)
		{
			if (_tmRobot.Pick(target, hand, slot, out var reason))
			{
				_task = TaskType.Pick;
				_taskHand = hand;
				_target = target;
				LogTaskStart(_task, $"{target}.{slot + 1}=>{Module}.{hand}");
			}
			return true;
		}

		public bool Place(ModuleName target, int slot, Hand hand)
		{
			if (_tmRobot.Place(target, hand, slot, out var reason))
			{
				_task = TaskType.Place;
				_taskHand = hand;
				_target = target;
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
				case TaskType.Purge:
                    taskSucceed = _tmRobot.CheckAcked(_fsmTask);
                    break;
				case TaskType.Pick:
                    taskSucceed = WaferManager.Instance.CheckHasTray(ModuleName.TMRobot, (int)_taskHand);
					break;
				case TaskType.Place:
                    taskSucceed = WaferManager.Instance.CheckNoTray(ModuleName.TMRobot, (int)_taskHand);
					break;
			}
			
            return SuperCheckTaskDone(taskSucceed, _tmRobot.IsIdle | _tmRobot.IsError);
		}
	}


}
