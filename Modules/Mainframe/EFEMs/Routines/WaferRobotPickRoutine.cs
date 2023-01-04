using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.LLs;
using Mainframe.LLs.Routines;
using Mainframe.UnLoads;
using Mainframe.UnLoads.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad;

namespace Mainframe.EFEMs.Routines
{
    public class WaferRobotPickRoutine : EfemBaseRoutine
    {
        /* Pick From Cassette,LoadLock(需要开门),Aligner/Unload(需要开门)
         * 1.判断目标是否有Wafer
         * 2.判断是否需要开门（包括气压等）
         * 3.判断顶针是否上升
         * 4.伸出,吸住
         * 5.ActionDone
         * 6.关门
         * 
         * 
         * 5.顶针顶起
6.Wafer机械手伸进来
7.夹爪松开
8.顶针下降
9.wafer机械手吸住
10.Wafer机械手退出
         */

        //private LoadLock _loadLock;
        private EfemSlitValveRoutine _efemSlitValveOpenRoutine = new EfemSlitValveRoutine();
        private EfemSlitValveRoutine _efemSlitValveCloseRoutine = new EfemSlitValveRoutine();
        private UnLoadLiftRoutine _unLoadLiftDown = new UnLoadLiftRoutine();
        private UnLoadLiftRoutine _unLoadLiftUp = new UnLoadLiftRoutine();
        private UnLoadClawRoutine _unLoadClaw = new UnLoadClawRoutine();
        private UnLoadClawRoutine _unLoadClawOpen = new UnLoadClawRoutine();

        private LoadLockLiftRoutine _loadLockLiftDown = new LoadLockLiftRoutine();
        private LoadLockLiftRoutine _loadLockLiftUp = new LoadLockLiftRoutine();
        private LoadLockClawRoutine _loadLockClaw = new LoadLockClawRoutine();
        private LoadLockClawRoutine _loadLockClawOpen= new LoadLockClawRoutine();

        private LoadRotationHomeOffsetRoutine _loadRotationHomeRoutine = new LoadRotationHomeOffsetRoutine();

        //private AlignerAlignRoutine _alignRoutine = new AlignerAlignRoutine();

        private ModuleName _source;
        private int _sourceSlot;
        private int _pickTimeout;
        private bool ClampHasWafer = false;
        private bool _isSimulator = false;


        enum RoutineStep
        {
            CheckWaferStatuBeforePick,
            OpenSlitValve,
            SetClawOpen,
            SetLiftUp,
            CheckRobotReady,
            SetClawOpen2,
            SetClaw,
            PickComplete,
            SetLiftDown,
            SetLiftDown2,
            TimeDelay1,
            ExtendForPick,
            CloaseSlitValve,
            Pick,
            CloseVacuum,
            SetExtendToDo,
            ClearRobortExtendToDo,
            CheckWaferStatusByRq,
            LoadRotationHome,
        }

        public WaferRobotPickRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            Name = "WaferRobortPick";
        }

        public override void Init(ModuleName source, int sourceSlot)
        {
            _source = source;
            _sourceSlot = sourceSlot;
        }


