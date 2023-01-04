using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadLiftRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            LiftMove
        }

        private bool _pinUp;


        private int _liftMoveTimeOut = 30;

        public UnLoadLiftRoutine()
        {
            Name = "UnLoad Lift";
        }

        public void Init(bool isUp)
        {
            _pinUp = isUp;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _liftMoveTimeOut = SC.GetValue<int>("UnLoad.LiftMoveTimeOut");

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //先动夹爪在动Lift
                LiftMove((int)RoutineStep.LiftMove, _pinUp, _liftMoveTimeOut);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify("Finished");

            return Result.DONE;
        }

        public override void Abort()
        {

        }


    }
}
