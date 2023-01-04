using System;
using System.Collections.Generic;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace Mainframe.TMs.Routines
{
    public class TMRobotGotoRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            RobotGoto,
        }

        private ModuleName _source;
        private int _sourceSlot;
        private RobotArmEnum _blade;
        private string _EX;
        private string _UP;
        private int _timeout;
        private TMSlitValveRoutine _closeSlitValveRoutine = new TMSlitValveRoutine();
        private double _shutterAndSlitValveMotionInterval;

        public TMRobotGotoRoutine()
        {
            Module = "TMRobot";
            Name = "Goto";
        }

        public void Init(ModuleName source, int sourceSlot, RobotArmEnum blade,string PE,string NE)
        {
            _source = source;
            _sourceSlot = sourceSlot;
            _blade = blade;
            _EX = PE;
            _UP = NE;
        }

        public void Init(ModuleName source, int sourceSlot, int blade,string EX, string UP)
        {
            Init(source, sourceSlot, blade == 0 ? RobotArmEnum.Blade1 : RobotArmEnum.Blade2,EX,UP);
        }
        public override Result Start(params object[] objs)
        {
            Reset();
            
            if (RobotDevice.RobotState != RobotStateEnum.Idle)
            {
                EV.PostWarningLog(Module, $"Can not Goto, TMRobot Is Not IDLE");
                return Result.FAIL;
            }
            _timeout = SC.GetValue<int>("TMRobot.GotoTimeout");

            Notify($"Start, goto {_source} slot {_sourceSlot + 1} with {(_blade == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");

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
                RobotGoto((int)RoutineStep.RobotGoto, RobotDevice, _source, _sourceSlot, _blade, _timeout);
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

            Notify($"Finish, goto {_source} slot {_sourceSlot + 1} with {(_blade == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");

            return Result.DONE;
        }

        private void RobotGoto(int id, RobotBaseDevice robot, ModuleName source, int slot, RobotArmEnum hand, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Goto {source} {slot + 1} use {(hand == RobotArmEnum.Blade1 ? "Blade1" : "Blade2")}");
                List<object> para = new List<object>() { "GoTo", hand, source, slot, _EX == "EX" ? false : true , _UP == "UP" ? false : true };
                if (!robot.GoTo(para.ToArray()))
                {
                    return false;
                }
                return true;
            }, () =>
            {
                if (robot.IsReady())
                    return true;

                return false;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"Goto failed, error {robot.ErrorCode}");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop(string.Format("timeout, can not complete in {0} seconds", timeout));
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
