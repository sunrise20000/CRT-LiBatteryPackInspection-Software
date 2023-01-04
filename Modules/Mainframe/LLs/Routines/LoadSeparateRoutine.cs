using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadSeparateRoutine : LoadLockBaseRoutine
    {
        /*
         *  1、Load伺服到传盘压力
            2、夹爪打开
            3、顶升晶圆
            4、夹爪闭合
            5、顶升缩回
         * 
         */
        enum RoutineStep
        {
            ClawOpen,
            LiftUp,
            Clawing,
            LiftDown,
            TimeDelay1,
            LoadRotationHome,
            WaitMoveDone,
        }

        private int _clawMoveTimeOut = 30;
        private int _liftMoveTimeOut = 30;
        private int _loadRatationMoveTimeout = 30;

        public LoadSeparateRoutine(ModuleName module)
        {
            Name = "Separate";
            Module = module.ToString();
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _liftMoveTimeOut = SC.GetValue<int>("LoadLock.LiftMoveTimeOut");
            _clawMoveTimeOut = SC.GetValue<int>("LoadLock.ClawMoveTimeOut");
            _loadRatationMoveTimeout = SC.GetValue<int>("LoadLock.LoadRotation.RotationTimeOut");

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //LoadRotation先回原点位置
                MoveHomePos((int)RoutineStep.LoadRotationHome);
                TimeDelay((int)RoutineStep.TimeDelay1, 1);
                WaitLoadRotationDone((int)RoutineStep.WaitMoveDone, _loadRatationMoveTimeout);

                LiftMove((int)RoutineStep.LiftUp, true, _liftMoveTimeOut);
                ClawMove((int)RoutineStep.ClawOpen, WaferClaw, false, _clawMoveTimeOut);
                ClawMove((int)RoutineStep.Clawing, WaferClaw, true, _clawMoveTimeOut);
                LiftMove((int)RoutineStep.LiftDown, false, _liftMoveTimeOut);
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

    }
}
