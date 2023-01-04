using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Routine;
using MECF.Framework.Common.Equipment;

namespace Mainframe.Buffers.Routines
{
    class BufferHomeRoutine : ModuleRoutine, IRoutine
    {
        enum RoutineStep
        {
            ClearError,
            Home,
            QueryStatus,
        }

        private int _timeout = 0;

        private SicBuffer _buffer = null;

        public BufferHomeRoutine(ModuleName module)
        {
            Module = module.ToString();

            Name = "Home";

            _buffer = DEVICE.GetDevice<SicBuffer>($"{module}.{module}");
        }


        public bool Initalize()
        {
            return true;
        }
 
        public Result Start(params object[] objs)
        {
            Reset();

            Notify($"Start");

            return Result.RUN;
        }


        public Result Monitor()
        {
            try
            {
                Home((int)RoutineStep.Home, _buffer, 2);
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

        public void Home(int id, SicBuffer buffer, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Start home {buffer.Name}");

                return true;
            }, () =>
                {
 
                return true;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop(string.Format("Home failed."));
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("Home timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void Abort()
        {
            
        }
 
    }
}
