using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using SicPM.Routines;

namespace SicPM.RecipeRoutine
{
    public class PostPMMacroRoutine : PMBaseRoutine
    {
        enum RoutineStep
        {

        }


        public PostPMMacroRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();  
            Name   = "PostPMMacro";
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
            PMDevice.AbortRunProcess(out string reason);
        }
    }
}
