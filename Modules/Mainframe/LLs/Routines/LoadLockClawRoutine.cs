using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.LLs.Routines
{
    public class LoadLockClawRoutine : LoadLockBaseRoutine
    {
        enum RoutineStep
        {
            LiftMove,
            ClawMove,
        }

        private bool _llCalw;
        private bool _isWaferClaw;
        private int _clawMoveTimeOut = 30;

        public LoadLockClawRoutine()
        {
            Name = "Claw";
        }

        public void Init(bool isWaferClaw,bool clawed)
        {
            _llCalw = clawed;
            _isWaferClaw = isWaferClaw;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (_isWaferClaw)
            {
                if ((_llCalw && WaferClaw.State == ClawStateEnum.Clamp) || (!_llCalw && WaferClaw.State == ClawStateEnum.Open))
                {
                    return Result.DONE;
                }
            }
            else
            {
                if ((_llCalw && TrayClaw.State == ClawStateEnum.Clamp) || (!_llCalw && TrayClaw.State == ClawStateEnum.Open))
                {
                    return Result.DONE;
                }
            }

            _clawMoveTimeOut = SC.GetValue<int>("LoadLock.ClawMoveTimeOut");

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {

                ClawMove((int)RoutineStep.ClawMove, (_isWaferClaw ? WaferClaw : TrayClaw), _llCalw, _clawMoveTimeOut);
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
