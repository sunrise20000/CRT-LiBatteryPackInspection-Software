using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.UnLoads.Routines
{
     public class UnLoadClawRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            LiftMove,
            ClawMove,
        }


        private bool _llCalw;

        private int _clawMoveTimeOut = 30;

        public UnLoadClawRoutine()
        {
            Name = "UnLoad Claw";
        }

        public void Init(bool isClaw)
        {
            _llCalw = isClaw;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if ((_llCalw && WaferClaw.State == ClawStateEnum.Clamp) || (!_llCalw && WaferClaw.State == ClawStateEnum.Open))
            {
                return Result.DONE;
            }

            _clawMoveTimeOut = SC.GetValue<int>("UnLoad.ClawMoveTimeOut");
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //先动夹爪在动Lift
                ClawMove((int)RoutineStep.ClawMove, WaferClaw, _llCalw, _clawMoveTimeOut);
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