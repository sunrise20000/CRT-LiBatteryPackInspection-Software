using System;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadHomeRoutine : LoadLockBaseRoutine
    {
        enum RoutineStep
        {
            CloseV79,
            CloseV83,
            CloseV84
        }

        private int _homeTimeout;

        public LoadHomeRoutine(ModuleName module)
        {
            Module = module.ToString();
            Name = "Home";
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _homeTimeout = SC.GetValue<int>("LoadLock.HomeTimeout");

            Notify("Start");

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CloseLoadVent((int)RoutineStep.CloseV79);
                CloseFastPump((int)RoutineStep.CloseV83);
                CloseSlowPump((int)RoutineStep.CloseV84);

                LoadLockDevice.AutoCreatWafer();
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

        public void CloseLoadVent(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close Load vent valve");

                if (!LoadLockDevice.SetFastVentValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseFastPump(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close Load fast pump valve");

                if (!LoadLockDevice.SetFastPumpValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowPump(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close slow pump valve");

                if (!LoadLockDevice.SetSlowPumpValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public override void Abort()
        {
            base.Abort();
        }

    }
}
