using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;

namespace Mainframe.UnLoads.Routines
{
    public class UnLoadSeparateRoutine : UnLoadBaseRoutine
    {
        /*
         *  1、Unload伺服到传盘压力
            2、夹爪打开
            3、顶升晶圆
            4、夹爪闭合
            5、顶升缩回
            6、打开闸板阀
            7、Robot开始取托盘到TM
            8、关闭闸板阀
         * 
         */
        enum RoutineStep
        {
            ClawOpen,
            Delay1,
            LiftUp,
            Delay2,
            Clawing,
            Delay3,
            LiftDown,
            Delay4
        }

        private int _clawMoveTimeOut = 30;
        private int _liftMoveTimeOut = 30;

        public UnLoadSeparateRoutine()
        {
            Name = "Separate";
        }

        public override Result Start(params object[] objs)
        {
            Reset();
            _liftMoveTimeOut = SC.GetValue<int>("UnLoad.LiftMoveTimeOut");
            _clawMoveTimeOut = SC.GetValue<int>("UnLoad.ClawMoveTimeOut");

            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                ClawMove((int)RoutineStep.ClawOpen, WaferClaw, false, _clawMoveTimeOut);
                TimeDelay((int)RoutineStep.Delay1, 2);
                LiftMove((int)RoutineStep.LiftUp, true, _liftMoveTimeOut);
                TimeDelay((int)RoutineStep.Delay2, 2);

                ClawMove((int)RoutineStep.Clawing, WaferClaw, true, _clawMoveTimeOut);
                TimeDelay((int)RoutineStep.Delay3, 3);
                LiftMove((int)RoutineStep.LiftDown, false, _liftMoveTimeOut);
                TimeDelay((int)RoutineStep.Delay4, 2);
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
