using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace Mainframe.TMs.Routines
{
    public class TMRobotPickRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            WaitOpenSlitValveInterlock,

            OpenSlitValve,

            CheckBeforePick,
            PrepareTransfer,
            PrepareTransferNotWait,

            Pick,
            QueryOffset,

            Extend,
            Handoff,
            HandoffDelay,
            Retract,
            UpdateWaferInfoByHandoff,

            RequestWaferPresent,
            BeforePickRequestWaferPresent,
            AfterPickRequestWaferPresent,
            RequestAWCData,
            CheckWaferInfoByRobotSensor,
            BeforePickCheckWaferInfoByRobotSensor,
            AfterPickCheckWaferInfoByRobotSensor,

            WaitCloseSlitValveInterlock,
            CloseSlitValve,

            SetRobortExtendToDo,
            ClearRobortExtendToDo,
            WaitPMSensor,

            PostTransfer,
            OpenShutter,
            CloseShutter,
            Delay1,
            Delay2,
            VceMoveToSlot,
            RobotGotoNotWait,
            CheckRobotReady,
            CheckTransferPrepared,
            SetConfinementRingUp,
            TimeDelay1,
            TimeDelay2,
            TimeDelay20,
            TimeDelay50,
        }

        private ModuleName _target;
        private int _targetSlot;
        private RobotArmEnum _blade;
        private int _pickTimeout;
        private bool _autoHand;
        private double _shutterAndSlitValveMotionInterval =5;
        private bool _isShutterAndSlitValveMotionOneByOne = false;

        private bool _requestAWCData;
        private SicTM _tm;

        private TMSlitValveRoutine _openSlitValveRoutine = new TMSlitValveRoutine();
        private TMSlitValveRoutine _closeSlitValveRoutine = new TMSlitValveRoutine();

        private bool _pmPostTrasferEnableHeat = true;

        public TMRobotPickRoutine()
        {
            Module = "TMRobot";
            Name = "Pick";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ ModuleName.TM.ToString()}");
        }

        public void Init(ModuleName source, int sourceSlot, RobotArmEnum blade)
        {
            Init(source, sourceSlot, blade, false);
        }

        public void Init(ModuleName source, int sourceSlot, int blade)
        {
            Init(source, sourceSlot, blade == 0 ? RobotArmEnum.Blade1 : RobotArmEnum.Blade2, false);
        }

        public void Init(ModuleName source, int sourceSlot)
        {
            Init(source, sourceSlot, RobotArmEnum.Blade1, true);
        }

        private void Init(ModuleName source, int sourceSlot, RobotArmEnum blade, bool autoHand)
        {
            _autoHand = autoHand;
            _target = source;
            _targetSlot = sourceSlot;
            _blade = blade;

            _openSlitValveRoutine.Init(source.ToString(), true, _pmPostTrasferEnableHeat);
            _closeSlitValveRoutine.Init(source.ToString(), false, _pmPostTrasferEnableHeat);
        }

        public void Init(ModuleName source, int sourceSlot, int blade, bool autoHand, bool pmPostTrasferEnableHeat)
        {
            _pmPostTrasferEnableHeat = pmPostTrasferEnableHeat;
            Init(source, sourceSlot, RobotArmEnum.Blade1, autoHand);
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            if (RobotDevice.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not pick, TMRobot Is Not IDLE");
                return Result.FAIL;
            }
            _pickTimeout = SC.GetValue<int>("TMRobot.PickTimeout");


            //if (ModuleHelper.IsPm(_target))
            //{
            //    _postTransferTimeout = SC.GetValue<int>($"{_target}.PostTransferTimeout");
            //    _shutterAndSlitValveMotionInterval = SC.GetValue<double>($"{_target}.ShutterAndSlitValveMotionInterval");
            //    _isShutterAndSlitValveMotionOneByOne = SC.GetValue<bool>($"{_target}.ShutterAndSlitValveMotionOneByOne");

            //    //CoralPM pm = DEVICE.GetDevice<CoralPM>(_target.ToString());
            //    if (!pm.PickPlaceCheck(out string reason, false))
            //    {
            //        EV.PostWarningLog(Module, $"Can not place, {reason}");
            //        return Result.FAIL;
            //    }
            //}


            _requestAWCData = SC.GetValue<bool>("System.RequestAWCDataAfterPick");

            if (!WaferManager.Instance.CheckHasTray(_target, _targetSlot))
            {
                EV.PostWarningLog(Module, $"Can not pick, No tray at {_target}, {_targetSlot + 1}");
                return Result.FAIL;
            }

            if (_autoHand)
            {
                if (WaferManager.Instance.CheckNoWafer(Module, 0))
                {
                    _blade = RobotArmEnum.Blade1;
                }
                else if (WaferManager.Instance.CheckNoWafer(Module, 1))
                {
                    _blade = RobotArmEnum.Blade2;
                }
                else
                {
                    EV.PostWarningLog(Module, $"Can not pick, Robot both arm has wafer");
                    return Result.FAIL;
                }
            }

            int slot = _blade == RobotArmEnum.Blade1 ? 0 : 1;
            if (!WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(Module), slot))
            {
                EV.PostWarningLog(Module, $"Can not pick, Robot arm {slot + 1} has tray");
                return Result.FAIL;
            }

            //顶针必须在低位
            if (_target == ModuleName.LoadLock || _target == ModuleName.Load)
            {
                if (!LoadLift.IsDown)
                {
                    EV.PostWarningLog(Module, $"Can not place, Load Lift must in down Position!");
                    return Result.FAIL;
                }
            }
            else if (_target == ModuleName.UnLoad)
            {
                if (!UnLoadLift.IsDown)
                {
                    EV.PostWarningLog(Module, $"Can not place, UnLoad Lift must in down Position!");
                    return Result.FAIL;
                }
            }

            //Pick之前先,根据Sensor检测是否有盘
            if (!SC.GetValue<bool>("System.IsSimulatorMode"))
            {
                if (ModuleHelper.IsLoadLock(_target) && !SensorLLTrayPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick, LLTrayPresence sensor check no Tray");
                    return Result.FAIL;
                }
                if (_target==ModuleName.UnLoad && !SensorUnloadWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,UnloadWaferPresence sensor check no wafer");
                    return Result.FAIL;
                }
                if (ModuleHelper.IsBuffer(_target)&&_targetSlot==2 && !SensorBufferHighWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,BufferHighWaferPresence sensor check no wafer");
                    return Result.FAIL;
                }
                if (ModuleHelper.IsBuffer(_target) && _targetSlot == 1 && !SensorBufferMiddleWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,BufferMiddleWaferPresence sensor check no wafer");
                    return Result.FAIL;
                }
                if (ModuleHelper.IsBuffer(_target) && _targetSlot == 0 && !SensorBufferLowWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,BufferLowWaferPresence sensor check no wafer");
                    return Result.FAIL;
                }
            }


            Notify($"Start, Pick from {_target} slot {_targetSlot + 1}, by {(_blade == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
            IsPicking = false;
            return Result.RUN;
        }

        public override void Abort()
        {
            _tm.CloseAllVentPumpValue(); 
            if (!IsPicking)
            {
                RobotDevice.Stop();
            }
            Notify("Abort");
        }


        public override Result Monitor()
        {
            try
            {
                WaitSlitValveOpenInterlock((int)RoutineStep.WaitOpenSlitValveInterlock, TMDevice.GetSlitValve(_target), _pickTimeout);
                RobotGotoNotWait((int)RoutineStep.RobotGotoNotWait, RobotDevice, _target, _targetSlot, _blade, _pickTimeout);

                ExecuteRoutine((int)RoutineStep.OpenSlitValve, _openSlitValveRoutine);
                CheckRobotReady((int)RoutineStep.CheckRobotReady, RobotDevice, _pickTimeout);
                CheckBeforePick((int)RoutineStep.CheckBeforePick, _target, _targetSlot, _blade);
                RobotRequestWaferPresent((int)RoutineStep.BeforePickRequestWaferPresent, RobotDevice, _blade, _pickTimeout);
                CheckWaferInfoByRobotRQ((int)RoutineStep.AfterPickCheckWaferInfoByRobotSensor, RobotDevice, _blade, 1000);
                WaitPMReadySensor((int)RoutineStep.WaitPMSensor, _target,5);
                SetRobortExtendToDO((int)RoutineStep.SetRobortExtendToDo, _target, 2);
                CheckRobotReady((int)RoutineStep.CheckRobotReady, RobotDevice, _pickTimeout);
                Pick((int)RoutineStep.Pick, RobotDevice, _target, _targetSlot, _blade, _pickTimeout);
                IsPicking = false;
                TimeDelay((int)RoutineStep.TimeDelay2, 1);
                RobotRequestWaferPresent((int)RoutineStep.RequestWaferPresent, RobotDevice, _blade, _pickTimeout);
                CheckWaferInfoByRobotRQ((int)RoutineStep.CheckWaferInfoByRobotSensor, RobotDevice, _blade, 1000);

                if (_requestAWCData)
                {
                    RobotRequestWaferAWCData((int)RoutineStep.RequestAWCData, RobotDevice, _pickTimeout);
                }


                ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);
                TimeDelay((int)RoutineStep.TimeDelay1, 1);

                //如果是Buffer腔，则不需要关闭闸板阀动作
                if (_target != ModuleName.Buffer)
                {
                    ExecuteRoutine((int)RoutineStep.CloseSlitValve, _closeSlitValveRoutine);
                }
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException ex)
            {
                LOG.Error(ex.ToString());
                RobotDevice.Stop();
                return Result.FAIL;
            }

            Notify($"Finished, Pick from {_target} slot {_targetSlot + 1}, by {_blade}");

            return Result.DONE;
        }

    }
   }
