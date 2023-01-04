using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace SicPM.Routines
{
    public class PMStopHeatEnableRoutine : PMBaseRoutine
    {
        enum RoutineStep
        {
            SetPSUDisable,
            SetSCRDisable,
        }

        private int _heatTimeOut = 5;

        public PMStopHeatEnableRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "StopEnable";
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
                SetPSUEnable((int)RoutineStep.SetPSUDisable, false, _heatTimeOut);

                SetSCREnable((int)RoutineStep.SetSCRDisable, false, _heatTimeOut);

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
    }
}