        public override Result Start(params object[] objs)
        {
            Reset();
            if (WaferManager.Instance.CheckHasWafer(ModuleName.WaferRobot, 0))
            {
                Stop("Can not pick,WaferRobot has wafer");
                return Result.FAIL;
            }

            //Pick之前先,根据Sensor检测是否有盘
            if (!WaferManager.Instance.CheckHasWafer(_source, _sourceSlot))
            {
                EV.PostWarningLog(Module, $"Can not pick, {_source} slot {_sourceSlot} no wafer");
                return Result.FAIL;
            }
            if (_source == ModuleName.Load || _source == ModuleName.LoadLock)
            {
                if (_loadWaferClaw.State == ClawStateEnum.Open && !_llLift.IsDown)//夹爪打开,顶针在低位,说明没Wafer
                {
                    EV.PostWarningLog(Module, $"Can not pick, Load Claw is open,Make sure wafer and Tray has Separated!");
                    return Result.FAIL;
                }
            }

            if (_source == ModuleName.UnLoad )
            {
                if (_unLoadWaferClaw.State == ClawStateEnum.Open && !_unLoadLift.IsDown)//夹爪打开,顶针在低位,说明没Wafer
                {
                    EV.PostWarningLog(Module, $"Can not pick, UnLoad Claw is open,Make sure wafer and Tray has Separated!");
                    return Result.FAIL;
                }
            }

            if (WaferRobot.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not pick, WaferRobot is not Idle");
                return Result.FAIL;
            }


            if (_source == ModuleName.CassAL)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassALWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,{_source} check wafer convex");
                    return Result.FAIL;
                }
                if (!_cassAL6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,{_source} sensor check no cassette");
                    return Result.FAIL;
                }
                if (WaferManager.Instance.GetWafer(_source, _sourceSlot).Status != WaferStatus.Normal)
                {
                    EV.PostWarningLog(Module, $"Can not pick,wafer statu is error!");
                    return Result.FAIL;
                }

            }
            else if (_source == ModuleName.CassAR)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassARWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,{_source} check wafer convex");
                    return Result.FAIL;
                }
                if (!_cassAR6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,{_source} sensor check no cassette");
                    return Result.FAIL;
                }
                if (WaferManager.Instance.GetWafer(_source, _sourceSlot).Status != Aitex.Core.Common.WaferStatus.Normal)
                {
                    EV.PostWarningLog(Module, $"Can not pick,wafer statu is error!");
                    return Result.FAIL;
                }
            }

            // 如果从LoadLock或UnLoad取Wafer，打开ATM闸板阀前先开始Vent,获取Vent参数
            if (_source == ModuleName.LoadLock || _source == ModuleName.UnLoad)
            {
                if (_source == ModuleName.LoadLock)
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

            _isSimulator = SC.GetValue<bool>($"System.IsSimulatorMode");

            _efemSlitValveOpenRoutine.Init(_source, ModuleName.WaferRobot, true);
            _efemSlitValveCloseRoutine.Init(_source, ModuleName.WaferRobot, false);
            _unLoadLiftDown.Init(false);
            _unLoadLiftUp.Init(true);
            _unLoadClaw.Init(true);
            _unLoadClawOpen.Init(false);

            _loadLockLiftDown.Init(false);
            _loadLockLiftUp.Init(true);
            _loadLockClaw.Init(true,true);
            _loadLockClawOpen.Init(true,false);

            _pickTimeout = SC.GetConfigItem($"{ModuleName.WaferRobot}.MotionTimeout").IntValue;
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                if (_source == ModuleName.Aligner && !_isSimulator)
                {
                    //关Aligner真空吸
                    CloseVacuum((int)RoutineStep.CloseVacuum, 15);
                }
                CheckRobotReady((int)RoutineStep.CheckRobotReady, WaferRobot, _pickTimeout);    //判断机械手当前是否空闲
                CheckWaferStatuBeforePick((int)RoutineStep.CheckWaferStatuBeforePick, WaferRobot, 10); //开始前先检查Wafer
                
                //执行Load Tray 找原点
                if ((_source == ModuleName.Load || _source == ModuleName.LoadLock) && WaferManager.Instance.CheckHasTray(_source, _sourceSlot))
                {
                    ExecuteRoutine((int)RoutineStep.LoadRotationHome, _loadRotationHomeRoutine);
                }


                ExecuteRoutine((int)RoutineStep.OpenSlitValve, _efemSlitValveOpenRoutine);      //打开闸板阀
                SetWaferRobortExtendToDO((int)RoutineStep.SetExtendToDo, _source, 10);           //设置ExtendToDO,用于检测InterLock

                if (_source == ModuleName.UnLoad)
                {
                    ExecuteRoutine((int)RoutineStep.SetLiftUp, _unLoadLiftUp);              //UnLoad,顶针上升
                }
                else if (_source == ModuleName.LoadLock || _source == ModuleName.Load)
                {
                    ExecuteRoutine((int)RoutineStep.SetLiftUp, _loadLockLiftUp);              //Load,顶针上升
                }

                ExtendForPick((int)RoutineStep.ExtendForPick, WaferRobot, _source, _sourceSlot, _pickTimeout);    //机械手到位

                if(_source == ModuleName.UnLoad)
                {
                    ExecuteRoutine((int)RoutineStep.SetClawOpen, _unLoadClawOpen);             //UnLoad夹爪松开
                    ExecuteRoutine((int)RoutineStep.SetLiftDown, _unLoadLiftDown);
                }
                else if (_source == ModuleName.LoadLock || _source == ModuleName.Load)
                {
                    ExecuteRoutine((int)RoutineStep.SetClawOpen, _loadLockClawOpen);             //Load夹爪松开
                    ExecuteRoutine((int)RoutineStep.SetLiftDown, _loadLockLiftDown);
                }

                Pick((int)RoutineStep.Pick, WaferRobot, _source, _sourceSlot, _pickTimeout);    //机械手吸住,收回
                ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);
                TimeDelay((int)RoutineStep.TimeDelay1, 1);
                ExecuteRoutine((int)RoutineStep.CloaseSlitValve, _efemSlitValveCloseRoutine);      //关闭闸板阀
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
            base.Abort();
        }
    }
}
