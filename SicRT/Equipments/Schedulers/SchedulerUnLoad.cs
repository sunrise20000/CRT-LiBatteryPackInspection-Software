using System.Diagnostics;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using Mainframe.LLs;
using SicRT.Equipments.Systems;
using SicRT.Modules.Schedulers;
using Mainframe.UnLoads;

namespace SicRT.Scheduler
{
    public class SchedulerUnLoad : SchedulerModule
    {
        public override bool IsAvailable
        {
            get
            {
                lock (SyncRoot)
                {
                    return _unL.IsIdle && _unL.IsOnline && CheckTaskDone() && _unL.CheckSlitValveClosed();
                }
            }
        }

        public override bool IsOnline
        {
            get
            {
                lock (SyncRoot)
                {
                    lock (SyncRoot)
                    {
                        return _unL.IsOnline;
                    }
                }
            }
        }

        public override bool IsError
        {
            get
            {
                lock (SyncRoot)
                {
                    return _unL.IsError;
                }
            }
        }

        private UnLoadModuleBase _unL = null;

        private ModuleName _taskRobot;
        private int _taskSlot;
        private int _entityTaskToken = (int)FSM_MSG.NONE;
        private bool _separated = false;
        private bool _purgedBefTrayPicking = false;
        private bool _purgedBefWaferPicking = false;
        private bool _purgedAfWaferPicked;
        private bool _coolingCompleted = false;
        private Stopwatch _swCooling = new Stopwatch();
        

        public bool IsInPumping
        {
            get
            {
                lock (SyncRoot)
                {
                    return _task == TaskType.Pump || _task == TaskType.Purge || _task == TaskType.PrepareTransfer;
                }
            }
        }

        public int LastAfAtmPurgeCount { get; set; }

        public int LastAfAtmPurgeDelay { get; set; }

        public bool HasWaferPickedByWaferRobot { get; set; }

        public SchedulerUnLoad() : base(ModuleName.UnLoad.ToString())
        {
            lock (SyncRoot)
            {
                _unL = Singleton<EquipmentManager>.Instance.Modules[ModuleName.UnLoad] as UnLoadModuleBase;
            }
        }

        public override bool PrepareTransfer(ModuleName robot, EnumTransferType type, int slot)
        {
            lock (SyncRoot)
            {
                _task = TaskType.PrepareTransfer;
                _taskRobot = robot;
                _taskSlot = slot;

                LogTaskStart(_task, $"{robot} {type} slot {slot + 1}");

                return _unL.PrepareTransfer(robot, Hand.Blade1, slot, type, out _);
            }
        }

        internal bool CheckAtAtm()
        {
            lock (SyncRoot)
            {
                SicLoadLock deviceLL = DEVICE.GetDevice<SicLoadLock>($"{_module}.{_module}");

                return deviceLL.CheckAtm();
            }
        }
        internal bool CheckAtVacuum()
        {
            lock (SyncRoot)
            {
                SicLoadLock deviceLL = DEVICE.GetDevice<SicLoadLock>($"{_module}.{_module}");

                return deviceLL.CheckVacuum();
            }
        }
        internal void SetJobStatue()
        {
            lock (SyncRoot)
            {
                SicLoadLock deviceLL = DEVICE.GetDevice<SicLoadLock>($"{_module}.{_module}");
                //deviceLL.SetJobDoneStatus();
            }
        }

