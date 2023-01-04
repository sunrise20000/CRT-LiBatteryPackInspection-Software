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
    public class PMChamberMoveBodyRoutine : PMBaseRoutine
    {
        public IoChamberMoveBody _ioChamberMoveBody;
        public IoGasConnector _ioGasConnector;
        public IoLid _ioSHLid;
        public IoLidSwing _ioSHSwing;
        public IoLid _ioMiddleLid;
        public IoLidSwing _ioMiddleSwing;
        private PMModule _pmModule;

        private string _moveBodyGroup;
        private bool _Open = true;
        int _timeoutLid = 10;
        int _timeoutSwing = 10;
        int _timeoutGasConnector = 10;

        private bool IsShOpen = false;   
        private bool IsTopOpen = false;
        private bool IsMiddleOpen = false; 

        private Stopwatch _swTimer = new Stopwatch();
        public int ElapsedTime
        {
            get { return _swTimer.IsRunning ? (int)(_swTimer.ElapsedMilliseconds / 1000) : 0; }
        }

        enum RoutineStep
        {
            StepSetTv76,
            StepSetTv75,

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
            RoutineStep11,
            RoutineStep12,
            RoutineStep13,
            RoutineStep14,
            RoutineStepTime1,
            RoutineStepTime2,
            RoutineStepTime3,
            RoutineStepTime4,
            RoutineStepTime5,

            CloseTv76,
            CloseTv75,
            WaitSensorOpen,

            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
            TimeDelay0
        }

        public PMChamberMoveBodyRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            Module = module.ToString();
            Name = "MoveBody";
            _pmModule = pm;

            _ioChamberMoveBody = DEVICE.GetDevice<IoChamberMoveBody>($"{pm.Name}.{"ChamberMoveBody"}");
            _ioGasConnector = DEVICE.GetDevice<IoGasConnector>($"{pm.Name}.{"GasConnector"}");

            _ioSHLid = DEVICE.GetDevice<IoLid>($"{pm.Name}.{"SHLid"}");
            _ioSHSwing = DEVICE.GetDevice<IoLidSwing>($"{pm.Name}.{"SHLidSwing"}");
            _ioMiddleLid = DEVICE.GetDevice<IoLid>($"{pm.Name}.{"MiddleLid"}");
            _ioMiddleSwing = DEVICE.GetDevice<IoLidSwing>($"{pm.Name}.{"MiddleLidSwing"}");

            _timeoutLid = 10;
            _timeoutSwing = 10;
            _timeoutGasConnector = 10;

            
        }

        private object ParseScNode(string v1, object node, string v2, string v3)
        {
            throw new NotImplementedException();
        }

        internal void Init(params object[] objs)
        {
            _moveBodyGroup = objs[0].ToString().ToUpper();

            _Open = Boolean.Parse(objs[1].ToString());
        }

        public override Result Start(params object[] objs)
        {
            if (PMDevice.SensorPMATMSW.Value)
            {
                EV.PostWarningLog(Module, "PM is not at ATM State(DI-9 PMATMSW),do not open Cham!");
                return Result.FAIL;
            }
            if (!_ioChamberMoveBody.DownFaceback)
            {
                EV.PostWarningLog(Module, "Chamber must in Down position!");
                return Result.FAIL;
            }
            if (!_ioChamberMoveBody.FrontFaceback)
            {
                EV.PostWarningLog(Module, "Chamber must in Front position!");
                return Result.FAIL;
            }
            if (_pmModule._pmInterLock != null && !_pmModule._pmInterLock.DoLidOpenRoutineSucceed && _Open) // 不限制close do-172
            {
                EV.PostWarningLog(Module, "DO-172 DO_PurgeRoutineSucceed must be on!");
                return Result.FAIL;
            }
            if (PMDevice.PSU2.AllHeatEnable)
            {
                EV.PostWarningLog(Module, "HeatEnable must be off!");
                return Result.FAIL;
            }

            if (_ioSHLid.LoosenFaceback)
            {
                IsShOpen = true;
                IsTopOpen = false;
                IsMiddleOpen = false;
            }
            else if (_ioMiddleLid.LoosenFaceback)
            {
                IsShOpen = false;
                IsTopOpen = false;
                IsMiddleOpen = true;
            }

            _swTimer.Restart();
            Reset();
            Notify("Start");

            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                //开启V76
                SetIoValueByName((int)RoutineStep.CloseTv75, "V75", false, 2);
                SetIoValueByName((int)RoutineStep.StepSetTv76, "V76", true, 2);

                TimeDelay((int)RoutineStep.TimeDelay0, 2);
                ////等待DOR.PressATM.SW 为1
                //WaitSensorOpenClose((int)RoutineStep.WaitSensorOpen, _pmModule.SensorDORPressATMSW, true);

                GasConnectorTighten((int)RoutineStep.RoutineStep0, false, _timeoutGasConnector);
                TimeDelay((int)RoutineStep.TimeDelay1, 2);

                if (_Open)
                {
                    if (_moveBodyGroup.Contains("MID"))
                    {
                        CheckLidTighten((int)RoutineStep.RoutineStep9, _ioMiddleLid, "MiddleLid", false, _timeoutLid);
                        TimeDelay((int)RoutineStep.TimeDelay2, 2);
                        CheckLidSwingLock((int)RoutineStep.RoutineStep10, _ioMiddleSwing, "MiddleLid", false, _timeoutSwing);
                    }
                    else
                    {
                        CheckLidTighten((int)RoutineStep.RoutineStep9, _ioSHLid, "SHLid", false, _timeoutLid);
                        TimeDelay((int)RoutineStep.TimeDelay2, 2);
                        CheckLidSwingLock((int)RoutineStep.RoutineStep10, _ioSHSwing, "SHLid", false, _timeoutSwing);
                    }

                    TimeDelay((int)RoutineStep.RoutineStepTime2, 2);

                }
                else
                {
                    if (IsShOpen)
                    {
                        CheckLidSwingLock((int)RoutineStep.RoutineStep4, _ioSHSwing, "SHLid", true, _timeoutSwing);
                        TimeDelay((int)RoutineStep.TimeDelay2, 2);
                        CheckLidTighten((int)RoutineStep.RoutineStep1, _ioSHLid, "SHLid", true, _timeoutLid);
                    }
                    else if (IsMiddleOpen)
                    {
                        CheckLidSwingLock((int)RoutineStep.RoutineStep6, _ioMiddleSwing, "MiddleLid", true, _timeoutSwing);
                        TimeDelay((int)RoutineStep.TimeDelay2, 2);
                        CheckLidTighten((int)RoutineStep.RoutineStep3, _ioMiddleLid, "MiddleLid", true, _timeoutLid);
                    }


                    GasConnectorTighten((int)RoutineStep.RoutineStep9, true, _timeoutGasConnector);

                    ////关闭V76
                    //SetIoValueByName((int)RoutineStep.StepSetTv75, "V75", true, 2);
                    //SetIoValueByName((int)RoutineStep.CloseTv76, "V76", false, 2);
                }
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


        protected void GasConnectorTighten(int id, bool isLock, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                if(isLock)
                {
                    Notify($"Run GasConnector Tighten");
                }
                else
                {
                    Notify($"Run GasConnector Loosen");
                }
                

                if (!_ioGasConnector.SetGasConnector(isLock, out string reason))
                {
                    Notify(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return  _ioGasConnector.GasConnectorTightenFeedback == isLock && _ioGasConnector.GasConnectorLoosenFeedback == !isLock;

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

        //protected void CheckChamberMoveFowardEnd(int id, bool moveend, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify(moveend ? $"Run ChamberMoveBody Move End" : $"Run ChamberMoveBody Move Foward");

        //        if (!_ioChamberMoveBody.MoveForwardEnd(moveend, out string reason))
        //        {
        //            return false;
        //        }

        //        return true;
        //    }, () =>
        //    {
        //        return moveend ? _ioChamberMoveBody.EndFaceback == true : _ioChamberMoveBody.FrontFaceback == true;

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

        //protected void CheckChamberMoveBodyUp(int id, bool moveup, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify(moveup ? $"Run ChamberMoveBody Move UP" : $"Run ChamberMoveBody Move Down");

        //        if (!_ioChamberMoveBody.MoveUpDown(moveup, out string reason))
        //        {
        //            return false;
        //        }

        //        return true;
        //    }, () =>
        //    {
        //        return moveup ? _ioChamberMoveBody.UpFaceback == true : _ioChamberMoveBody.DownFaceback == true;

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

        //protected void CheckChamberMoveLatch(int id, bool isLatch, int timeout)
        //{
        //    Tuple<bool, Result> ret = Execute(id, () =>
        //    {
        //        Notify(isLatch ? $"Run ChamberMoveBody UpLatch(true)" : $"Run ChamberMoveBody UpLatch(false)");

        //        if (!_ioChamberMoveBody.SetUpBrake(isLatch, out string reason))
        //        {
        //            return false;
        //        }

        //        return true;
        //    });

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

        protected void CheckLidTighten(int id, IoLid cLid,string strLidName, bool isTighten, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify(isTighten ? $"Run {strLidName} Tighten" : $"Run {strLidName} Loosen");

                if (!cLid.SetLid(!isTighten, out string reason))
                {
                    Notify(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return  cLid.TightenFaceback == isTighten && cLid.LoosenFaceback == !isTighten;

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
                    Notify(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return cLidSwing.LidLockFaceback == isLock && cLidSwing.LidUnlockFaceback == !isLock;

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

        /// <summary>
        /// 等待Sensor状态变化
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ioSensor"></param>
        /// <param name="open"></param>
        protected void WaitSensorOpenClose(int id,IoSensor ioSensor,bool open)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return ioSensor.Value == open;
            }, 10 * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Wait {ioSensor.Name} timeout, over 10 seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void SetIoValueByName(int id, string ioName, bool close, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify((close ? "Open" : "Close") + $"{ioName} value");
                _pmModule.SetIoValue(new System.Collections.Generic.List<string> { ioName }, close);
                return true;
            }, () =>
            {
                return _pmModule.CheckIoValue(new System.Collections.Generic.List<string> { ioName }, close);
            },
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop((close ? "Open" : "Close") + $"{ioName} value timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

    }
}
