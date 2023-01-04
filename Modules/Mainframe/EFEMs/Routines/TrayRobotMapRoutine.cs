using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.EFEMs.Routines
{
    public class TrayRobotMapRoutine : EfemBaseRoutine
    {
        enum RoutineStep
        {
            CheckRobotReady,
            RobotMap,
            TimeDelay1,
        }

        public TrayRobotMapRoutine()
        {
            Module = ModuleName.EFEM.ToString();
            Name = "TrayRobotMap";
        }

        private ModuleName _source;
        private int _motionTimeOut = 10;

        public override void Init(ModuleName source)
        {
            _source = source;
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            if (_source == ModuleName.LoadLock || _source == ModuleName.UnLoad)
            {
                return Result.DONE;
            }

            if (_source == ModuleName.CassBL)
            {
                //检测凸片Sensor和有无Sensor
                if (_cassBLWaferConvex.Value)
                {
                    EV.PostWarningLog(Module, $"Can not map,{_source} check wafer convex");
                    return Result.FAIL;
                }
                if (!_cassBL6Inch.Value)
                {
                    EV.PostWarningLog(Module, $"Can not map,{_source} sensor check no cassette");
                    return Result.FAIL;
                }
            }

            _motionTimeOut =  SC.GetConfigItem($"{ModuleName.TrayRobot}.MapTimeout").IntValue;
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CheckRobotReady((int)RoutineStep.CheckRobotReady, WaferRobot, _motionTimeOut);      //判断机械手当前是否空闲
                Map((int)RoutineStep.RobotMap, TrayRobot, _source, _motionTimeOut);                 //机械手吸住,收回

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
