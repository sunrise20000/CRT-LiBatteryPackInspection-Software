using Aitex.Core.Common;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using SicRT.Modules;
using SicRT.Modules.Schedulers;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Equipments;
using SicRT.Equipments.Systems;
using SicPM;
using Aitex.Core.RT.Device;
using SicPM.Devices;

namespace SicRT.Scheduler
{
    public class SchedulerPM : SchedulerModule
    {
        public override bool IsAvailable
        {
            get
            {
                if (Singleton<EquipmentManager>.Instance.IsAutoMode)
                {
                    return _pm.IsProcessIdle && _pm.IsOnline && CheckTaskDone();
                }
                else
                {
                    return _pm.IsProcessIdle && _pm.IsOnline && CheckTaskDone();
                }
            }
        }

        public override bool IsOnline
        {
            get { return _pm.IsOnline; }
        }
        public override bool IsError
        {
            get { return _pm.IsError; }
        }

        public override bool IsService
        {
            get { return _pm.IsService; }
        }


        private PMModuleBase _pm = null;

        private ModuleName _taskRobot;
        private EnumTransferType _taskTransferType;
        private int _taskSlot;
        private int _entityTaskToken = (int)FSM_MSG.NONE;

        private DeviceTimer _timer = new DeviceTimer();

        public SchedulerPM(ModuleName chamber) : base(chamber.ToString())
        {
            _pm = Singleton<EquipmentManager>.Instance.Modules[chamber] as PMModuleBase;
            System.Diagnostics.Trace.Assert(_pm != null);
        }

        public override bool IsReadyForPick(ModuleName robot, int slot)
        {
            return _pm.IsPrepareTransferReady(robot, EnumTransferType.Pick, slot)
                   && WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(_module), slot);
        }

        public override bool IsReadyForPlace(ModuleName robot, int slot)
        {
            return _pm.IsPrepareTransferReady(robot, EnumTransferType.Place, slot)
                   && WaferManager.Instance.CheckNoWafer(ModuleHelper.Converter(_module), slot);
        }

        public override bool PrepareTransfer(ModuleName robot, EnumTransferType type, int slot)
        {
            _task = TaskType.PrepareTransfer;
            _taskRobot = robot;
            _taskSlot = slot;
            _taskTransferType = type;
            _entityTaskToken = _pm.InvokePrepareTransfer(robot, type, slot);

            LogTaskStart(_task, $"{robot} {type} slot {slot + 1}");
            return _entityTaskToken != (int)FSM_MSG.NONE;
        }

        public override bool Process(string recipeName, bool isCleanRecipe, bool withWafer)
        {
            _task = TaskType.Process;

            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                _timer.Start(5000);

                _entityTaskToken = (int)FSM_MSG.TIMER;

                WaferManager.Instance.GetWafer(Module, 0).ProcessState = EnumWaferProcessStatus.InProcess;
            }
            else
            {
                _entityTaskToken = _pm.InvokeProcess(recipeName, isCleanRecipe, withWafer);
            }

            LogTaskStart(_task, $"recipe: {recipeName}, clean: {isCleanRecipe}, with wafer: {withWafer}");


            return _entityTaskToken != (int)FSM_MSG.NONE;
        }

        public bool CheckTaskDone()
        {
            var taskSucceed = false;
            switch (_task)
            {
                case TaskType.None:
                    taskSucceed = true;
                    break;
                case TaskType.PrepareTransfer:
                    taskSucceed = _pm.CheckAcked(_entityTaskToken) && _pm.IsPrepareTransferReady(_taskRobot, _taskTransferType, _taskSlot);
                    break;
                case TaskType.Process:

                    if (SC.GetValue<bool>("System.IsATMMode"))
                    {
                        taskSucceed = _timer.IsTimeout();

                        if (taskSucceed)
                            WaferManager.Instance.GetWafer(Module, 0).ProcessState = EnumWaferProcessStatus.Completed;
                    }
                    else
                    {
                        taskSucceed = _pm.CheckAcked(_entityTaskToken) && _pm.IsProcessed();

                    }

                    break;
            }

            if (taskSucceed && _task != TaskType.None)
            {
                LogTaskDone(_task, "");
                _task = TaskType.None;
            }

            return taskSucceed;
        }

        public bool Monitor()
        {
            return true;
        }

        //TMRobot从Buffer放到PM时检查
        public bool CheckBufferToPMTemp()
        {
            //有一个PSU Enable则关闭Enable
            if (!_pm.CheckPlacetoPMTemp() && _pm.CheckHeaterEnable())
            {
                _pm.CloseHeaterEnable(out _);
            }

            SicPM.Devices.IoInterLock pmIoInterLock = DEVICE.GetDevice<SicPM.Devices.IoInterLock>($"{_module}.PMInterLock");
            if (pmIoInterLock != null)
            {
                return pmIoInterLock.DiHeaterTempBelow900CSW && _pm.CheckPlacetoPMTemp();
            }

            return false;
        }

        public bool CheckTempBelow900()
        {
            SicPM.Devices.IoInterLock pmIoInterLock = DEVICE.GetDevice<SicPM.Devices.IoInterLock>($"{_module}.PMInterLock");
            if (pmIoInterLock != null)
            {
                return pmIoInterLock.DiHeaterTempBelow900CSW;
            }

            return false;
        }

    }
}
