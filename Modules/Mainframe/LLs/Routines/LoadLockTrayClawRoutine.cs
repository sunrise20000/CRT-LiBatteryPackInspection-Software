using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;

namespace Mainframe.LLs.Routines
{
    public class LoadLockTrayClawRoutine : LoadLockBaseRoutine
    {
        enum RoutineStep
        {
            ClawMove,
        }

        private IoClaw _llTrayClaw = null;
        private bool _clamp;

        public LoadLockTrayClawRoutine()
        {
            Name = "Lift Move";
            _llTrayClaw = DEVICE.GetDevice<IoClaw>($"{Module}.LLTrayClaw");
        }

        public void Init(bool isClamp)
        {
            _clamp = isClamp;
        }


        public override Result Start(params object[] objs)
        {
            Reset();

            if (_clamp && _llTrayClaw.State ==  ClawStateEnum.Clamp)
            {
                return Result.DONE;
            }
            else if (!_clamp && _llTrayClaw.State ==  ClawStateEnum.Open)
            {
                return Result.DONE;
            }

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //先动夹爪在动Lift
                ClawMove((int)RoutineStep.ClawMove, _llTrayClaw, _clamp, 15);
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
