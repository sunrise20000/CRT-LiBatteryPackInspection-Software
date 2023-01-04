using System;
using System.Collections.Generic;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace Mainframe.TMs.Routines
{
    public class TMRobotHomeRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            SetCommunication,
            SetLoadArm1,
            SetLoadArm2,
            Home,
            CheckLoad1,
            CheckLoad2,
            RequestWaferPresent1,
            RequestWaferPresent2,
            UpdateWaferInfoByRobotSensor,
        }

        private int _homeTimeout;
        private bool _isPriArmWaferPresent;
        private bool _isSecArmWaferPresent;

        public TMRobotHomeRoutine()
        {
            Module = "TMRobot";
            Name = "Home";
        }


        public override Result Start(params object[] objs)
        {
            Reset();

            _homeTimeout = SC.GetValue<int>("TMRobot.HomeTimeout");
            _isPriArmWaferPresent = WaferManager.Instance.CheckHasWafer(Module, 0);
            _isSecArmWaferPresent = WaferManager.Instance.CheckHasWafer(Module, 1);

            RobotDevice.Reset();

            Notify("Start");

            return Result.RUN;
        }

        public override void Abort()
        {
            RobotDevice.Stop();
            Notify("Abort");
        }


        public override Result Monitor()
        {
            try
            {
                //RobotSetCommunication((int)RoutineStep.SetCommunication, RobotDevice, _homeTimeout);

                //RobotSetLoad((int)RoutineStep.SetLoadArm1, RobotDevice, RobotArmEnum.Blade1, _isPriArmWaferPresent, _homeTimeout);

                //RobotSetLoad((int)RoutineStep.SetLoadArm2, RobotDevice, RobotArmEnum.Blade2, _isSecArmWaferPresent, _homeTimeout);

                RobotHome((int)RoutineStep.Home, RobotDevice, _homeTimeout);

                RobotCheckLoad((int)RoutineStep.CheckLoad1, RobotDevice, RobotArmEnum.Blade1, _homeTimeout);

                //RobotCheckLoad((int)RoutineStep.CheckLoad2, RobotDevice, RobotArmEnum.Blade2, _homeTimeout);

                RobotRequestWaferPresent((int)RoutineStep.RequestWaferPresent1, RobotDevice, RobotArmEnum.Blade1, _homeTimeout);

                //RobotRequestWaferPresent((int)RoutineStep.RequestWaferPresent2, RobotDevice, RobotArmEnum.Blade2, _homeTimeout);

                UpdateWaferInfoByRobotSensor((int)RoutineStep.UpdateWaferInfoByRobotSensor, RobotDevice);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException ex)
            {
                LOG.Error(ex.ToString());
                RobotDevice.Stop();
                return Result.FAIL;
            }

            Notify("Finished");

            return Result.DONE;
        }


        //public void RobotSetCommunication(int id, RobotBaseDevice robot, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //        {
        //            Notify("Set default communication");

        //            if (!robot.SetCommunication(out string reason))
        //            {
        //                Stop(reason);
        //                return false;
        //            }

        //            return true;
        //        }, () =>
        //        {
        //            if (!robot.Busy)
        //            {
        //                return true;
        //            }

        //            return false;

        //        }, timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT) //timeout
        //        {
        //            Stop($"timeout over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}


        public void RobotSetLoad(int id, RobotBaseDevice robot, RobotArmEnum hand, bool isWaferPresent, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set default load {hand}");

                List<object> parm = new List<object>() { "SetLoad", hand, isWaferPresent };
                if (!robot.SetParameters(parm.ToArray()))
                {
                    return false;
                }

                return true;
            }, () =>
            {
                if (robot.IsReady())
                {
                    return true;
                }

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void RobotHome(int id, RobotBaseDevice robot, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("Execute home");

                if (!robot.Home(null))
                {
                    EV.PostAlarmLog(Module, $"Can not home robot");
                    return false;
                }

                return true;
            }, () =>
            {
                if (robot.IsReady())
                {
                    return true;
                }

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void RobotCheckLoad(int id, RobotBaseDevice robot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify("Check load");

                List<object> parm = new List<object>() { "CheckLoad", hand };
                if (!robot.ReadParameter(parm.ToArray()))
                {
                    EV.PostAlarmLog(Module, $"Can not check load");
                    return false;
                }

                return true;
            }, () =>
            {
                if (robot.IsReady())
                {
                    return true;
                }

                return false;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"timeout over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


    }
}
