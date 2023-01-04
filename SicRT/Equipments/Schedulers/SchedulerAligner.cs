using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using SicRT.Equipments.Systems;
using Mainframe.Aligners;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerAligner : SchedulerModule
    {
        public override bool IsAvailable
        {
            get { return _aliger.IsIdle && _aliger.IsOnline && CheckTaskDone(); }

        }
        public override bool IsOnline
        {
            get { return _aliger.IsOnline; }

        }
        public override bool IsError
        {
            get { return _aliger.IsError; }

        }

        public bool HasAligned { get; private set; }


        private AlignerModuleBase _aliger = null;

        private int _entityTaskToken = (int)FSM_MSG.NONE;

        public SchedulerAligner() : base(ModuleName.Aligner.ToString())
        {
            _aliger = Singleton<EquipmentManager>.Instance.Modules[ModuleName.Aligner] as AlignerModule;
        }

        public bool Monitor()
        {
            return true;
        }

        public override bool Aligning()
        {
            _task = TaskType.Align;
            _entityTaskToken = _aliger.InvokeAligner();
            LogTaskStart(_task, $"{Module} Aligning");
            HasAligned = true;
            return _entityTaskToken != (int)FSM_MSG.NONE;
        }

        public void ResetAlignedStatus()
        {
            HasAligned = false;
        }

        public bool CheckTaskDone()
        {
            var taskSucceed = false;
            switch (_task)
            {
                case TaskType.None:
                    taskSucceed = true;
                    break;
                case TaskType.Align:
                    taskSucceed = _aliger.CheckAcked(_entityTaskToken);
                    break;
            }

            return SuperCheckTaskDone(taskSucceed, _aliger.IsIdle | _aliger.IsError);
        }

        public override bool IsReadyForPick(ModuleName robot, int slot)
        {
            return true;
        }

        public override bool IsReadyForPlace(ModuleName robot, int slot)
        {
            return true;
        }
    }
}