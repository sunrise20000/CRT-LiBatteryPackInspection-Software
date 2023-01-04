using System.Diagnostics;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.Aligners.Routines
{
    public class AlignerAlignRoutine :AlignerBaseRoutine
    {
        enum RoutineStep
        {
            CheckWafer,
            OpenVacuum,
            Aligner,
            CloseVacuum,
            TimeDelay1,
            AlignerMoveTo
        }


        private Stopwatch _swTimer = new Stopwatch();
        private int _alignerTimeOut = 60;

        public AlignerAlignRoutine()
        {
            Module = ModuleName.Aligner.ToString();
            Name = "Align";
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _swTimer.Restart();
            _alignerTimeOut= SC.GetValue<int>("HiWinAligner.AlignerTimeout");
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

                CheckWafer((int)RoutineStep.CheckWafer, 10);

                AlignerMove((int)RoutineStep.Aligner, _alignerTimeOut);

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            return Result.DONE;
        }


    }
}
