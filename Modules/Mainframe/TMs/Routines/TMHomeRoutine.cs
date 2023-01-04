using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.TMs.Routines
{
    public class TMHomeRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            CloseV77,
            CloseV80,
            TMRobotHome
        }

        private int _homeTimeout;

        private TMRobotHomeRoutine _tmRobotHomeRoutine =
             new TMRobotHomeRoutine();

        public TMHomeRoutine()
        {
            Module = "TM";
            Name = "Home";
        }


        public override Result Start(params object[] objs)
        {
            Reset();

            _homeTimeout = SC.GetValue<int>("TM.HomeTimeout");

            _tmRobotHomeRoutine = new TMRobotHomeRoutine();

            Notify("Start");

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CloseTMVent((int)RoutineStep.CloseV77);
                //CloseBufferVent((int)RoutineStep.CloseV80);
                ExecuteRoutine((int)RoutineStep.TMRobotHome, _tmRobotHomeRoutine);
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

            Notify("Finished");

            return Result.DONE;
        }

        public override  void Abort()
        {
            base.Abort();
        }

    }
}
