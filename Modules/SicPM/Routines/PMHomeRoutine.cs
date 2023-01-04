using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;

namespace SicPM.Routines
{
    public class PMHomeRoutine : PMBaseRoutine
    {
        enum RoutineStep
        {          
            Home,
 
        }
 
        private int _timeout;
 
        public PMHomeRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();  
            Name   = "Home";
        }

        public Result Init( )
        {
 
            return Result.DONE;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _timeout = SC.GetValue<int>("PM.HomeTimeout");
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                if (PMDevice != null && PMDevice.IsInstalled)
                {
                    Home((int)RoutineStep.Home, PMDevice, _timeout);
                }

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

        public void Home(int id, PMModuleBase pm,  int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
                {
                    Notify($"Run {pm.Name} home");

                    //pm.Home();

                    return true;
                }, () =>
                {
                    if (pm.IsError)
                    {
                        return null;
                    }

                    return true;

                }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"{pm.Name} error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
