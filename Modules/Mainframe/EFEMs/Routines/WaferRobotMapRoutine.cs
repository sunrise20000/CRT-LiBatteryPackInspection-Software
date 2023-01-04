using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.EFEMs.Routines
{
    public class WaferRobotMapRoutine : EfemBaseRoutine
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

        private ModuleName _source;
        private int _pickTimeout;


        enum RoutineStep
        {
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
            Pick,
        }

        public WaferRobotMapRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            Name = "WaferRobotMap";
        }

        public override void Init(ModuleName sourseMod)
        {
            _source = sourseMod;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (!ModuleHelper.IsCassette(_source))
            {
                return Result.DONE;
            }
            if (_source == ModuleName.CassAL)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassALWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not map,{_source} check wafer convex");
                    return Result.FAIL;
                }
                if (!_cassAL6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not map,{_source} sensor check no cassette");
                    return Result.FAIL;
                }

            }
            else if (_source == ModuleName.CassAR)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassARWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not map,{_source} check wafer convex");
                    return Result.FAIL;
                }
                if (!_cassAR6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not map,{_source} sensor check no cassette");
                    return Result.FAIL;
                }

            }

            _pickTimeout = SC.GetConfigItem($"{ModuleName.WaferRobot}.MotionTimeout").IntValue; 
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CheckRobotReady((int)RoutineStep.CheckRobotReady, WaferRobot, _pickTimeout);    //判断机械手当前是否空闲
                Map((int)RoutineStep.Pick, WaferRobot, _source,  _pickTimeout);    //机械手吸住,收回
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
