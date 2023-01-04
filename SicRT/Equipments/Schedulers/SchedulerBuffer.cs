using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using SicRT.Equipments.Systems;
using Mainframe.Buffers;
using Aitex.Core.RT.Device;
using SicPM.Devices;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerBuffer : SchedulerModule 
    {
        public override bool IsAvailable
        {
            get { return _buffer.IsIdle && _buffer.IsOnline && CheckTaskDone(); }

        }
        public override bool IsOnline
        {
            get { return _buffer.IsOnline; }

        }
        public override bool IsError
        {
            get { return _buffer.IsError; }

        }

        public bool HasPurged { get; set; }

        private BufferModuleBase _buffer = null;

        private int _entityTaskToken = (int)FSM_MSG.NONE;

        public SchedulerBuffer() : base(ModuleName.Buffer.ToString())
        {
            _buffer = Singleton<EquipmentManager>.Instance.Modules[ModuleName.Buffer] as BufferModuleBase;
        }

        public bool Monitor()
        { 
            return true;
        }

        public void ResetPurgeStatus()
        {
            HasPurged = false;
        }

        internal float GetTemperature()
        {
            IoTempMeter deviceBufferTemp = DEVICE.GetDevice<IoTempMeter>($"Buffer.BufferTemp");
            return deviceBufferTemp.FeedBack;
        }

        public override bool Cooling(bool coolingTypeIsTime,int coolingValue)
        {
            _task = TaskType.Cooling;

            _entityTaskToken = _buffer.InvokeCooling(coolingTypeIsTime, coolingValue);

            if (coolingTypeIsTime)
            {
                LogTaskStart(_task, $"{Module} cooling {coolingValue} seconds");
            }
            else 
            {
                LogTaskStart(_task, $"{Module} cooling wait temprature below {coolingValue}℃");
            }

            return _entityTaskToken != (int)FSM_MSG.NONE;
        }

        public bool PreHeat(int preTemp)
        {
            _task = TaskType.WarmUp;
            LogTaskStart(_task, $"{Module} warm up to {preTemp} ℃");
            _entityTaskToken = _buffer.InvokeWarmUp(preTemp);
            return _entityTaskToken != (int)FSM_MSG.NONE;
        }

        /// <summary>
        /// 是否有空槽位
        /// </summary>
        /// <returns></returns>
        public bool HasEmptySlot()
        {
            return !HasTray(0) || !HasTray(1) || !HasTray(2);
        }

        /// <summary>
        /// Buffer中是否有可用于工艺的Tray。
        /// </summary>
        /// <returns></returns>
        public bool HasAvailableTray()
        {
            return HasTrayAndNotExceedProcessCount(0)
                   || HasTrayAndNotExceedProcessCount(1)
                   || HasTrayAndNotExceedProcessCount(2);
        }

        public bool CheckTaskDone()
        {
            var taskSucceed = false;
            switch (_task)
            {
                case TaskType.None:
                    taskSucceed = true;
                    break;
                case TaskType.Cooling:
                    taskSucceed = _buffer.CheckAcked(_entityTaskToken);
                    break;
                case TaskType.WarmUp:
                    taskSucceed = _buffer.CheckAcked(_entityTaskToken);
                    break;
            }

            return SuperCheckTaskDone(taskSucceed, _buffer.IsIdle | _buffer.IsError);
        }
    }
}