using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;

namespace Mainframe.TMs.Routines
{
    public class TMRobotPickAndPlaceRoutine : TMBaseRoutine 
    {
        enum RoutineStep
        {
            WaitOpenSlitValveInterlock,

            OpenSlitValve,

            CheckBeforePick,
            Pick,
            PickLiftMove,
            PickExtend,
            PickHandoff,
            PickHandoffDelay,
            PickRetract,
            PickUpdateWaferInfoByHandoff,
            PickRequestWaferPresent,
            PickCheckWaferInfoByRobotSensor,
            PickWaitCloseSlitValveInterlock,
            PickRequestAWCData,

            CheckBeforePlace,
            Place,
            PlaceLiftMove,
            PlaceExtend,
            PlaceHandoff,
            PlaceHandoffDelay,
            PlaceRetract,
            PlaceUpdateWaferInfoByHandoff,
            PlaceRequestWaferPresent,
            PlaceCheckWaferInfoByRobotSensor,
            PlaceWaitCloseSlitValveInterlock,
            PlaceRequestAWCData,

            CloseSlitValve,

            PostTransfer,
        }

        private Hand _pickBlade;
        private Hand _placeBlade;
        private ModuleName _targetModule;
        private int _pickSlot;
        private int _placeSlot;
        private int _pickTimeout;
        private int _placeTimeout;
        private int _postTransferTimeout;
        private TMSlitValveRoutine _openSlitValveRoutine = new TMSlitValveRoutine();
        private TMSlitValveRoutine _closeSlitValveRoutine = new TMSlitValveRoutine();

        private bool _requestAWCDataPick;
        private bool _requestAWCDataPlace;

        public TMRobotPickAndPlaceRoutine()
        {
            Module = "TMRobot";
            Name = "Swap";
        }

        public void Init(ModuleName targetModule, int pickSlot, int placeSlot, Hand pickBlade, Hand placeBlade)
        {
            _pickBlade = pickBlade;
            _placeBlade = placeBlade;
            _targetModule = targetModule;
            _pickSlot = pickSlot;
            _placeSlot = placeSlot;
            _openSlitValveRoutine.Init(targetModule.ToString(), true);
            _closeSlitValveRoutine.Init(targetModule.ToString(), false);
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _pickTimeout = SC.GetValue<int>("TMRobot.PickTimeout");
            _placeTimeout = SC.GetValue<int>("TMRobot.PlaceTimeout");
            _postTransferTimeout = SC.GetValue<int>($"{_targetModule}.PostTransferTimeout");
            _requestAWCDataPick = SC.GetValue<bool>("System.RequestAWCDataAfterPick");
            _requestAWCDataPlace = SC.GetValue<bool>("System.RequestAWCDataAfterPlace");

            if (!WaferManager.Instance.CheckNoWafer(ModuleName.TMRobot, (int) _pickBlade))
            {
                EV.PostWarningLog(Module, $"Can not pick by {_pickBlade}, found wafer on");
                return Result.FAIL;
            }

            if (!WaferManager.Instance.CheckHasWafer(ModuleName.TMRobot, (int)_placeBlade))
            {
                EV.PostWarningLog(Module, $"Can not place by {_placeBlade}, no wafer on");
                return Result.FAIL;
            }

            if (_pickSlot != _placeSlot)
            {
                if (!WaferManager.Instance.CheckHasWafer(_targetModule, _pickSlot))
                {
                    EV.PostWarningLog(Module, $"Can not pick from {_targetModule} slot {_pickSlot + 1}, no wafer on");
                    return Result.FAIL;
                }
                if (!WaferManager.Instance.CheckNoWafer(_targetModule, _placeSlot))
                {
                    EV.PostWarningLog(Module, $"Can not place to {_targetModule} slot {_placeSlot + 1}, found wafer on");
                    return Result.FAIL;
                }
            }
            else
            {
                if (!WaferManager.Instance.CheckHasWafer(_targetModule, _placeSlot))
                {
                    {
                        EV.PostWarningLog(Module, $"Can not pick&place from {_targetModule} slot {_placeSlot + 1}, no wafer on");
                        return Result.FAIL;
                    }
                }
            }

             

            Notify("Start");

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
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify("Finished");

            return Result.DONE;
        }

       
    }
}