        public override bool IsReadyForPick(ModuleName robot, int slot)
        {
            lock (SyncRoot)
            {
                if (robot == ModuleName.WaferRobot)
                {
                    return _unL.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Pick, out _)
                           && WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(_module), slot);
                }
                else if (robot == ModuleName.TMRobot)
                {
                    return _unL.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Pick, out _)
                           && WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(_module), slot);
                }

                return false;
            }
        }

        public override bool IsReadyForPlace(ModuleName robot, int slot)
        {
            lock (SyncRoot)
            {
                return _unL.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Place, out _)
                       && WaferManager.Instance.CheckNoWafer(ModuleHelper.Converter(_module), slot)
                       && WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(_module), slot);
            }
        }


        public bool Vent()
        {
            lock (SyncRoot)
            {
                _task = TaskType.Vent;

                _entityTaskToken = _unL.InvokeVent();

                LogTaskStart(_task, $"{Module} vent to ATM");

                return _entityTaskToken != (int)FSM_MSG.NONE;
            }
        }

        public bool Pump()
        {
            lock (SyncRoot)
            {
                _entityTaskToken = _unL.InvokePump();
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Pump;
                    LogTaskStart(_task, $"{Module} pump to Vaccum");
                    return true;
                }

                return false;
            }
        }

        public override bool CoolingAndPurge(bool coolingType, int coolingTime, int purgeLoopCount, int purgePumpDelay)
        {
            lock (SyncRoot)
            {
                // Wafer进入UnLoad后并不一定马上开始执行CoolingAndPurge，因此需要记录停留时间，这部分时间折算进Cooling时间，
                // 当开始CoolingAndPurge时，已等待的时间从Cooling时间里减去，已节省时间。
                _swCooling.Stop();
                EV.PostInfoLog(Module.ToString(), $"Wafer and Tray have cooled for {_swCooling.Elapsed.TotalSeconds:F0}s");
                var newCoolingTime = coolingTime - (int)_swCooling.Elapsed.TotalSeconds;
                _entityTaskToken = _unL.InvokeCoolingAndPurge(newCoolingTime,
                    purgeLoopCount, purgePumpDelay);
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.CoolingAndPurge;
                    _purgedBefTrayPicking = true;

                    LogTaskStart(_task, $"{Module} CoolingAndPurge for {newCoolingTime}s");
                }

                _coolingCompleted = _entityTaskToken != (int)FSM_MSG.NONE;

                return _entityTaskToken != (int)FSM_MSG.NONE;
            }
        }

        public override bool Cooling(bool coolingType, int coolingTime)
        {
            lock (SyncRoot)
            {
                _entityTaskToken = _unL.InvokeCooling(coolingTime);
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Cooling;
                    LogTaskStart(_task, $"{Module} cooling {coolingTime} seconds");
                }

                _coolingCompleted = _entityTaskToken != (int)FSM_MSG.NONE;

                return _entityTaskToken != (int)FSM_MSG.NONE;
            }
        }

        public bool PurgeBeforeTrayPicking(params object[] args)
        {
            lock (SyncRoot)
            {
                // 如果循环次数为0，则跳过此步骤。
                if (args is null || (args[0] is int cycle && cycle <= 0))
                {
                        LogTaskStart(_task,
                            $" Purge before tray picking was ignored since the cycle is zero");
                        _purgedBefTrayPicking = true;
                        return true;
                }


                _entityTaskToken = _unL.InvokePurge(args);
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Purge;
                    LogTaskStart(_task, $" Purge before tray picking");
                    _purgedBefTrayPicking = true;
                    return true;
                }

                return false;
            }
        }

        public bool PurgeBeforeWaferPicking(params object[] args)
        {
            lock (SyncRoot)
            {
                // 如果循环次数为0，则跳过此步骤。
                if (args is null || (args[0] is int cycle && cycle <= 0))
                {
                    LogTaskStart(_task,
                        $"[{Module}] Purge before wafer picking was ignored since the cycle is zero");
                    _purgedBefWaferPicking = true;
                    return true;
                }


                _entityTaskToken = _unL.InvokePurge(args);
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Purge;
                    LogTaskStart(_task, $" Purge before wafer picking");
                    _purgedBefWaferPicking = true;
                    return true;
                }

                return false;
            }
        }
        
        public bool PurgeAfterWaferPicked(params object[] args)
        {
            lock (SyncRoot)
            {
                // 如果循环次数为0，则跳过此步骤。
                if (args is null || (args[0] is int cycle && cycle <= 0))
                {
                    LogTaskStart(_task,
                        $" Purge after wafer picked was ignored since the cycle is zero");
                    _purgedAfWaferPicked = true;
                    HasWaferPickedByWaferRobot = false;

                    return true;
                }

                _entityTaskToken = _unL.InvokePurge(args);
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Purge;
                    LogTaskStart(_task, $" Purge after wafer picked");
                    _purgedAfWaferPicked = true;
                    HasWaferPickedByWaferRobot = false;
                    LastAfAtmPurgeCount = 0;
                    LastAfAtmPurgeDelay = 0;
                    return true;
                }

                return false;
            }
        }

        public bool Monitor()
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public bool GroupWaferTray()
        {
            lock (SyncRoot)
            {
                _entityTaskToken = _unL.InvokeGroupWaferTray();
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {

                    _task = TaskType.Group;
                    LogTaskStart(_task, $"{Module} start group wafer and tray");
                }

                return _entityTaskToken != (int)FSM_MSG.NONE;
            }
        }

        public bool SeparateWaferTray()
        {
            lock (SyncRoot)
            {
                _entityTaskToken = _unL.InvokeSeparateWaferTray();
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Separate;
                    LogTaskStart(_task, $"{Module} start separate wafer and tray");
                }

                _separated = _entityTaskToken != (int)FSM_MSG.NONE;
                return _entityTaskToken != (int)FSM_MSG.NONE;
            }
        }

        public void ResetPurgedAndSeparatedStatus()
        {
            lock (SyncRoot)
            {
                _coolingCompleted = false;
                _separated = false;
                _purgedBefTrayPicking = false;
                _purgedBefWaferPicking = false;
                _purgedAfWaferPicked = false;

                // 开始记录Wafer进入UnLoad后停留的时长
                _swCooling.Restart();
                EV.PostInfoLog(Module.ToString(), "Start to record cooling time");
            }
        }

        public bool CheckCoolingCompleted()
        {
            lock (SyncRoot)
            {
                return _coolingCompleted;
            }
        }

        public override bool CheckWaferTraySeparated()
        {
            lock (SyncRoot)
            {
                return _separated;
            }
        }

        public bool CheckPurgedBeforeTrayPicking()
        {
            lock (SyncRoot)
            {
                return _purgedBefTrayPicking;
            }
        }
        
        public bool CheckPurgedBeforeWaferPicking()
        {
            lock (SyncRoot)
            {
                return _purgedBefWaferPicking;
            }
        }
        
        public bool CheckPurgedAfterWaferPicked()
        {
            lock (SyncRoot)
            {
                return _purgedAfWaferPicked;
            }
        }

        public override bool CheckSlitValveClosed()
        {
            lock (SyncRoot)
            {
                return _unL.CheckSlitValveClosed();
            }
        }

        public int GetWaferPurgeCount(int slot, string whichPurge = "PurgeCount")
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(Module, slot))
                    return 0;

                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return 0;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return 0;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules.Contains(Module))
                    return 0;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter.ContainsKey(whichPurge))
                    return 0;

                if (int.TryParse(
                        wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter[whichPurge].ToString(),
                        out int purgeCount))
                {
                    return purgeCount;
                }

                return 0;
            }
        }

        public int GetWaferPumpDelayTime(int slot, string whichPumpDelay = "PumpDelayTime")
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(Module, slot))
                    return 0;

                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return 0;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return 0;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules.Contains(Module))
                    return 0;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter.ContainsKey(whichPumpDelay))
                    return 0;

                if (int.TryParse(
                        wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter[whichPumpDelay]
                            .ToString(), out int pumpDelayTime))
                {
                    return pumpDelayTime;
                }

                return 0;
            }
        }

        public string GetTaskRunning()
        {
            return _task.ToString();
        }

        public bool CheckTaskDone()
        {
            lock (SyncRoot)
            {
                var taskSucceed = false;
                switch (_task)
                {
                    case TaskType.None:
                        taskSucceed = true;
                        break;
                    case TaskType.PrepareTransfer:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Cooling:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.CoolingAndPurge:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Vent:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Pump:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Group:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Purge:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Separate:
                        taskSucceed = _unL.CheckAcked(_entityTaskToken);
                        break;
                }

                return SuperCheckTaskDone(taskSucceed, _unL.IsIdle | _unL.IsError);
            }
        }
    }
}
