using System;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using Mainframe.LLs;
using SicRT.Equipments.Systems;
using SicRT.Modules.Schedulers;

namespace SicRT.Scheduler
{
    public class SchedulerLoadLock : SchedulerModule
    {

        #region Variables

        private LoadLockModuleBase _ll = null;
        private ModuleName _taskRobot;
        private int _taskSlot;
        private int _entityTaskToken = (int)FSM_MSG.NONE;

        #endregion

        #region Constructors

        public SchedulerLoadLock() : base(ModuleName.LoadLock.ToString())
        {
            _ll = Singleton<EquipmentManager>.Instance.Modules[ModuleName.LoadLock] as LoadLockModuleBase;
        }

        #endregion

        #region Properties

        public override bool IsAvailable
        {
            get { return _ll.IsIdle && _ll.IsOnline && CheckTaskDone() && _ll.CheckSlitValveClosed(); }
        }

        public override bool IsOnline
        {
            get { return _ll.IsOnline; }
        }

        public override bool IsError
        {
            get { return _ll.IsError; }
        }

        public bool IsInPumping =>
            _task == TaskType.Pump || _task == TaskType.Purge || _task == TaskType.PrepareTransfer;

        /// <summary>
        /// 是否执行Purge。
        /// <para>如果Tray来自TMRobot，放完Wafer马上Purge。</para>
        /// <para>如果Tray来自TrayCassette，放完Tray以后统一Purge。</para>
        /// </summary>
        public bool HasPurged { get; private set; }


        #endregion


        public override bool PrepareTransfer(ModuleName robot, EnumTransferType type, int slot)
        {
            lock (SyncRoot)
            {
                _task = TaskType.PrepareTransfer;
                _taskRobot = robot;
                _taskSlot = slot;

                LogTaskStart(_task, $"{robot} {type} slot {slot + 1}");
                return _ll.PrepareTransfer(robot, Hand.Blade1, slot, type, out _);
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
                    return _ll.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Pick, out _)
                           && WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(_module), slot);
                }
                else if (robot == ModuleName.TMRobot || robot == ModuleName.TrayRobot)
                {
                    return _ll.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Pick, out _)
                           && WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(_module), slot);
                }

                return false;
            }
        }

        public override bool IsReadyForPlace(ModuleName robot, int slot)
        {
            lock (SyncRoot)
            {
                if (robot == ModuleName.WaferRobot)
                {
                    return _ll.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Place, out _);
                }
                else if (robot == ModuleName.TMRobot || robot == ModuleName.TrayRobot)
                {
                    return _ll.CheckReadyForTransfer(robot, Hand.Blade1, slot, EnumTransferType.Place, out _)
                           && WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(_module), slot);
                }

                return false;
            }
        }


        /// <summary>
        /// 重置HasPurged和HasGrouped标记。
        /// </summary>
        public void ResetPurgedAndGroupedStatus()
        {
            lock (SyncRoot)
            {
                HasPurged = false;
            }
        }


        public bool Vent()
        {
            lock (SyncRoot)
            {
                _task = TaskType.Vent;

                _entityTaskToken = _ll.InvokeVent();

                LogTaskStart(_task, $"{Module} vent to ATM");

                return _entityTaskToken != (int)FSM_MSG.NONE;
            }
        }

        public bool Pump()
        {
            lock (SyncRoot)
            {
                _entityTaskToken = _ll.InvokePump();
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Pump;
                    LogTaskStart(_task, $"{Module} pump to Vaccum");
                    return true;
                }

                return false;
            }

        }

        /// <summary>
        /// Purge，次数由Sequence中的设定决定。
        ///<para></para>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Purge(params object[] args)
        {
            lock (SyncRoot)
            {
                // 如果循环次数为0，则跳过此步骤。
                if (args is null || (args[0] is int cycle && cycle <= 0))
                {
                    LogTaskStart(_task, " Purge was ignored since the cycle is zero");
                    HasPurged = true;
                    return true;
                }
                
                _entityTaskToken = _ll.InvokePurge(args);
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Purge;
                    LogTaskStart(_task, $"Purging");
                    HasPurged = true;
                    return true;
                }

                return false;
            }
        }

        public bool GroupWaferTray()
        {
            lock (SyncRoot)
            {
                _entityTaskToken = _ll.InvokeGroupWaferTray();
                if (_entityTaskToken != (int)FSM_MSG.NONE)
                {
                    _task = TaskType.Group;
                    LogTaskStart(_task, $"{Module} start group wafer and tray");
                    return true;
                }

                return false;
            }
        }

        public bool SeparateWaferTray()
        {
            throw new NotSupportedException();
        }


        public int? GetWaferPurgeCount(int slot)
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(Module, slot))
                    return null;

                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return null;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return null;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules
                        .Any(m => m == ModuleName.LoadLock || m == ModuleName.Load))
                    return null;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter.ContainsKey("PurgeCount"))
                    return null;

                if (int.TryParse(
                        wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter["PurgeCount"].ToString(),
                        out int purgeCount))
                {
                    return purgeCount;
                }

                return null;
            }
        }

        public int? GetWaferPumpDelayTime(int slot)
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(Module, slot))
                    return null;

                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return null;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return null;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules
                        .Any(m => m == ModuleName.LoadLock || m == ModuleName.Load))
                    return null;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter.ContainsKey("PumpDelayTime"))
                    return null;

                if (int.TryParse(
                        wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepParameter["PumpDelayTime"]
                            .ToString(), out int pumpDelayTime))
                {
                    return pumpDelayTime;
                }

                return null;
            }
        }

        public override bool CheckWaferNextStepIsThis(ModuleName module, int slot)
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(module, slot))
                    return false;

                WaferInfo wafer = WaferManager.Instance.GetWafer(module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return false;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return false;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules
                        .Any(m => m == ModuleName.LoadLock || m == ModuleName.Load))
                    return false;

                return true;
            }
        }

        public override bool CheckWaferTrayGrouped()
        {
            lock (SyncRoot)
            {
                return !_ll.CheckWaferClamped() && WaferManager.Instance.CheckHasWafer(Module, 0);
            }
        }

        public override bool CheckWaferTraySeparated()
        {
            throw new NotSupportedException();
        }

        public override bool CheckSlitValveClosed()
        {
            lock (SyncRoot)
            {
                return _ll.CheckSlitValveClosed();
            }
        }

        public bool Monitor()
        {
            lock (SyncRoot)
            {
                return true;
            }
        }
        
        public string GetTaskRunning()
        {
            return $"{_task.ToString()}/{_taskRobot}";
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
                        taskSucceed = _ll.CheckAcked(
                            _entityTaskToken); //&& _ll.CheckReadyForTransfer(_taskRobot, Hand.Blade1, _taskSlot, EnumTransferType.Place, out _);
                        break;
                    case TaskType.Cooling:
                        taskSucceed = _ll.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Vent:
                        taskSucceed = _ll.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Pump:
                        taskSucceed = _ll.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Purge:
                        taskSucceed = _ll.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Group:
                        taskSucceed = _ll.CheckAcked(_entityTaskToken);
                        break;
                    case TaskType.Separate:
                        taskSucceed = _ll.CheckAcked(_entityTaskToken);
                        break;

                }

                return SuperCheckTaskDone(taskSucceed, _ll.IsIdle | _ll.IsError);
            }
        }
    }
}
