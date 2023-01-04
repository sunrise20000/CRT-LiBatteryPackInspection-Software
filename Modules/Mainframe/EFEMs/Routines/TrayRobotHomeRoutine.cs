using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.EFEMs.Routines
{
    public class TrayRobotHomeRoutine : EfemBaseRoutine
    {
        enum RoutineStep
        {
            Home,
            TimeDelay,
            WaitReady,
        }
        private int _homeTimeout;


        public TrayRobotHomeRoutine()
        {
            Name = "TrayRobotHome";
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _homeTimeout = SC.GetValue<int>("TrayRobot.HomeTimeout");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                RobotHomeExcute((int)RoutineStep.Home, TrayRobot, _homeTimeout);
                TimeDelay((int)RoutineStep.TimeDelay, 2);
                CheckRobotReady((int)RoutineStep.WaitReady, TrayRobot, _homeTimeout);
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
