using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.LLs.Routines;
using Mainframe.UnLoads.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.HwAligner;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace Mainframe.EFEMs.Routines
{
    public class WaferRobotPlaceRoutine : EfemBaseRoutine
    {
        /* 1.Place to LoadLock(需要开门)/Aligner/Cassette
         * 2.判断目标是否无Wafer，判断Robot是否有Wafer
         * 3.判断是否需要开门（包括气压等）
         * 4.判断顶针是否上升
         * 5.伸出
         * 6.判断顶针上升
         * 7.ActionDone
         * 8.关门
         * 
机械手伸进来
顶针顶起来
夹爪夹住
夹爪松开
机械手退出
顶针下降
         */
        private EfemSlitValveRoutine _efemSlitValveOpenRoutine = new EfemSlitValveRoutine();
        private EfemSlitValveRoutine _efemSlitValveCloseRoutine = new EfemSlitValveRoutine();
        private LoadLockLiftRoutine _loadLockLiftDown = new LoadLockLiftRoutine();
        private LoadLockLiftRoutine _loadLockLiftUp = new LoadLockLiftRoutine();
        private LoadLockClawRoutine _loadLockClaw = new LoadLockClawRoutine();
        private LoadLockClawRoutine _loadLockClawOpen = new LoadLockClawRoutine();

        private UnLoadLiftRoutine _unLoadLiftDown = new UnLoadLiftRoutine();
        private UnLoadLiftRoutine _unLoadLiftUp = new UnLoadLiftRoutine();
        private UnLoadClawRoutine _unLoadClaw = new UnLoadClawRoutine();
        private UnLoadClawRoutine _unLoadClawOpen = new UnLoadClawRoutine();

        private HwAlignerGuide _alignerDevice;

        private ModuleName _target;
        private int _targetSlot;
        private int _placeTimeout;
        private int _alignerMoveTimeOut = 10;

        private bool _isSimulator = false;


        enum RoutineStep
        {
            SetLiftDown0,
            AlignerMoveTo,
            OpenSlowVent,
            OpenSlitValve,
            SetLiftUp,
            CheckRobotReady,
            ExtendForPlace,
            PickComplete,
            SetLiftDown,
            CloseSlitValve,
            CloseSlowVent,
            TimeDelay1,
            Place,
            SetClawOpen1,
            SetClawOpen2,
            SetClaw,
            AlignerCheckWafer,
            SetExtendToDo,
            ClearRobortExtendToDo,
            CheckWaferStatusByRq
        }

        public WaferRobotPlaceRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            Name = "WaferRobotPlace";
            _alignerDevice = DEVICE.GetDevice<HwAlignerGuide>($"TM.HiWinAligner");
        }

        public override void Init(ModuleName targetModule, int targetSlt)
        {
            _target = targetModule;
            _targetSlot = targetSlt;
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            if (!WaferManager.Instance.CheckHasWafer(ModuleName.WaferRobot, 0))
            {
                Stop("Can not place,WaferRobot no wafer");
                return Result.FAIL;
            }
            //Place之前先,根据Sensor检测是否有盘
            if (WaferManager.Instance.CheckHasWafer(_target, _targetSlot))
            {
                EV.PostWarningLog(Module, $"Can not place, {_target} slot {_targetSlot} has wafer");
                return Result.FAIL;
            }
            if (WaferRobot.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not place, WaferRobot is not Idle");
                return Result.FAIL;
            }

            //LoadLock传感器是否需要检测有Wafer
            //if (_target == ModuleName.LoadLock || _target == ModuleName.Load)
            //{
            //    if (_loadWaferPlaced.Value)
            //    {
            //        EV.PostWarningLog(Module, $"Can not place,{_target} sensor[DI-35] check have wafer");
            //        return Result.FAIL;
            //    }
            //}

            if (_target == ModuleName.CassAL)
            {
                if (!_cassAL6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,{_target} sensor check no cassette");
                    return Result.FAIL;
                }
            }
            else if (_target == ModuleName.CassAR)
            {
                if (!_cassAR6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,{_target} sensor check no cassette");
                    return Result.FAIL;
                }
            }

            // 如果从LoadLock或UnLoad取Wafer，打开ATM闸板阀前先开始Vent,获取Vent参数
            if (_target == ModuleName.LoadLock || _target == ModuleName.UnLoad)
            {
                if (_target == ModuleName.LoadLock)
                {
                    _slowVentTimeout = SC.GetValue<int>("LoadLock.Vent.SlowVentTimeout");
                    _ventBasePressure = SC.GetValue<double>("LoadLock.Vent.VentBasePressure");
                }
                else
                {
                    _slowVentTimeout = SC.GetValue<int>("UnLoad.Vent.SlowVentTimeout");
                    _ventBasePressure = SC.GetValue<double>("UnLoad.Vent.VentBasePressure");
                }
            }

            _efemSlitValveOpenRoutine.Init(_target, ModuleName.WaferRobot, true);
            _efemSlitValveCloseRoutine.Init(_target, ModuleName.WaferRobot, false);
            _loadLockLiftDown.Init(false);
            _loadLockLiftUp.Init(true);
            _loadLockClaw.Init(true,true);
            _loadLockClawOpen.Init(true, false);

            _unLoadLiftDown.Init(false);
            _unLoadLiftUp.Init(true);
            _unLoadClaw.Init(true);
            _unLoadClawOpen.Init(false);

            _placeTimeout = SC.GetConfigItem($"{ModuleName.WaferRobot}.MotionTimeout").IntValue;
            _isSimulator = SC.GetValue<bool>($"System.IsSimulatorMode");

            _alignerMoveTimeOut = SC.GetValue<int>("HiWinAligner.AlignerMoveToCenterTimeOut");

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                if (_target == ModuleName.Aligner && !_isSimulator)
                {
                    AlignerCheckHaveWafer((int)RoutineStep.AlignerCheckWafer, 15);  //发送指令检测是否有Wafer
                    AlignerMoveToRobotPutPalce((int)RoutineStep.AlignerMoveTo, _alignerMoveTimeOut); //Aligner移动至测量中心点
                }

                // 如果从LoadLock或UnLoad取Wafer，打开ATM闸板阀前先开始Vent
                /*if (_target == ModuleName.LoadLock || _target == ModuleName.UnLoad)
                {
                    SlowVent((int)RoutineStep.OpenSlowVent, _target, _ventBasePressure, _slowVentTimeout);
                }*/

                if (_target == ModuleName.LoadLock || _target == ModuleName.Load)
                {
                    ExecuteRoutine((int)RoutineStep.SetLiftDown0, _loadLockLiftDown);
                    ExecuteRoutine((int)RoutineStep.SetClawOpen1, _loadLockClawOpen);               //夹爪打开
                }
                else if (_target == ModuleName.UnLoad)
                {
                    ExecuteRoutine((int)RoutineStep.SetLiftDown0, _unLoadLiftDown);
                    ExecuteRoutine((int)RoutineStep.SetClawOpen1, _unLoadClawOpen);               //夹爪打开
                }

                ExecuteRoutine((int)RoutineStep.OpenSlitValve, _efemSlitValveOpenRoutine);      //打开闸板阀
                SetWaferRobortExtendToDO((int)RoutineStep.SetExtendToDo, _target, 10);           //设置ExtendToDO,用于检测InterLock
                CheckRobotReady((int)RoutineStep.CheckRobotReady, WaferRobot, _placeTimeout);    //判断机械手当前是否空闲
                ExtendForPlace((int)RoutineStep.ExtendForPlace, WaferRobot, _target, _targetSlot, _placeTimeout); //伸出,关真空

                //夹爪打开,顶针上升
                if (_target == ModuleName.LoadLock || _target == ModuleName.Load)
                {
                    ExecuteRoutine((int)RoutineStep.SetLiftUp, _loadLockLiftUp);
                }
                else if (_target == ModuleName.UnLoad)
                {
                    ExecuteRoutine((int)RoutineStep.SetLiftUp, _unLoadLiftUp);
                }

                Place((int)RoutineStep.Place, WaferRobot, _target, _targetSlot, _placeTimeout);    //机械手收回

                if (_target == ModuleName.LoadLock || _target == ModuleName.Load)
                { 
                    ExecuteRoutine((int)RoutineStep.SetClaw, _loadLockClaw);
                    ExecuteRoutine((int)RoutineStep.SetLiftDown, _loadLockLiftDown);
                }
                else if (_target == ModuleName.UnLoad)
                {
                    ExecuteRoutine((int)RoutineStep.SetClaw, _unLoadClaw);
                    ExecuteRoutine((int)RoutineStep.SetLiftDown, _unLoadLiftDown);
                }

                ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);
                TimeDelay((int)RoutineStep.TimeDelay1, 1);
                CheckWaferStatuAfterPlace((int)RoutineStep.CheckWaferStatusByRq, WaferRobot, 10); //结束后检查Wafer            
                ExecuteRoutine((int)RoutineStep.CloseSlitValve, _efemSlitValveCloseRoutine);      //关闭闸板阀
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException ex)
            {
                LOG.Error(ex.ToString());
                return Result.FAIL;
            }

            Notify($"Finish");

            return Result.DONE;
        }

        public override void Abort()
        {
            WaferRobot.Abort();
            WaferRobot.Stop();
            base.Abort();
        }


        protected void AlignerMoveToRobotPutPalce(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Move to Robot Put Place");
                _alignerDevice.MoveToRobotPutPlace();
                return true;
            }, () =>
            {
                return !_alignerDevice.IsBusy;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Move to Robot Put Place Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void AlignerCheckHaveWafer(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Aligner check  wafer exist or not");
                _alignerDevice.CheckWaferLoad();
                return true;
            }, () =>
            {
                if (!_alignerDevice.IsBusy)
                {
                    if (_alignerDevice.HaveWafer)
                    {
                        Notify($"Check result : aligner have wafer, can not place to aligner");
                        return null;
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Aligner Move to Robot Put Place Timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
