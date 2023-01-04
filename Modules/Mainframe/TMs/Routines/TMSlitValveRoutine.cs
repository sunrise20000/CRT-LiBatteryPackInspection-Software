using System;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Mainframe.LLs;
using Mainframe.LLs.Routines;
using Mainframe.UnLoads;
using Mainframe.UnLoads.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UnLoad;
using SicPM.Devices;

namespace Mainframe.TMs.Routines
{
    public class TMSlitValveRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            ChamberBalance,
            OpenTMToLLVent,
            OpenTMToPMV70,
            CheckPressureCondition,

            SetSlitValve,

            SetSlowVentValve,
            SetM40,

            LLRoutine,

            Delay, 
            Delay1, Delay2, Delay3, Delay4,
            CloseTMToLLVent, 
            CloseTMToPMV70,
            PumpDownToBase,
            CheckConfinementIsDown,

            SetConfinementRingUp,
            SetConfinementRingDown,
            SetPostTransfer,
            SetPmPreTrasfer,
            SetV77,
            SetV77Close,
            SetTmMfc,

            WaitTempBelow900,
            WaitPVTempBelowSet,
            WaitPMIdle
        }


        private int _timeout;

        private SicTM _tm;
        private LoadLock _ll;
        private UnLoad _unLoad;
        private IoValve PMToTm_V70;
        private IoPressure _pmPT1;
        private IoConfinementRing _confinementRing;
        private SicPM.Devices.SicServo _sicServo;
        SicPM.Devices.IoInterLock _pmIoInterLock;
        SicPM.Devices.IoTC _tc1;

        private string _paramTarget;
        private bool _paramIsOpen;
        private bool _confinementIsDown = false;


        //private TMPumpWithTurboRoutine _pumpDownRoutine;

        private TMPressureBalanceRoutine _balanceRoutine;
        private TMPressureBalancePidRoutine _pidBalanceRoutine;
        private TMPressureBalancePidRoutine _pidAwaysRoutine;
        private LoadLockServoToRoutine _llVentTo;
        private UnLoadServoToRoutine _unLoadVentTo;
        private IoMFC _m40;

        private bool _needPressureBalance;
        private bool _userPidBalance;

        private int _rotationStopTimeout = 120;

        private double _maxPressureDiffOpenSlitValve;
        private bool _isAtmMode;
        private bool _isLockLock;
        private bool _isUnLoad;
        private bool _isPM;
        private bool _isBuffer;
        private int _tmV77DelayTime = 5;
        private double _tmMfcFlow = 10;

        private bool _pmPostTrasferNeedEnableHeat = true; 
        private bool _preTransferPSUEnable = false;
        private bool _preTransferSCREnable = false;
        public IoPSU PSU1 = null;
        public IoPSU PSU2 = null;
        public IoPSU PSU3 = null;
        public IoSCR SCR1 = null;
        public IoSCR SCR2 = null;
        public IoSCR SCR3 = null;

        private double _pmPVInnerTempLimit = 0;
        private double _pmPVMiddleTempLimit = 0;
        private double _pmPVOuterTempLimit = 0;

        public TMSlitValveRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "Slit Valve";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ ModuleName.TM.ToString()}");
            _ll = DEVICE.GetDevice<SicLoadLock>($"{ModuleName.LoadLock}.{ModuleName.LoadLock}");
            _unLoad= DEVICE.GetDevice<SicUnLoad>($"{ModuleName.UnLoad}.{ModuleName.UnLoad}");
        }

        public void Init(string module, bool isOpen, bool needEnableHeat)
        {
            _pmPostTrasferNeedEnableHeat = needEnableHeat;
            Init(module, isOpen);
        }

        public void Init(string module, bool isOpen)
        {
            _paramTarget = module;
            _paramIsOpen = isOpen;
            Name = isOpen ? "Open Slit Valve" : "Close Slit Valve";

            _balanceRoutine = new TMPressureBalanceRoutine();
            _pidBalanceRoutine = new TMPressureBalancePidRoutine();
            _pidAwaysRoutine = new TMPressureBalancePidRoutine();
            _llVentTo = new LoadLockServoToRoutine(ModuleName.LoadLock);
            _unLoadVentTo = new UnLoadServoToRoutine(ModuleName.UnLoad);
            _balanceRoutine.Init(ModuleHelper.Converter(module));
            _pidBalanceRoutine.Init(ModuleHelper.Converter(module));
            _pidAwaysRoutine.Init(ModuleHelper.Converter(module), true);

            /*_needPressureBalance = SC.GetValue<bool>("TM.NeedPressureBalance");
            _userPidBalance= SC.GetValue<bool>("TM.PressureBalanceUsePid");*/
            _needPressureBalance = true;
            _userPidBalance = false;
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _isLockLock = ModuleHelper.IsLoadLock(_paramTarget);
            _isUnLoad = _paramTarget == ModuleName.UnLoad.ToString();
            _isPM = ModuleHelper.IsPm(_paramTarget);
            _isBuffer = _paramTarget == ModuleName.Buffer.ToString();

            if (_isPM)
            {
                PMToTm_V70 = DEVICE.GetDevice<IoValve>($"{_paramTarget}.V70");
                _confinementRing = DEVICE.GetDevice<IoConfinementRing>($"{_paramTarget}.ConfinementRing");
                _sicServo = DEVICE.GetDevice<SicPM.Devices.SicServo>($"{_paramTarget}.PMServo");
                _pmIoInterLock = DEVICE.GetDevice<SicPM.Devices.IoInterLock>($"{_paramTarget}.PMInterLock");
                _tc1 = DEVICE.GetDevice<SicPM.Devices.IoTC>($"{_paramTarget}.TC1");
            }

            if (TMDevice.GetSlitValve(ModuleHelper.Converter(_paramTarget)) == null)
            {
                EV.PostWarningLog(Module, $"do not find slit valve info");
                return Result.FAIL;
            }

            if ((_paramIsOpen && TMDevice.CheckSlitValveOpen(ModuleHelper.Converter(_paramTarget)))
                || (!_paramIsOpen && TMDevice.CheckSlitValveClose(ModuleHelper.Converter(_paramTarget))))
            {
                string message = _paramIsOpen ? "open" : "close";                
                return Result.DONE;
            }

            _tm.CloseAllVentPumpValue();

            if (_paramIsOpen)
            {
                var isAtmMode = SC.GetValue<bool>("System.IsATMMode");
                if (ModuleHelper.IsLoadLock(_paramTarget))
                {
                    var ll = DEVICE.GetDevice<LoadLock>($"{_paramTarget}.{_paramTarget}");

                    if (isAtmMode)
                    {
                        if (!ll.CheckAtm())
                        {
                            EV.PostWarningLog(Module, $"can not open slit valve, running in ATM mode, but {_paramTarget} not in ATM");
                            return Result.FAIL;
                        }
                    }
                    /*else
                    {
                        if (!ll.CheckVacuum())
                        {
                            EV.PostWarningLog(Module, $"can not open slit valve, {_paramTarget} not in vacuum");
                            return Result.FAIL;
                        }
                    }*/
                }

                if (ModuleHelper.IsPm(_paramTarget))
                {
                    _pmPT1 = DEVICE.GetDevice<IoPressure>($"{_paramTarget}.PT1");
                    double atmBase = SC.GetValue<double>($"PM.AtmPressureBase");
                    double vacBase = SC.GetValue<double>($"PM.VacuumPressureBase");

                    PSU1 = DEVICE.GetDevice<IoPSU>($"{_paramTarget}.PSU1");
                    PSU2 = DEVICE.GetDevice<IoPSU>($"{_paramTarget}.PSU2");
                    PSU3 = DEVICE.GetDevice<IoPSU>($"{_paramTarget}.PSU3");
                    SCR1 = DEVICE.GetDevice<IoSCR>($"{_paramTarget}.SCR1");
                    SCR2 = DEVICE.GetDevice<IoSCR>($"{_paramTarget}.SCR2");
                    SCR3 = DEVICE.GetDevice<IoSCR>($"{_paramTarget}.SCR3");

                    if (WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(_paramTarget), 0))
                    {
                        _pmPVInnerTempLimit = SC.GetValue<double>($"PM.{_paramTarget}.Heater.PickPVInnerTempLimit");
                        _pmPVMiddleTempLimit = SC.GetValue<double>($"PM.{_paramTarget}.Heater.PickPVMiddleTempLimit");
                        _pmPVOuterTempLimit = SC.GetValue<double>($"PM.{_paramTarget}.Heater.PickPVOuterTempLimit");
                    }
                    else
                    {
                        _pmPVInnerTempLimit = SC.GetValue<double>($"PM.{_paramTarget}.Heater.PlacePVInnerTempLimit");
                        _pmPVMiddleTempLimit = SC.GetValue<double>($"PM.{_paramTarget}.Heater.PlacePVMiddleTempLimit");
                        _pmPVOuterTempLimit = SC.GetValue<double>($"PM.{_paramTarget}.Heater.PlacePVOuterTempLimit");
                    }

                    if (isAtmMode)
                    {
                        if (_pmPT1.FeedBack < atmBase)
                        {
                            EV.PostWarningLog(Module, $"can not open slit valve, running in ATM mode, but {_paramTarget} not in ATM");
                            return Result.FAIL;
                        }
                    }
                    else
                    {
                        if (_pmPT1.FeedBack > vacBase)
                        {
                            EV.PostWarningLog(Module, $"can not open slit valve, {_paramTarget} not in vacuum");
                            return Result.FAIL;
                        }
                    }
                }

                ModuleName[] slitValveModules = new ModuleName[]
                {
                        ModuleName.LoadLock,ModuleName.PM1,ModuleName.PM2,ModuleName.UnLoad
                };
                foreach (var slitValveModule in slitValveModules)
                {
                    if (slitValveModule.ToString() == _paramTarget)
                        continue;

                    if (!_tm.CheckSlitValveClose(slitValveModule))
                    {
                        EV.PostWarningLog(Module, $"can not open slit valve {_paramTarget},  {slitValveModule} slit valve not closed, can not open at same time");
                        return Result.FAIL;
                    }
                }
            }

            if (ModuleHelper.IsPm(_paramTarget))
            {
                _preTransferPSUEnable = SC.GetValue<bool>($"PM.{_paramTarget}.PreTransferPSUEnable");
                _preTransferSCREnable = SC.GetValue<bool>($"PM.{_paramTarget}.PreTransferSCREnable");
            }

            _maxPressureDiffOpenSlitValve = SC.GetValue<double>("TM.MaxPressureDiffOpenSlitValve");
            _timeout = SC.GetValue<int>("System.SlitValveMotionTimeout");
            _isAtmMode = SC.GetValue<bool>("System.IsATMMode");

         
            _tmV77DelayTime= SC.GetValue<int>($"TM.OpenSlitValveDelayTimeForPM");
            _tmMfcFlow = SC.GetValue<double>($"TM.MFCFlowWhenV77Open");

            Notify("Start");

            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                if (_isLockLock)
                {
                    if (_paramIsOpen)
                    {
                        TimeDelay((int)RoutineStep.Delay1, 1);
                        if (_userPidBalance)
                        {
                            ExecuteRoutine((int)RoutineStep.ChamberBalance, _pidBalanceRoutine);
                        }
                        else
                        {
                            ExecuteRoutine((int)RoutineStep.ChamberBalance, _balanceRoutine);
                        }

                        if (_needPressureBalance && !_isAtmMode)
                        {
                            TimeDelay((int)RoutineStep.Delay2, 3);
                            ExecuteRoutine((int)RoutineStep.LLRoutine, _llVentTo);
                            SetTmToLoadLockVent((int)RoutineStep.OpenTMToLLVent, true, _tm, _paramTarget, 5);
                        }

                        TimeDelay((int)RoutineStep.Delay3, 3);
                        CheckPressureCondition((int)RoutineStep.CheckPressureCondition, _tm, _paramTarget);
                    }
                    SetSlitValve((int)RoutineStep.SetSlitValve, TMDevice, _paramTarget, _paramIsOpen, _timeout);
                    TimeDelay((int)RoutineStep.Delay4, 1);
                    SetTmToLoadLockVent((int)RoutineStep.CloseTMToLLVent, false, _tm, _paramTarget, 5);
                }
                else if (_isUnLoad)
                {
                    if (_paramIsOpen)
                    {
                        TimeDelay((int)RoutineStep.Delay1, 1);
                        if (_userPidBalance)
                        {
                            ExecuteRoutine((int)RoutineStep.ChamberBalance, _pidBalanceRoutine);
                        }
                        else
                        {
                            ExecuteRoutine((int)RoutineStep.ChamberBalance, _balanceRoutine);
                        }

                        if (_needPressureBalance && !_isAtmMode)
                        {
                            TimeDelay((int)RoutineStep.Delay2, 1);
                            ExecuteRoutine((int)RoutineStep.LLRoutine, _unLoadVentTo);
                            SetTmToUnLoadVent((int)RoutineStep.OpenTMToLLVent, true, _tm, _paramTarget, 10);
                        }

                        TimeDelay((int)RoutineStep.Delay3, 3);
                        CheckPressureCondition((int)RoutineStep.CheckPressureCondition, _tm, _paramTarget);
                    }
                    SetSlitValve((int)RoutineStep.SetSlitValve, TMDevice, _paramTarget, _paramIsOpen, _timeout);
                    TimeDelay((int)RoutineStep.Delay4, 1);
                    SetTmToUnLoadVent((int)RoutineStep.CloseTMToLLVent, false, _tm, _paramTarget, 10);
                }
                else if (_isPM)
                {
                    if (_paramIsOpen)
                    {
                        SetPreTransfer((int)RoutineStep.SetPmPreTrasfer, _rotationStopTimeout);//旋转停止,加热停止

                        WaitPVTempratureBelowSet((int)RoutineStep.WaitPVTempBelowSet, 600);

                        if (_needPressureBalance && !_isAtmMode)
                        {
                            if (_userPidBalance)
                            {
                                ExecuteRoutine((int)RoutineStep.ChamberBalance, _pidBalanceRoutine);
                            }
                            else
                            {
                                ExecuteRoutine((int)RoutineStep.ChamberBalance, _balanceRoutine);
                            }

                            SetTmMfc((int)RoutineStep.SetTmMfc, _tmMfcFlow);
                            SetPmToTmV70((int)RoutineStep.OpenTMToPMV70, true, _tm, _paramTarget, 5);
                            SetSlowVentValve((int)RoutineStep.SetV77, _tm, _paramIsOpen, _timeout);
                            TimeDelay((int)RoutineStep.Delay1, _tmV77DelayTime); //5秒可配置
                        }
                        CheckPressureCondition((int)RoutineStep.CheckPressureCondition, _tm, _paramTarget);
                    }


                    SetSlitValve((int)RoutineStep.SetSlitValve, TMDevice, _paramTarget, _paramIsOpen, _timeout);
                    if (!_paramIsOpen)
                    {
                        //关闭闸板阀需要关闭V77
                        SetSlowVentValve((int)RoutineStep.SetV77Close, _tm, _paramIsOpen, _timeout);
                    }
                    SetPmToTmV70((int)RoutineStep.CloseTMToPMV70, false, _tm, _paramTarget, 5);

                    if (_paramIsOpen)
                    {
                        WaitPMIdle((int)RoutineStep.WaitPMIdle, _paramTarget, 20);
                        SetConfinementRingDownAndWait((int)RoutineStep.SetConfinementRingDown, 15); //隔热罩下降
                    }

                    if (!_paramIsOpen)
                    {
                        SetPostTransfer((int)RoutineStep.SetPostTransfer, _pmPostTrasferNeedEnableHeat);
                        TimeDelay((int)RoutineStep.Delay4, 2);
                    }
                }
                else if(_isBuffer)
                {
                    //CheckPressureCondition((int)RoutineStep.CheckPressureCondition, _tm, _paramTarget);
                    SetSlitValve((int)RoutineStep.SetSlitValve, TMDevice, _paramTarget, _paramIsOpen, _timeout);
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
            _tm.CloseAllVentPumpValue();
        }

        /// <summary>
        /// 打开或关闭V85
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isOpen"></param>
        /// <param name="timeDelay"></param>
        private void SetTmToLoadLockVent(int id,bool isOpen, TM tm, string module, int timeDelay)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify((isOpen ? "Open" : "Close") + " TM to LoadLock Vent");

                if (!_tm.SetTmToLLVent(isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                double tmPressure = tm.ChamberPressure;
                double targetPressure = 0.0;
                if (ModuleHelper.IsLoadLock(_paramTarget))
                {
                    targetPressure = _ll.ChamberPressure;
                }
                else
                {
                    Stop($"{module} not define pressure condition");
                    return false;
                }

                return Math.Abs(tmPressure - targetPressure) <= _maxPressureDiffOpenSlitValve;
            },
            timeDelay * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"can not complete {nameof(SetTmToLoadLockVent)}() in {timeDelay} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        private void SetTmToUnLoadVent(int id, bool isOpen, TM tm, string module, int timeDelay)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify((isOpen ? "Open" : "Close") + " TM to UnLoad Vent");

                if (!_tm.SetTmToUnLoadVent(isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                double tmPressure = tm.ChamberPressure;
                double targetPressure = _unLoad.ChamberPressure; 

                return Math.Abs(tmPressure - targetPressure) <= _maxPressureDiffOpenSlitValve;
            },
            timeDelay * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"can not complete Unload/TM pressure balance in {timeDelay} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void SetPmToTmV70(int id, bool isOpen, TM tm, string module, int timeDelay)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"set V70 " + (isOpen ? "Open" : "Close"));

                if (!PMToTm_V70.TurnValve(isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                double tmPressure = tm.ChamberPressure;
                double targetPressure = 0.0;

                 if (ModuleHelper.IsPm(_paramTarget))
                {
                    targetPressure = DEVICE.GetDevice<SicPM.Devices.IoPressure>($"{module}.PT1").FeedBack;
                }
                else
                {
                    Stop($"{module} not define pressure condition");
                    return false;
                }

                return Math.Abs(tmPressure - targetPressure) <= _maxPressureDiffOpenSlitValve;
            },
            timeDelay * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"can not complete {nameof(SetPmToTmV70)}() in {timeDelay} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void SetSlitValve(int id, TM tm, string module, bool isOpen, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"set slit valve {module} " + (isOpen ? "Open" : "Close"));

                if (!tm.SetSlitValve(ModuleHelper.Converter(module), isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return isOpen ? tm.CheckSlitValveOpen(ModuleHelper.Converter(module)) : tm.CheckSlitValveClose(ModuleHelper.Converter(module));

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"can not complete {nameof(SetSlitValve)}() in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CheckPressureCondition(int id, TM tm, string target)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
                {
                    Notify($"check pressure condition to open {target} slit valve");

                    double tmPressure = tm.ChamberPressure;
                    double targetPressure = 0.0;
                    if (ModuleHelper.IsLoadLock(_paramTarget))
                    {
                        targetPressure = _ll.ChamberPressure;
                    }
                    else if(_paramTarget == ModuleName.UnLoad.ToString())
                    {
                        targetPressure = _unLoad.ChamberPressure;
                    }
                    else if (ModuleHelper.IsPm(_paramTarget))
                    {
                        targetPressure = DEVICE.GetDevice<SicPM.Devices.IoPressure>($"{target}.PT1").FeedBack;
                    }
                    else if (_paramTarget == ModuleName.Buffer.ToString())
                    {
                        targetPressure = tm.ChamberPressure;
                    }
                    else
                    {
                        Stop($"{target} not define pressure condition");
                        return false;
                    }

                    double pressureDiff = Math.Abs(tmPressure - targetPressure);
                    if (pressureDiff > _maxPressureDiffOpenSlitValve)
                    {
                        Stop($"pressure difference {pressureDiff:F3}  between TM and {target} exceed tolerance {_maxPressureDiffOpenSlitValve}");
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

        public void SetSlowVentValve(int id, SicTM tm, bool isOpen, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                string op = isOpen ? "Open" : "Close";

                Notify($"{op} Slow Vent Valve");

                if (!tm.SetSlowVentValve(isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.CheckSlowVentValve(isOpen);

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set Slow Vent Valve timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void SetTmMfc(int id, double flow)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set TM MFC flow to {flow} ");

                _tm.SetVentMfc(flow, out string reason);

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

        public void SetConfinementRingUp(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Confinement RingUp");

                _confinementRing.MoveUpPos(out string reason);

                return true;
            }, () =>
            {
                 return _confinementRing.RingDone && _confinementRing.RingUpSensor;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set Confinement RingUp timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void WaitPMIdle(int id, string _targetModule, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                return true;
            }, () =>
            {
                string pm1Status = DATA.Poll($"{_targetModule}.Status") == null ? "" : DATA.Poll($"{_targetModule}.Status").ToString();
                return pm1Status == "ProcessIdle" || pm1Status == "Idle";

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Wait PM Idle timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        protected void SetConfinementRingDownAndWait(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set ConfinementRing Down");

                if (!_confinementRing.MoveDownPos(out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _confinementRing.RingDone && _confinementRing.RingDownSensor;
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
                    Stop($"Set ConfinementRing Down, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void SetPostTransfer(int id, bool postTransferEnableHeat)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set PostTransfer");

                OP.DoOperation($"{_paramTarget}.PostTransfer", new object[] { postTransferEnableHeat });

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

        public void SetPreTransfer(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set PSU and SCR Disable");

                if (_sicServo != null)
                {
                    _sicServo.SetActualSpeed(0, 0);
                }
                if (!_preTransferPSUEnable)
                {
                    PSU1.SetPSUEnable(false, out _);
                    PSU2.SetPSUEnable(false, out _);
                    PSU3.SetPSUEnable(false, out _);
                }
                if (!_preTransferSCREnable)
                {
                    SCR1.SetEnable(false, out _);
                    SCR2.SetEnable(false, out _);
                    SCR3.SetEnable(false, out _);
                }
                return true;
            }, () =>
            {
                if (_sicServo.ActualSpeedFeedback > 0)
                {
                    return false;
                }
                if (!_preTransferPSUEnable)
                {
                    if (PSU1 != null && PSU1.StatusFeedBack)
                    {
                        return false;
                    }
                    if (PSU2 != null && PSU2.StatusFeedBack)
                    {
                        return false;
                    }
                    if (PSU3 != null && PSU3.StatusFeedBack)
                    {
                        return false;
                    }
                }
                if (!_preTransferSCREnable)
                {
                    if (SCR1 != null && SCR1.StatusFeedBack)
                    {
                        return false;
                    }
                    if (SCR2 != null && SCR2.StatusFeedBack)
                    {
                        return false;
                    }
                    if (SCR3 != null && SCR3.StatusFeedBack)
                    {
                        return false;
                    }
                }
                return true;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set PreTransfer timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        private void WaitTempratureBelow900(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Wait DiHeaterTempBelow900CSW");
                return true;

            }, () =>
            {
                if (_pmIoInterLock != null)
                {
                    return _pmIoInterLock.DiHeaterTempBelow900CSW;
                }
                else
                {
                    return true;
                }
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
                    Stop($"Wait PM Temprature below 900 timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        private void WaitPVTempratureBelowSet(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, ()=>
            {
                Notify($"Wait PV1Temp below{_pmPVInnerTempLimit}℃ and PV2Temp below{_pmPVMiddleTempLimit}℃ and PV3Temp below{_pmPVOuterTempLimit}℃");
                return true;

            }, () =>
            {
                if (_tc1 != null && _pmPVOuterTempLimit > 0 && _pmPVMiddleTempLimit > 0 && _pmPVOuterTempLimit > 0)
                {
                    return _tc1.L1PVFeedBack < _pmPVInnerTempLimit && _tc1.L2PVFeedBack < _pmPVMiddleTempLimit && _tc1.L3PVFeedBack < _pmPVOuterTempLimit;
                }
                else
                {
                    return true;
                }
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
                    Stop($"Wait PM Temprature below 900 timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }


}
