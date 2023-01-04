using Aitex.Core.RT.Routine;
using Aitex.Core.Util;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadCoolingRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            TimeDealy
        }

        private DeviceTimer _dt = new DeviceTimer();

        private int _coolingTime;

        public UnLoadCoolingRoutine()
        {
            Name = "UnLoad Cooling";
        }

        public void Init(int coolingTime)
        {
            _coolingTime = coolingTime;
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _dt.Start(0);
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                Delay((int)RoutineStep.TimeDealy, _coolingTime);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }
            _dt.Stop();
            Notify("Finished");

            return Result.DONE;
        }

        public override void Abort()
        {

        }

        public int GetRemainedTime()
        {
            if (!_dt.IsIdle())
                return _coolingTime - (int)(_dt.GetElapseTime() / 1000);
            return 0;
        }

    }
}
