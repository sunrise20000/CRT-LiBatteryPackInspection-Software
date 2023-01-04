using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.EFEMs.Routines
{
    public class EFEMHomeRoutine : EfemBaseRoutine
    {
        enum RoutineStep
        {
            Home,
            Home2,
            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
            WaitReady1,
            WaitReady2
        }
        private int _homeTimeout1;
        private int _homeTimeout2;
        private ModuleName _targetModule;


        public EFEMHomeRoutine()
        {
            Name = "RobotHome";
        }

        public override void Init(ModuleName target)
        {
            _targetModule = target;
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _homeTimeout1 = SC.GetValue<int>("WaferRobot.HomeTimeout");
            _homeTimeout2 = SC.GetValue<int>("TrayRobot.HomeTimeout");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                RobotHomeExcute((int)RoutineStep.Home, WaferRobot, _homeTimeout1);
                RobotHomeExcute((int)RoutineStep.Home2, TrayRobot, _homeTimeout2);
                TimeDelay((int)RoutineStep.TimeDelay1, 2);
                CheckRobotReady((int)RoutineStep.WaitReady1, WaferRobot, _homeTimeout1);
                CheckRobotReady((int)RoutineStep.WaitReady2, TrayRobot, _homeTimeout2);
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
