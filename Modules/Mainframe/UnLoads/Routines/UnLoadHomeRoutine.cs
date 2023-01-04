using System;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadHomeRoutine : UnLoadBaseRoutine
    {
        enum RoutineStep
        {
            CloseV78,
            CloseV122,
            CloseV123
        }

        private int _homeTimeout;

        public UnLoadHomeRoutine()
        {
            Module = ModuleName.UnLoad.ToString();
            Name = "Home";
        }


        public override Result Start(params object[] objs)
        {
            Reset();

            _homeTimeout = SC.GetValue<int>("UnLoad.HomeTimeout");

            Notify("Start");

            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                CloseVent((int)RoutineStep.CloseV78);
                CloseFastPump((int)RoutineStep.CloseV122);
                CloseSlowPump((int)RoutineStep.CloseV123);
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

        public void CloseVent(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close Load vent valve");

                if (!UnLoadDevice.SetFastVentValve(false, out string reason))
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

                if (!UnLoadDevice.SetFastPumpValve(false, out string reason))
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

                if (!UnLoadDevice.SetSlowPumpValve(false, out string reason))
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
