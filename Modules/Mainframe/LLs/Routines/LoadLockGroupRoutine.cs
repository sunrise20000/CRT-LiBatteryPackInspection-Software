using System;
using System.Linq;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.LLs.Routines
{
    public class LoadLockGroupRoutine : LoadLockBaseRoutine
    {
        /*
         *  8、顶升上升
            9、夹爪打开
            10、下降wafer
            11、托盘旋转一周，进行距离判断
            12、旋转到指定角度，视觉判断
            13、抽真空

         * 旋转一周,检测到未放好，报错，人为干预
         * 拍照确认指令(Ok,NG)NG则报错，人为干预
         */
        enum RoutineStep
        {
            CCDModeSet,
            LiftUp,
            ClawOpen,
            LiftDown,
            TryResetServo,
            MoveOneCircle,//旋转一周检查上测距Sensor是否感应到
            TimeDelay1,
            WaitMoveOneCircleDone,
            MoveCCD1Pos,//运动到CCDPos1
            TimeDelay2,
            TimeDelay3,
            TimeDelay4,
            TimeDelay5,
            WaitMoveDone,
            TrigCCD1Pos,//CCDPos1拍照
            MoveCCD2Pos,//运动到CCDPos2
            TrigCCD2Pos,//CCDPos2拍照
            LoadRotationRelativeHome,

        }

        private float _ccdPos1;
        private float _ccdPos2;
        private int _loadRatationResetTimeout = 5;
        private int _loadRatationMoveTimeout = 30;
        private int _liftMoveTimeOut = 30;
        private int _clawMoveTimeOut = 30;

        private bool _isSimulator = false;
        private bool _enableCCDCheck = false;
        private bool _enableWarpageTest = false;
        private LoadRotationHomeOffsetRoutine _loadRotationHomeRoutine = new LoadRotationHomeOffsetRoutine();

        public LoadLockGroupRoutine(ModuleName module)
        {
            Module = module.ToString();
            Name = "TrayGroup";
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            Notify("Start");

            _liftMoveTimeOut = SC.GetValue<int>("LoadLock.LiftMoveTimeOut");
            _clawMoveTimeOut = SC.GetValue<int>("LoadLock.ClawMoveTimeOut");
            _loadRatationMoveTimeout = SC.GetValue<int>("LoadLock.LoadRotation.RotationTimeOut");
            _ccdPos1 = (float)SC.GetValue<double>("LoadLock.LoadRotation.CCD1Pos");
            _ccdPos2 = (float)SC.GetValue<double>("LoadLock.LoadRotation.CCD2Pos");
            _isSimulator = SC.GetValue<bool>($"System.IsSimulatorMode");
            _enableCCDCheck = SC.GetValue<bool>($"LoadLock.EnableCCDCheck");
            _enableWarpageTest = SC.GetValue<bool>($"LoadLock.EnableDistanceSensorCheck");

            return Result.RUN;
        }

        Result _warpageMeasResult;
        Result _ccdCheckResult;
        public override Result Monitor()
        {
            try
            {
                TryResetServo((int)RoutineStep.TryResetServo, _loadRatationResetTimeout);

                //第一步Tray找原点
                ExecuteRoutine((int)RoutineStep.LoadRotationRelativeHome, _loadRotationHomeRoutine);
                TimeDelay((int)RoutineStep.TimeDelay1, 3);
                LiftMove((int)RoutineStep.LiftUp, true, _liftMoveTimeOut);
                ClawMove((int)RoutineStep.ClawOpen, WaferClaw, false, _clawMoveTimeOut);
                LiftMove((int)RoutineStep.LiftDown, false, _liftMoveTimeOut);
                TimeDelay((int)RoutineStep.TimeDelay2, 3);

                if (!_isSimulator)
                {
                    if (_enableWarpageTest)
                    {
                        MoveOneCircle((int)RoutineStep.MoveOneCircle, _loadRatationMoveTimeout); //旋转1周检查上方测距Sensor
                        TimeDelay((int)RoutineStep.TimeDelay3, 3);
                        _warpageMeasResult = WaitMoveOneCircleDone((int)RoutineStep.WaitMoveOneCircleDone, _loadRatationMoveTimeout);
                    }

                    TimeDelay((int)RoutineStep.TimeDelay4, 3);

                    if (_enableCCDCheck)
                    {
                        CCDModeSet((int)RoutineStep.CCDModeSet);

                        MoveCCD1Pos((int)RoutineStep.MoveCCD1Pos, _loadRatationMoveTimeout); //移动到CCD位置1
                        TimeDelay((int)RoutineStep.TimeDelay5, 3);
                        WaitLoadRotationDone((int)RoutineStep.WaitMoveDone, _loadRatationMoveTimeout);

                        _ccdCheckResult = CCDTrigger((int)RoutineStep.TrigCCD1Pos, 10); //拍照
                    }

                    // 翘曲测试结果
                    var warpageMeasNg = false;
                    
                    if (_enableWarpageTest)
                    {
                        if (_warpageMeasResult == Result.Succeed)
                        {
                            // 测试没有完成，可能是伺服故障
                            warpageMeasNg = true;
                        }
                        else
                        {
                            // 计算测试结果
                            var distanceSensorOKRatio = SC.GetValue<double>("LoadLock.LoadRotation.DistanceSensorOKRatio");

                            var totalOfTrueValue = DISensroQueuen.Count(x => x); // Sensor采集的高电平数量
                            var totalValue = DISensroQueuen.Count(); // Sensor采集的总数据点数
                            var calRatio = Math.Round(totalOfTrueValue * 1.0 / totalValue * 100.0, 1);
                            warpageMeasNg = calRatio < distanceSensorOKRatio;

                            Notify($"Warpage Measurement data points: {totalOfTrueValue}/{totalValue}");

                            if (warpageMeasNg)
                            {
                                Stop($"{Module} Warpage Measurement failed [TM DI-35], the setting ratio is {calRatio:F1}%");
                            }
                            else
                            {
                                Notify($"{Module} Warpage Measurement Succeed [TM DI-35], the setting ratio is {calRatio:F1}%");
                            }
                        }
                    }

                    // CCD位置判断结果
                    var ccdMeasureNg = false;
                    if (_enableCCDCheck)
                    {
                        ccdMeasureNg = _ccdCheckResult == Result.VERIFYFAIL;

                        if(ccdMeasureNg)
                            Stop($"CCD Measurement Result NG");
                        else
                            Notify($"CCD Measurement Result OK");

                    }

                    // 如果翘曲测试或CCD位置检测任意一项未通过，则报错
                    if (ccdMeasureNg || warpageMeasNg)
                    {
                        return Result.FAIL;
                    }

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

    }
}
