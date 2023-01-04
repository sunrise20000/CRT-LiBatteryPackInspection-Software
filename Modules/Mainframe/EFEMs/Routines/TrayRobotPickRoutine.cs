using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.LLs.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace Mainframe.EFEMs.Routines
{
    public class TrayRobotPickRoutine : EfemBaseRoutine
    {
        /* Pick From Cassette,LoadLock(需要开门),Aligner/Unload(需要开门)
         * 1.判断目标是否有Wafer
         * 2.判断是否需要开门（包括气压等）
         * 3.判断顶针是否上升
         * 4.伸出,吸住
         * 5.ActionDone
         * 6.关门
         * 
         */

        //private LoadLock _loadLock;
        private EfemSlitValveRoutine _efemSlitValveOpenRoutine = new EfemSlitValveRoutine();
        private EfemSlitValveRoutine _efemSlitValveCloseRoutine = new EfemSlitValveRoutine();
        private LoadLockLiftRoutine _loadLockLiftDown = new LoadLockLiftRoutine();

        private ModuleName _source;
        private int _sourceSlot;
        private int _pickTimeout;

        enum RoutineStep
        {
            OpenSlowVent,
            OpenSlitValve,
            SetLiftUp,
            CheckRobotReady,
            PickComplete,
            SetLiftDown,
            TimeDelay1,
            CloseSlitValve,
            CloseSlowVent,
            SetExtendToDo,
            Pick,
            CheckTrayStatuBeforePick,
            ClearRobortExtendToDo
        }

        public TrayRobotPickRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            Name = "TrayRobortPick";
        }

        public override void Init(ModuleName source, int sourceSlot)
        {
            _source = source;
            _sourceSlot = sourceSlot;
        }


        public override Result Start(params object[] objs)
        {
            Reset();
            if (WaferManager.Instance.CheckHasTray(ModuleName.TrayRobot, 0))
            {
                Stop("Can not pick,TrayRobot has tray");
                return Result.FAIL;
            }
            //Pick之前先,根据Sensor检测是否有盘
            if (!WaferManager.Instance.CheckHasTray(_source, _sourceSlot))
            {
                EV.PostWarningLog(Module, $"Can not pick, {_source} slot {_sourceSlot + 1} has no tray");
                return Result.FAIL;
            }
            if (TrayRobot.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not pick, TrayRobot is not Idle");
                return Result.FAIL;
            }

            if (_source == ModuleName.CassBL)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassBLWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,{_source} check tray convex");
                    return Result.FAIL;
                }
                if (!_cassBL6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not pick,{_source} sensor check no cassette");
                    return Result.FAIL;
                }
            }
            if (_source == ModuleName.LoadLock && !_llLift.IsDown)
            {
                EV.PostWarningLog(Module, $"Can not pick,{_source} lift is not in down position!");
                return Result.FAIL;
            }

            // 如果从LoadLock或UnLoad取Wafer，打开ATM闸板阀前先开始Vent,获取Vent参数
            if (_source == ModuleName.LoadLock || _source == ModuleName.UnLoad)
            {
                if (_source == ModuleName.LoadLock || _source == ModuleName.Load)
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

            _efemSlitValveOpenRoutine.Init(_source, ModuleName.TrayRobot, true);
            _efemSlitValveCloseRoutine.Init(_source, ModuleName.TrayRobot, false);
            _loadLockLiftDown.Init(false);

            _pickTimeout = SC.GetConfigItem($"{ModuleName.TrayRobot}.MotionTimeout").IntValue;

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                if (_source == ModuleName.LoadLock || _source == ModuleName.Load)
                {
                    CheckRobotReady((int)RoutineStep.CheckRobotReady, TrayRobot, _pickTimeout);
                    CheckTrayStatuBeforePick((int)RoutineStep.CheckTrayStatuBeforePick, TrayRobot, 10); //Pick前先检查
                    ExecuteRoutine((int)RoutineStep.OpenSlitValve, _efemSlitValveOpenRoutine);      //打开闸板阀
                    SetTrayRobortExtendToDO((int)RoutineStep.SetExtendToDo, _source, 10);           //设置ExtendToDO,用于检测InterLock

                    Pick((int)RoutineStep.Pick, TrayRobot, _source, _sourceSlot, _pickTimeout);

                    ClearRobortExtendToDO((int)RoutineStep.ClearRobortExtendToDo);
                    TimeDelay((int)RoutineStep.TimeDelay1, 1);
                    ExecuteRoutine((int)RoutineStep.CloseSlitValve, _efemSlitValveCloseRoutine);      //关闭闸板阀
                }
                else
                {
                    CheckRobotReady((int)RoutineStep.CheckRobotReady, TrayRobot, _pickTimeout);
                    CheckTrayStatuBeforePick((int)RoutineStep.CheckTrayStatuBeforePick, TrayRobot, 10); //Pick前先检查
                    Pick((int)RoutineStep.Pick, TrayRobot, _source, _sourceSlot, _pickTimeout);    //机械手到位,夹爪打开
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

        public override void Abort()
        {
            TrayRobot.Abort();
            base.Abort();
        }

    }
}
