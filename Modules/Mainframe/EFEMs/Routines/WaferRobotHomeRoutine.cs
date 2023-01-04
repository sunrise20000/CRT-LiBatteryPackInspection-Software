using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.EFEMs.Routines
{
    public class WaferRobotHomeRoutine : EfemBaseRoutine
    {
        enum RoutineStep
        {
            Home,
            TimeDelay,
            WaitReady,
        }
        private int _homeTimeout1;


        public WaferRobotHomeRoutine()
        {
            Name = "WaferRobotHome";
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _homeTimeout1 = SC.GetValue<int>("WaferRobot.HomeTimeout");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                RobotHomeExcute((int)RoutineStep.Home, WaferRobot, _homeTimeout1);
                TimeDelay((int)RoutineStep.TimeDelay, 2);
                CheckRobotReady((int)RoutineStep.WaitReady, WaferRobot, _homeTimeout1);
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
