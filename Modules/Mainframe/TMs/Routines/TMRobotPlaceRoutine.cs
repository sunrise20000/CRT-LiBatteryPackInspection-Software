using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.LLs;
using Mainframe.LLs.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace Mainframe.TMs.Routines
{
    public class TMRobotPlaceRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            WaitOpenSlitValveInterlock,
            OpenSlitValve,

            CheckBeforePlace,
            Place,
            PrepareTransfer,

            Extend,
            Handoff,
            HandoffDelay,
            Retract,
            UpdateWaferInfoByHandoff,

            RequestWaferPresent,
            BeforePlaceRequestWaferPresent,
            AfterPlaceRequestWaferPresent,
            CheckWaferInfoByRobotSensor,
            BeforePlaceCheckWaferInfoByRobotSensor,
            AfterPlaceCheckWaferInfoByRobotSensor,
            RequestAWCData,
            SetRobortExtendToDo,
            ClearRobortExtendToDo,

            WaitCloseSlitValveInterlock,
            CloseSlitValve,
            WaitPMSensor,
            PostTransfer,
            OpenShutter,
            CloseShutter,
            Delay1,
            Delay2,
            VceMoveToSlot,
            RobotGotoNotWait,
            PrepareTransferNotWait,
            CheckRobotReady,
            CheckTransferPrepared,
            LoadRatation,
            TrayClamp,
            TrayUnClamp,
            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
            SuspectorCheck,
            TMPurge
        }

        private ModuleName _target;
        private int _targetSlot;
        private RobotArmEnum _blade;
        private int _placeTimeout;
        private int _postTransferTimeout;
        private bool _requestAWCData;
        private double _shutterAndSlitValveMotionInterval;

        private bool _autoHand;
        private TMSlitValveRoutine _openSlitValveRoutine = new TMSlitValveRoutine();
        private TMSlitValveRoutine _closeSlitValveRoutine = new TMSlitValveRoutine();
        //private TMPurgeRoutine _tmPurgeRoutine = new TMPurgeRoutine();

        private LoadRotationHomeOffsetRoutine loadRotationHomeRoutine = new LoadRotationHomeOffsetRoutine();
        private LoadLockTrayClawRoutine _trayClamp = new LoadLockTrayClawRoutine();
        private LoadLockTrayClawRoutine _trayUnClamp = new LoadLockTrayClawRoutine();
        private bool _isShutterAndSlitValveMotionOneByOne;
        private SicTM _tm;

        public TMRobotPlaceRoutine()
        {
            Module = "TMRobot";
            Name = "Place";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ ModuleName.TM.ToString()}");

        }

        public void Init(ModuleName target, int targetSlot, RobotArmEnum blade)
        {
            Init(target, targetSlot, blade, false);
        }

        public void Init(ModuleName target, int targetSlot, int blade)
        {
            Init(target, targetSlot, blade == 0 ? RobotArmEnum.Blade1 : RobotArmEnum.Blade2, false);
        }

        public void Init(ModuleName target, int targetSlot)
        {
            Init(target, targetSlot, RobotArmEnum.Blade1, true);
        }

        private void Init(ModuleName target, int targetSlot, RobotArmEnum blade, bool autoHand)
        {
            _autoHand = autoHand;
            _target = target;
            _targetSlot = targetSlot;
            _blade = blade;

            _openSlitValveRoutine.Init(target.ToString(), true);
            _closeSlitValveRoutine.Init(target.ToString(), false);
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            if (RobotDevice.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not place, TMRobot Is Not IDLE");
                return Result.FAIL;
            }
            _placeTimeout = SC.GetValue<int>("TMRobot.PlaceTimeout");
            _requestAWCData = SC.GetValue<bool>("System.RequestAWCDataAfterPlace");

            if (!WaferManager.Instance.CheckNoTray(_target, _targetSlot))
            {
                EV.PostWarningLog(Module, $"Can not place, should no tray at {_target}, {_targetSlot + 1}");
                return Result.FAIL;
            }

            if (_autoHand)
            {
                if (WaferManager.Instance.CheckHasWafer(Module, 0))
                {
                    _blade = RobotArmEnum.Blade1;
                }
                else if (WaferManager.Instance.CheckHasWafer(Module, 1))
                {
                    _blade = RobotArmEnum.Blade2;
                }
                else
                {
                    EV.PostWarningLog(Module, $"Can not place, Robot both arm no wafer");
                    return Result.FAIL;
                }
            }

            int slot = _blade == RobotArmEnum.Blade1 ? 0 : 1;
            if (WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(Module), slot))
            {
                EV.PostWarningLog(Module, $"Can not place, Robot arm {slot + 1} no tray");
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


            //Place之前先,根据Sensor检测是否有盘
            if (!SC.GetValue<bool>("System.IsSimulatorMode"))
            {
                if (_target ==ModuleName.LoadLock&& SensorLLTrayPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place, LLTrayPresence sensor has tray");
                    return Result.FAIL;
                }
                if (_target == ModuleName.UnLoad && SensorUnloadWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place, UnloadWaferPresence sensor has wafer");
                    return Result.FAIL;
                }
                if (ModuleHelper.IsBuffer(_target) && _targetSlot == 2&& SensorBufferHighWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,BufferHighWaferPresence sensor check has wafer");
                    return Result.FAIL;
                }
                if (ModuleHelper.IsBuffer(_target) && _targetSlot == 1&& SensorBufferMiddleWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,BufferMiddleWaferPresence sensor check has wafer");
                    return Result.FAIL;
                }
                if (ModuleHelper.IsBuffer(_target) && _targetSlot == 0&& SensorBufferLowWaferPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,BufferLowWaferPresence sensor check has wafer");
                    return Result.FAIL;
                }
            }
            _trayClamp.Init(true);
            _trayUnClamp.Init(false);

            //TM Purge设定为1次，Pump到0mbar,延时20s
            //_tmPurgeRoutine.Init(1, 5, 20, 300, 0);

            Notify($"Start, Place to {_target} slot {_targetSlot + 1}, by {(_blade == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");

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
                WaitSlitValveOpenInterlock((int)RoutineStep.WaitOpenSlitValveInterlock, TMDevice.GetSlitValve(_target), _placeTimeout);
                RobotGotoNotWait((int)RoutineStep.RobotGotoNotWait, RobotDevice, _target, _targetSlot, _blade, _placeTimeout);

                ExecuteRoutine((int)RoutineStep.OpenSlitValve, _openSlitValveRoutine);
                CheckRobotReady((int)RoutineStep.CheckRobotReady, RobotDevice, _placeTimeout);

                CheckBeforePlace((int)RoutineStep.CheckBeforePlace, _target, _targetSlot, _blade);
                RobotRequestWaferPresent((int)RoutineStep.BeforePlaceRequestWaferPresent, RobotDevice, _blade, _placeTimeout);
                CheckWaferInfoByRobotRQ((int)RoutineStep.BeforePlaceCheckWaferInfoByRobotSensor, RobotDevice, _blade, 10);

                WaitPMReadySensor((int)RoutineStep.WaitPMSensor, _target, 5);
                SetRobortExtendToDO((int)RoutineStep.SetRobortExtendToDo, _target, 2);

                CheckRobotReady((int)RoutineStep.CheckRobotReady, RobotDevice, _placeTimeout);
                Place((int)RoutineStep.Place, RobotDevice, _target, _targetSlot, _blade, _placeTimeout);
                IsPicking = false;
                TimeDelay((int)RoutineStep.TimeDelay1, 1);
                RobotRequestWaferPresent((int)RoutineStep.RequestWaferPresent, RobotDevice, _blade, _placeTimeout);
                CheckWaferInfoByRobotRQ((int)RoutineStep.CheckWaferInfoByRobotSensor, RobotDevice, _blade, 10);

                if (_requestAWCData)
                {
                    RobotRequestWaferAWCData((int)RoutineStep.RequestAWCData, RobotDevice, _placeTimeout);
                }

                ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);
                TimeDelay((int)RoutineStep.TimeDelay2, 1);


                if (_target == ModuleName.Load || _target == ModuleName.LoadLock)
                {
                    //对中
                    ExecuteRoutine((int)RoutineStep.TrayClamp, _trayClamp);        //夹爪关闭
                    TimeDelay((int)RoutineStep.TimeDelay3, 1);//延迟1s
                    ExecuteRoutine((int)RoutineStep.TrayUnClamp, _trayUnClamp);      //夹爪打开
                    //ExecuteRoutine((int)RoutineStep.LoadRatation, loadRotationHomeRoutine);
                }

                //如果是PM腔，放完盘后检查传感器DI_ReactorSuspectorCheck
                if (ModuleHelper.IsPm(_target))
                {
                    CheckReactorSuspector((int)RoutineStep.SuspectorCheck);
                }

                //如果是Buffer腔，则不需要关闭闸板阀动作
                if (_target != ModuleName.Buffer)
                {
                    ExecuteRoutine((int)RoutineStep.CloseSlitValve, _closeSlitValveRoutine);
                }
                //如果是Buffer,则TM腔执行Purge
                /*else
                {
                    ExecuteRoutine((int)RoutineStep.TMPurge, _tmPurgeRoutine);
                }*/
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

            Notify($"Finish, Place to {_target} slot {_targetSlot + 1}, by {_blade}");

            return Result.DONE;
        }




    }
}
