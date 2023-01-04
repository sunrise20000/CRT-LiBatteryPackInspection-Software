using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using Mainframe.LLs.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace Mainframe.EFEMs.Routines
{
    public class TrayRobotPlaceRoutine : EfemBaseRoutine
    {
        /* 1.Place to LoadLock(需要开门)/Cassette
         * 2.判断目标是否无Tray，判断Robot是否有Tray
         * 3.判断是否需要开门（包括气压等）
         * 4.判断顶针是否下降
         * 5.伸出
         * 6.托盘旋转到定位角
         * 7.托盘对中（夹紧放开）
         * 8.关门
         */

        private LoadRotationHomeOffsetRoutine _loadRotationHomeRoutine = new LoadRotationHomeOffsetRoutine();
        private EfemSlitValveRoutine _efemSlitValveOpenRoutine = new EfemSlitValveRoutine();
        private EfemSlitValveRoutine _efemSlitValveCloseRoutine = new EfemSlitValveRoutine(); 
        private LoadLockLiftRoutine _loadLockLiftDown = new LoadLockLiftRoutine();
        //private LoadLockTrayAlignerRoutine _trayAligner = new LoadLockTrayAlignerRoutine();
        private LoadLockTrayClawRoutine _trayClamp = new LoadLockTrayClawRoutine();
        private LoadLockTrayClawRoutine _trayUnClamp = new LoadLockTrayClawRoutine();

        private IoLoadRotation _llRotation = null;
        

        private ModuleName _source;
        private int _sourceSlot;
        private int _placeTimeout;
        private double _homeOffset;
        private int _loadRotationTimeOut = 60;


        enum RoutineStep
        {
            OpenSlowVent,
            OpenSlitValve,
            SetLiftUp,
            CheckRobotReady,
            PickComplete,
            SetLiftDown,
            Place,
            TrayAligner,
            TrayClamp,
            TrayUnClamp,
            CloseSlitValve,
            CloseSlowVent,
            TrayUnClamp1,
            LoadRotationRelativeHome,

            FindPositionSensor,
            SetExtendToDo,
            ClearRobortExtendToDo,
            CheckTraySensor,

            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
            TimeDelay4
        }

        public TrayRobotPlaceRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            Name = "TrayRobortPlace";

            _llRotation = DEVICE.GetDevice<IoLoadRotation>("Load.Rotation");
        }

        public override void Init(ModuleName sourseMod,int sourceSlt)
        {
            _source = sourseMod;
            _sourceSlot = sourceSlt;
        }
      
        public override Result Start(params object[] objs)
        {
            Reset();
            if (!WaferManager.Instance.CheckHasTray(ModuleName.TrayRobot, 0))
            {
                Stop("Can not place,TrayRobot no tray");
                return Result.FAIL;
            }
            //Place之前先,根据Sensor检测是否有盘
            if (WaferManager.Instance.CheckHasTray(_source, _sourceSlot))
            {
                EV.PostWarningLog(Module, $"Can not place, {_source} slot {_sourceSlot + 1} has tray");
                return Result.FAIL;
            }
            if (TrayRobot.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not place, WaferRobot is not Idle");
                EV.PostWarningLog(Module, $"Can not place, TrayRobot is not Idle");
                return Result.FAIL;
            }

            //LoadLock传感器是否需要检测有Tray盘
            if (_source == ModuleName.LoadLock || _source == ModuleName.Load)
            {
                if (_loadTrayPresence.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,{_source} sensor[DI-32] check have tray");
                    return Result.FAIL;
                }
                if (!_llLift.IsDown)
                {
                    EV.PostWarningLog(Module, $"Can not place,{_source} lift is not in down position!");
                    return Result.FAIL;
                }
            }
            else if (_source == ModuleName.CassBL)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassBLWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,{_source} check tray convex");
                    return Result.FAIL;
                }
                if (!_cassBL6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not place,{_source} sensor check no cassette");
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

            _loadRotationTimeOut = SC.GetValue<int>("LoadLock.LoadRotation.RotationTimeOut");

            _efemSlitValveOpenRoutine.Init(_source, ModuleName.TrayRobot, true);
            _efemSlitValveCloseRoutine.Init(_source, ModuleName.TrayRobot, false);
            _loadLockLiftDown.Init(false);
            _trayClamp.Init(true);
            _trayUnClamp.Init(false);

            _placeTimeout = SC.GetConfigItem($"{ModuleName.TrayRobot}.MotionTimeout").IntValue;
            _homeOffset = SC.GetConfigItem($"LoadLock.LoadRotation.HomeOffset").DoubleValue; 

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                /* DI_LoadTrayPresence 托盘有无
                 * DI_LoadWaferPlaced Wafer有无
                 * DI_LoadHomeTraySensor 托盘测距
                 */

                // 如果从LoadLock或UnLoad取Wafer，打开ATM闸板阀前先开始Vent
                /*if (_source == ModuleName.LoadLock || _source == ModuleName.UnLoad)
                {
                    SlowVent((int)RoutineStep.OpenSlowVent, _source, _ventBasePressure, _slowVentTimeout);
                }*/


                if (_source == ModuleName.LoadLock || _source == ModuleName.Load)
                {
                    
                    ExecuteRoutine((int)RoutineStep.TrayUnClamp1, _trayUnClamp);      //夹爪打开

                    ExecuteRoutine((int)RoutineStep.OpenSlitValve, _efemSlitValveOpenRoutine);          //打开闸板阀
                    SetTrayRobortExtendToDO((int)RoutineStep.SetExtendToDo, _source, 10);           //设置ExtendToDO,用于检测InterLock
                    CheckRobotReady((int)RoutineStep.CheckRobotReady, TrayRobot, _placeTimeout);       //判断机械手当前是否空闲
                    Place((int)RoutineStep.Place, TrayRobot, _source, _sourceSlot, _placeTimeout);     //机械手 
                    ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);

                    CheckTraySensor((int)RoutineStep.CheckTraySensor);

                    //对中
                    ExecuteRoutine((int)RoutineStep.TrayClamp, _trayClamp);        //夹爪关闭
                    TimeDelay((int)RoutineStep.TimeDelay2, 1);//延迟1s
                    ExecuteRoutine((int)RoutineStep.TrayUnClamp, _trayUnClamp);      //夹爪打开

                    //ExecuteRoutine((int)RoutineStep.LoadRotationRelativeHome, _loadRotationHomeRoutine);      //Tray找原点

                    //TimeDelay((int)RoutineStep.TimeDelay1, 1);
                    ExecuteRoutine((int)RoutineStep.CloseSlitValve, _efemSlitValveCloseRoutine);      //关闭闸板阀
                }
                else
                {
                    CheckRobotReady((int)RoutineStep.CheckRobotReady, TrayRobot, _placeTimeout);       //判断机械手当前是否空闲
                    SetTrayRobortExtendToDO((int)RoutineStep.SetExtendToDo, _source, 10);           //设置ExtendToDO,用于检测InterLock
                    Place((int)RoutineStep.Place, TrayRobot, _source, _sourceSlot, _placeTimeout);     //机械手 
                    ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);
                }
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


    }
}
