using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.Aligners.Routines
{
    public class FeederHomeRoutine : FeederBaseRoutine
    {
        enum RoutineStep
        {
            Home,
            SME,
            MsgCheckWafer,
            TimeDelay1,
        }

        public FeederHomeRoutine()
        {
            Name = "Home";
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            Notify("Start");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                if (SC.GetValue<bool>($"System.IsSimulatorMode"))
                {
                    TimeDelay((int)RoutineStep.TimeDelay1, 3);
                    return Result.DONE;
                }

                Home((int)RoutineStep.Home, 40);
                MsgCheckHaveWafer((int)RoutineStep.MsgCheckWafer, 30);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }
            return Result.DONE;
        }


    }
}
