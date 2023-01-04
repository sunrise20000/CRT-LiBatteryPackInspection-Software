using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;

namespace Mainframe.TMs.Routines
{
    public class TMRobotExtendRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            WaitOpenSlitValveInterlock,
            OpenSlitValve,
            CheckBeforeExtend,
            Extend,
        }

        private ModuleName _source;
        private int _sourceSlot;
        private Hand _hand;
        private int _timeout;
        private TMSlitValveRoutine _openSlitValveRoutine = new TMSlitValveRoutine();

        public TMRobotExtendRoutine()
        {
            Module = "TMRobot";
            Name = "Extend";
         }

        public void Init(ModuleName source, int sourceSlot, Hand blade)
        {
            _source = source;
            _sourceSlot = sourceSlot;
            _hand = blade;
            _openSlitValveRoutine.Init(source.ToString(), true);
 
        }

        public void Init(ModuleName source, int sourceSlot, int blade)
        {
            Init(source, sourceSlot, (Hand)blade);
         }

         
        public override Result Start(params object[] objs)
        {
            Reset();

            _timeout = SC.GetValue<int>("TMRobot.ExtendTimeout");

            if (ModuleHelper.IsLoadLock(_source))
            {
                LoadLock ll = DEVICE.GetDevice<LoadLock>($"{_source.ToString()}.{_source.ToString()}");
                if (!ll.CheckEnableTransfer(EnumTransferType.Extend))
                {
                    EV.PostWarningLog(Module, $"can not extend, {_source} not ready for transfer");
                    return Result.FAIL;
                }
            }

            if (ModuleHelper.IsPm(_source))
            {
                PM pm = DEVICE.GetDevice<PM>(_source.ToString());
                if (!pm.CheckEnableTransfer(EnumTransferType.Extend, out string reason))
                {
                    EV.PostWarningLog(Module, $"can not extend, {_source} reason");
                    return Result.FAIL;
                }
            }

            Notify($"Start, extend to {_source} slot {_sourceSlot+1} with {_hand}");

            return Result.RUN;
        }

        public override void Abort()
        {
            Notify("Abort");
        }


        public override Result Monitor()
        {
            try
            {
                WaitSlitValveOpenInterlock((int)RoutineStep.WaitOpenSlitValveInterlock, TMDevice.GetSlitValve(_source), _timeout);

                ExecuteRoutine((int)RoutineStep.OpenSlitValve, _openSlitValveRoutine);

                //CheckBeforeExtend((int)RoutineStep.CheckBeforeExtend, _source, _sourceSlot, _hand);

                //Extend((int)RoutineStep.Extend, RobotDevice, _source, _sourceSlot, _hand, _timeout);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify($"Finish, extend to {_source} slot {_sourceSlot + 1} with {_hand}");

            return Result.DONE;
        }

    }
}
