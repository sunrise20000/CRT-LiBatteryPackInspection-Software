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
    public class TMRobotRetractRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            CheckBeforeRetract,
            Retract,
            WaitCloseSlitValveInterlock,
            CloseSlitValve,
        }

        private ModuleName _source;
        private int _sourceSlot;
        private Hand _blade;
        private int _timeout;
        private TMSlitValveRoutine _closeSlitValveRoutine = new TMSlitValveRoutine();

        public TMRobotRetractRoutine()
        {
            Module = "TMRobot";
            Name = "Retract";
 
        }

        public void Init(ModuleName source, int sourceSlot, Hand blade)
        {
            _source = source;
            _sourceSlot = sourceSlot;
            _blade = blade;
 
            _closeSlitValveRoutine.Init(source.ToString(), false);
        }

        public void Init(ModuleName source, int sourceSlot, int blade)
        {
            Init(source, sourceSlot, (Hand)blade);
 

        }
        public override Result Start(params object[] objs)
        {
            Reset();

            _timeout = SC.GetValue<int>("TMRobot.RetractTimeout");

            if (ModuleHelper.IsLoadLock(_source))
            {
                LoadLock ll = DEVICE.GetDevice<LoadLock>($"{_source.ToString()}.{_source.ToString()}");
                if (!ll.CheckEnableTransfer(EnumTransferType.Extend))
                {
                    EV.PostWarningLog(Module, $"can not retract, {_source} not ready for transfer");
                    return Result.FAIL;
                }
            }

            if (ModuleHelper.IsPm(_source))
            {
                PM pm = DEVICE.GetDevice<PM>(_source.ToString());
                if (!pm.CheckEnableTransfer(EnumTransferType.Retract, out string reason))
                {
                    EV.PostWarningLog(Module, $"can not retract, {_source} reason");
                    return Result.FAIL;
                }
            }

            Notify($"Start, retract from {_source} slot {_sourceSlot + 1} with {_blade}");

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

               // Retract((int)RoutineStep.Retract, RobotDevice, _source, _sourceSlot, _blade, _timeout);
                WaitSlitValveCloseInterlock((int)RoutineStep.WaitCloseSlitValveInterlock, TMDevice.GetSlitValve(_source), _timeout);

                ExecuteRoutine((int)RoutineStep.CloseSlitValve, _closeSlitValveRoutine);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify($"Finish, retract from {_source} slot {_sourceSlot + 1} with {_blade}");

            return Result.DONE;
        }


    }
}
