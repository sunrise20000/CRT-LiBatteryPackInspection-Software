using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mainframe.LLs
{
    public class LoadRotationHomeRoutine : LoadLockBaseRoutine
    {
        enum RoutineStep
        {
            MoveRelativeHome,
            TimeDelay,
            WaitMoveDone,
            MoveHomeOffset,
            TimeDelay2,
            WaitMoveDone2,
        }
        private float _homeOffset;

        public LoadRotationHomeRoutine()
        {
            Name = "Home";
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _homeOffset = (float)SC.GetConfigItem($"LoadLock.LoadRotation.HomeOffset").DoubleValue;

            Notify("Start");

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                if (SC.GetValue<bool>($"System.IsSimulatorMode"))
                {
                    return Result.DONE;
                }

                MoveRelativeHome((int)RoutineStep.MoveRelativeHome, 10);
                TimeDelay((int)RoutineStep.TimeDelay, 1);
                WaitLoadRotationDone((int)RoutineStep.WaitMoveDone, 60);
                MoveLoadRotationHomeOffset((int)RoutineStep.MoveHomeOffset, _homeOffset, 10);
                TimeDelay((int)RoutineStep.TimeDelay2, 1);
                WaitLoadRotationDone((int)RoutineStep.WaitMoveDone2, 60);
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

        public override void Abort()
        {
            base.Abort();
        }

    }
}
