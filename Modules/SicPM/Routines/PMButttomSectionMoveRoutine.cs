using System;
using System.Diagnostics;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using SicPM;
using SicPM.Devices;
using SicPM.Routines;

namespace SicPM.Routines
{
    public class PMButttomSectionMoveRoutine : PMBaseRoutine
    {

        //public IoGasConnector _ioGasConnector;
        //public IoLid _ioButtomLid;
        //public IoLidSwing _ioButtomSwing;
        //public IoBottomSection _ioButtomSection;

        int _timeoutLid = 10;
        int _timeoutSwing = 10;
        int _timeoutGasConnector = 10;
        int _timeoutButtomSection = 10;

        private bool _Open = true;


        private Stopwatch _swTimer = new Stopwatch();
        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }

        enum RoutineStep
        {
            RoutineStep0,
            RoutineStep1,
            RoutineStep2,
            RoutineStep3,
            RoutineStep4,
            RoutineStep5,
            RoutineStep6,
            RoutineStep7,
            RoutineStep8,
            RoutineStep9,
            RoutineStep10,
            RoutineStepTime1,
            RoutineStepTime2,
            RoutineStepTime3,
            RoutineStepTime4,
            RoutineStepTime5,
        }

        public PMButttomSectionMoveRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "MoveBottom";

            //_ioGasConnector = DEVICE.GetDevice<IoGasConnector>($"{pm.Name}.{"GasConnector"}");
            //_ioButtomLid = DEVICE.GetDevice<IoLid>($"{pm.Name}.{"BottomLid"}");
            //_ioButtomSwing = DEVICE.GetDevice<IoLidSwing>($"{pm.Name}.{"BottomLidSwing"}");
            //_ioButtomSection = DEVICE.GetDevice<IoBottomSection>($"{pm.Name}.{"BottomSection"}");

            //_timeoutLid = SC.GetValue<int>("PM.Motion.LidTimeout");
            //_timeoutSwing = SC.GetValue<int>("PM.Motion.LidSwingTimeout");
            //_timeoutGasConnector = SC.GetValue<int>("PM.Motion.GasConnectorTimeout");
            //_timeoutButtomSection = SC.GetValue<int>("PM.Motion.ButtomSectionUpTimeout"); 


        }

        internal void Init(params object[] objs)
        {
            _Open = Boolean.Parse(objs[0].ToString());
        }

        public override Result Start(params object[] objs)
        {
            _swTimer.Restart();
            Reset();
            Notify("Start");
            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                return Result.DONE;
                //GasConnectorLoosen((int)RoutineStep.RoutineStep0, true, _timeoutGasConnector);

                //if (_Open)
                //{
                //    CheckLidTighten((int)RoutineStep.RoutineStep2, _ioButtomLid, "Bottom", false, _timeoutLid);
                //    CheckLidSwingLock((int)RoutineStep.RoutineStep1, _ioButtomSwing, "BottomSwing", false, _timeoutSwing);
                //    TimeDelay((int)RoutineStep.RoutineStep3, 1);
                //    CheckButtomSectionUpDown((int)RoutineStep.RoutineStep4, _ioButtomSection, true, _timeoutButtomSection);
                //}
                //else
                //{
                //    TimeDelay((int)RoutineStep.RoutineStep3, 1);
                //    CheckButtomSectionUpDown((int)RoutineStep.RoutineStep4, _ioButtomSection, false, _timeoutButtomSection);
                //    CheckLidSwingLock((int)RoutineStep.RoutineStep1, _ioButtomSwing, "BottomSwing", true, _timeoutSwing);
                //    CheckLidTighten((int)RoutineStep.RoutineStep2, _ioButtomLid, "Bottom", true, _timeoutLid);
                //}
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException ex)
            {
                if (ex.Message == "Timeout")
                {
                    Notify("Timeout");
                }
                return Result.FAIL;
            }
            _swTimer.Stop();
            Notify("Finished");

            return Result.DONE;
        }


        //protected void GasConnectorLoosen(int id, bool isLock, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run GasConnector Tighten");

        //        if (!_ioGasConnector.SetGasConnector(true, out string reason))
        //        {
        //            return false;
        //        }

        //        return true;
        //    }, () =>
        //    {
        //        return isLock ? _ioGasConnector.GasConnectorTightenFeedback == true : _ioGasConnector.GasConnectorLoosenFeedback == true;

        //    }, timeout * 1000);


        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT)
        //        {
        //            throw (new RoutineFaildException("Timeout"));
        //        }
        //        else
        //        {
        //            throw (new RoutineBreakException());
        //        }
        //    }
        //}

        protected void CheckLidTighten(int id, IoLid cLid, string strLidName, bool isTighten, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify(isTighten ? $"Run {strLidName} Tighten" : $"Run {strLidName} Loosen");

                if (!cLid.SetLid(!isTighten, out string reason))
                {
                    return false;
                }

                return true;
            }, () =>
            {
                return isTighten ? cLid.TightenFaceback == true : cLid.LoosenFaceback == true;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    throw (new RoutineFaildException("Timeout"));
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void CheckLidSwingLock(int id, IoLidSwing cLidSwing, string strLidName, bool isLock, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify(isLock ? $"Run {strLidName} SwingLock" : $"Run {strLidName} SwingUnLock");

                if (!cLidSwing.SetLidSwing(isLock, out string reason))
                {
                    return false;
                }

                return true;
            }, () =>
            {
                return isLock ? cLidSwing.LidLockFaceback == true : cLidSwing.LidUnlockFaceback == true;

            }, timeout * 1000);


            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    throw (new RoutineFaildException("Timeout"));
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        //protected void CheckButtomSectionUpDown(int id, IoBottomSection cSection, bool toDown, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        if (toDown)
        //        {
        //            Notify( $"Run BottomSection Down");

        //            if (!cSection.MoveDown(out string reason))
        //            {
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            Notify($"Run BottomSection Up");

        //            if (!cSection.MoveUp(out string reason))
        //            {
        //                return false;
        //            }
        //        }

        //        return true;
        //    }, () =>
        //    {
        //        return toDown ? cSection.DownFaceback == true : cSection.UpFaceback == true;

        //    }, timeout * 1000);


        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT)
        //        {
        //            throw (new RoutineFaildException("Timeout"));
        //        }
        //        else
        //        {
        //            throw (new RoutineBreakException());
        //        }
        //    }
        //}


        public override void Abort()
        {
            base.Abort();
        }
    }
}
