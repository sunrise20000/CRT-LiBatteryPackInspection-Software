using System;
using System.Collections.Generic;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Routine;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;

namespace SicPM.Routines
{
    public class PMBaseRoutine : ModuleRoutine, IRoutine
    {

        protected PressureUpOrDown currentPressureUpOrDown = PressureUpOrDown.None;
        protected PressureLevel _lastPressureLevel = PressureLevel.Leavel0;
        protected bool _checkPMPressureOver1000;
        protected bool _checkPMPt2Over1000;
        protected bool _checkChamberLowThan20 = false;
        protected bool _checkChamberHighThan20 = false;
        protected bool _v72IsOpen = false;

        protected bool _finalOpen = false;
        protected bool _isTvOpen = false;
        protected bool _isEPV2Open = false;

        protected bool _setMfc291519to38Flag = false;   //在LidOpen和LidClose过程中是否已经设置过了MFC的值

        protected enum PressureLevel
        {
            Leavel0 = 50,
            Leavel1 = 150,
            Leavel2 = 250,
            Leavel3 = 350,
            Leavel4 = 450,
            Leavel5 = 550,
            Leavel6 = 650,
            Leavel7 = 750,
            Leavel8 = 850,
            Leavel9 = 950,
            Leavel10= 1150,
        }

        protected PressureLevel GetPressure(double pressure)
        {
            if (pressure < 100)
            {
                return PressureLevel.Leavel0;
            }
            else if (pressure < 200)
            {
                return PressureLevel.Leavel1;
            }
            else if (pressure < 300)
            {
                return PressureLevel.Leavel2;
            }
            else if (pressure < 400)
            {
                return PressureLevel.Leavel3;
            }
            else if (pressure < 500)
            {
                return PressureLevel.Leavel4;
            }
            else if (pressure < 600)
            {
                return PressureLevel.Leavel5;
            }
            else if (pressure < 700)
            {
                return PressureLevel.Leavel6;
            }
            else if (pressure < 800)
            {
                return PressureLevel.Leavel7;
            }
            else if (pressure < 900)
            {
                return PressureLevel.Leavel8;
            }
            else
            {
                return PressureLevel.Leavel9;
            }
        }

        protected enum PressureUpOrDown
        {
            Uping,
            Dowing,
            None,
        }

        protected PMModule PMDevice
        {
            get { return _pm; }
        }
        private PMModule _pm;
 
        public PMBaseRoutine(ModuleName module, PMModule pm)
        {
            Module = module.ToString();
            _pm = pm;
        }

       
        public virtual Result Start(params object[] objs)
        {
            return Result.DONE;
        }

        public virtual Result Monitor()
        {
            return Result.DONE;
        }

        public virtual void Abort()
        {
            _pm.SetMfcToDefaultByGroup(MfcGroupName.M27toM40);
        }

        protected void PreparePump(int id, PMModuleBase pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"{pm.Name} prepare pump");

                if (!pm.PreparePump(out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return pm.CheckPreparePump();
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"{pm.Name} prepare pump timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void TurnPumpOn(int id, PMModuleBase pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Turn on {pm.Name} pump");

                if (!pm.TurnOnPump(out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return pm.CheckPumpIsOn();
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"{pm.Name} turn on pump timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void PumpToBase(int id, PMModuleBase pm, double basePressure, int tvPosition, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} pump to base pressure {basePressure:F2} mbar");

                if (!pm.FastPump(tvPosition, out string reason))
                {
                    Stop(reason);
                    return false;
                }
 
                return true;
            }, () =>
            {
                if (pm.IsError)
                {
                    Stop($"{pm.Name} error");
                    return null;
                }

                return pm.ChamberPressure < basePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} pump to base pressure {basePressure:F2} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

 
        protected void PumpToHighVacuum(int id, string stepName, PM pm, double pumpHighVacuumPressure, int tvPosition, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} fast pump to {pumpHighVacuumPressure:F2} mbar");

                //if (!pm.FastPump(tvPosition, out string reason))
                //{
                //    Stop(reason);
                //    return false;
                //}

                _stepSpan = new TimeSpan(0, 0, 0, (int)timeout);
                _stepStartTime = DateTime.Now;
                _stepName = stepName;

                return true;
            }, () =>
            {
                if (pm.IsError)
                {
                    Stop($"{pm.Name} error");
                    return null;
                }

                return pm.ChamberPressure < pumpHighVacuumPressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} pump to gas line pump pressure timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SlowPump(int id, PMModuleBase pm, double slowPumpBasePressure, int tvPosition, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} slow pump to {slowPumpBasePressure:F2} mbar");

                if (!pm.SlowPump(tvPosition, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                if (pm.IsError)
                {
                    Stop($"{pm.Name} error");
                    return null;
                }

                return pm.ChamberPressure < slowPumpBasePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} slow pump timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void CheckForelinePressure(int id, string stepName, PM pm, double forelineBasePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run {pm.Name} check foreline pressure lower than {forelineBasePressure:F2} mbar");

                _stepSpan = new TimeSpan(0, 0, 0, (int)timeout);
                _stepStartTime = DateTime.Now;
                _stepName = stepName;

                return true;
            }, () =>
            {
                if (pm.IsError)
                {
                    Stop($"{pm.Name} error");
                    return null;
                }

                return pm.ForelinePressure <= forelineBasePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} wait foreline lower than {forelineBasePressure:F2} mbar timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        protected void CheckPumpOn(int id, PMModuleBase pm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Run {pm.Name} check pump is on");

                if (!pm.CheckPumpIsOn())
                {
                    Stop("Pump not on");
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
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void CheckChamberOk(int id, PMModuleBase pm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Run {pm.Name} check chamber running no error");

                if (pm.IsError)
                {
                    Stop($"{pm.Name} error");
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
                {
                    throw (new RoutineBreakException());
                }
            }
        }


        protected void EnableRotation(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} rotation enable");
                if (!_pm.SetRotationEnable(true, out string reason))
                {
                    Stop($"Set {Module} rotation enable failed, {reason}");
                    return false;
                }

                return true;
            }, () =>
            {
                return _pm.CheckRotationEnable();
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
                    Stop($"Set {Module} rotation enable timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        //protected void EnableHeater(int id,int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Set {_pm.Name} Heater Enable");

        //        if (!_pm.EnableHeater(true, out string reason))
        //        {
        //            Stop($"Set {_pm.Name} Heater Enable failed, {reason}");
        //            return false;
        //        }

        //        return true;
        //    }, () =>
        //    {
        //        return _pm.CheckHeaterEnable();
        //    }, timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        if (ret.Item2 == Result.TIMEOUT)
        //        {
        //            Stop($"Set {_pm.Name} Heater Enable timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //        {
        //            throw (new RoutineBreakException());
        //        }
        //    }
        //}

        /// <summary>
        /// 设置电机转速
        /// </summary>
        /// <param name="checkSpeed">检查是否低于此速度</param>
        protected void SetRotationValve(int id, float checkSpeed,bool bigThan, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} rotation to {checkSpeed}");
                if (!_pm.SetRotationServo(checkSpeed, 0))
                {
                    Stop($"Set {Module} rotation error");
                    return false; 
                }

                return true;
            }, () =>
            {
                return _pm.CheckRotationServoOn(checkSpeed, bigThan);
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
                    Stop($"{Module} rotation move timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetRotationValveAndNoWait(int id, float checkSpeed)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                //Notify($"Set {Module} rotation");
                if (!_pm.SetRotationServo(checkSpeed, 0))
                {
                    //Stop($"Set {Module} rotation error");
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


        protected void SetHeatEnable(int id, bool enable, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                string state = enable ? "true" : "false";
                Notify($"Set {Module} heat {state}");

                if (!_pm.SetHeatEnable(enable))
                {
                    Stop($"Set {Module} heat error");
                    return false; 
                }

                return true;
            }, () =>
            {
                return _pm.CheckHeatEnable(enable);
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
                    Stop($"{Module} Set heat timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetPSUEnable(int id, bool enable, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} heat TC1");
                if (!_pm.SetHeatEnableTC(enable,true))
                {
                    Stop($"Set {Module} heat TC1 error");
                    return false;
                }

                return true;
            }, () =>
            {
                return _pm.CheckHeatEnableTC(enable,true);
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
                    Stop($"{Module} Set heat TC1 timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetSCRReset(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set {Module} SCR Reset");

                if (!_pm.SetSCRReset())
                {
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

        protected void SetSCREnable(int id, bool enable, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set {Module} heat TC2");
                if (!_pm.SetHeatEnableTC(enable,false))
                {
                    Stop($"Set {Module} heat TC2 error");
                    return false;
                }

                return true;
            }, () =>
            {
                return _pm.CheckHeatEnableTC(enable,false);
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
                    Stop($"{Module} Set heat TC2 timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetPSUHeatMode(int id, float mode)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set PSU HeatMode");
                if (!_pm.TC1.SetHeaterMode(mode,0))
                {
                    Stop($"Set PSU HeatMode error");
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

        protected void SetPSUHeatRatio(int id,float lp1,float lp2,float lp3)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set PSU Ratio");
                if (!_pm.TC1.SetRatio(lp1, lp2, lp3))
                {
                    Stop($"Set PSU Ratio error");
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

        //protected void SetTC1PowerRef(int id, float powerRef)
        //{
        //    Tuple<bool, Result> ret = Execute(id, () =>
        //    {
        //        Notify($"Set PSU PowerRef1");
        //        if (!_pm.TC1.SetPowerRef1(powerRef))
        //        {
        //            Stop($"Set PSU SetPowerRef1 error");
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
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        protected void SetSCRHeatMode(int id, float mode)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set SCR HeatMode");
                if (!_pm.TC2.SetHeaterMode2(mode,0))
                {
                    Stop($"Set SCR HeatMode error");
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

        protected void SetSCRHeatRatio(int id, float lp1, float lp2, float lp3)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set SCR Ratio");
                if (!_pm.TC2.SetRatio(lp1, lp2, lp3))
                {
                    Stop($"Set SCR Ratio error");
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

        //protected void SetTC2PowerRef(int id, float powerRef)
        //{
        //    Tuple<bool, Result> ret = Execute(id, () =>
        //    {
        //        Notify($"Set SCR PowerRef1");
        //        if (!_pm.TC2.SetPowerRef(powerRef))
        //        {
        //            Stop($"Set SCR SetPowerRef1 error");
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
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        protected void CheckFinalIoStatue(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Check Final valve is open or closed!");
                _finalOpen = _pm.CheckIOValueByGroup(IoGroupName.Final1, true) && _pm.CheckIOValueByGroup(IoGroupName.Final2, true);
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void CheckTvOpen(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _isTvOpen = _pm._ioThrottleValve.TVValveEnable && _pm._ioThrottleValve.PressureMode == PressureCtrlMode.TVPressureCtrl;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }


        protected void CheckEPV2Open(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _isEPV2Open = _pm.EPV2.Status;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void CheckChamberV72Open(int id,double pressure)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _v72IsOpen = _pm.CheckIOValueByGroup(IoGroupName.VentPump, true) && _pm.CheckIOValueByGroup(IoGroupName.V888990, true) && _pm.PT1.FeedBack >= pressure;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void WaitV27Open(int id, int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return _pm.CheckIOValueByGroup(IoGroupName.V27,true);
            },
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Wait V27 Open Timeout;");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        protected void WaitChamberPressUpTo(int id, double press, int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return _pm.GetChamberPressure() >= press;
            },
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Wait Chamber Pressure to {press} Timeout,over {timeout} seconds;");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void WaitChamberPressDownTo(int id, double press, double _pmPressureMaxDiff, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Wait pm pressure to {press} mbar");
                return true;
            },
            () =>
            {
                return Math.Abs(_pm.GetChamberPressure() - press) <= _pmPressureMaxDiff;
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
                    Stop($"Wait Chamber Pressure Down to {press} Timeout,over {timeout} seconds;");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetIoValueByGroup(int id, IoGroupName groupName, bool close, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify((close ? "Open" : "Close") + $" {groupName.ToString()} value");
                return _pm.SetIOValueByGroup(groupName, close);
            }, () =>
            {
                return _pm.CheckIOValueByGroup(groupName, close);
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
                    Stop((close ? "Open" : "Close") + $" {groupName.ToString()} value timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void OpenH2Valve(int id, PMModule pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {pm.Name} V31 valve");

                if (!pm.V31.TurnValve(true, out string reason))
                {
                    Stop($"Open {pm.Name} V31 valve failed, {reason}");
                    return false;
                }

                return true;
            }, () =>
            {
                return pm.V31.Status;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Open {pm.Name} V31 valve timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void SetMfcModeToNormalByGroup(int id, MfcGroupName groupName)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set {groupName.ToString()} MFC Mode to normal");
                _pm.SetMfcModelToNormal(groupName);
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
        

        protected void SetMfcToDefaultByGroup(int id, MfcGroupName groupName,int time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {                
                Notify($"Set {GetMfcTipsByGroup(groupName)} MFC value to default");

                if (!_pm.SetMfcToDefaultByGroupRamp(groupName, time))
                {
                    Stop($"Set {GetMfcTipsByGroup(groupName)} MFC value to default failed");
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

        protected void SetMfc28to40Special(int id, double mfc28Flow,double mfc29Flow,double mfc31Flow, double mfc40Flow,int time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set MFC28,29,31,40 to {mfc28Flow},{mfc29Flow},{mfc31Flow},{mfc40Flow}");

                if (!_pm.SetMFC28to40Special(mfc28Flow, mfc29Flow, mfc31Flow, mfc40Flow, time))
                {
                    Stop($"Set MFC28,29,31,40 to {mfc28Flow},{mfc29Flow},{mfc31Flow},{mfc40Flow} failed");
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

        protected void SetMfcByGroup(int id, MfcGroupName groupName, double dValue,int time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set {GetMfcTipsByGroup(groupName)} MFC value to {dValue}");

                if (!_pm.SetMfcByGroup(groupName, dValue, time))
                {
                    Stop($"Set {GetMfcTipsByGroup(groupName)} MFC value to {dValue} failed");
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

        protected void SetMfcForPurgeConfigByGroup(int id, MfcGroupName groupName, string dValue, int time)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set {GetMfcTipsByGroup(groupName)} MFC value to {dValue}");

                if (!_pm.SetMfcForPurgeConfigByGroup(groupName, dValue, time))
                {
                    Stop($"Set {GetMfcTipsByGroup(groupName)} MFC value by config {dValue} failed");
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


        protected void SetMfcRampByGroupAndPressureAlways(PressureUpOrDown presOpera)
        {
            try
            {
                if (presOpera == PressureUpOrDown.None)
                {
                    return;
                }
                else
                {
                    PressureLevel level = GetPressure(_pm.GetChamberPressure());
                    if ((int)level != (int)_lastPressureLevel)
                    {
                        int cIntLv = (int)level;
                        if (cIntLv < 100)
                        {
                            Notify($"Set MFC28,29,31,40 to pressureLevel [20-100]");
                        }
                        else if (cIntLv >= 950)
                        {
                            Notify($"Set MFC28,29,31,40 to pressureLevel [950]");                            
                        }
                        else
                        {
                            Notify($"Set MFC28,29,31,40 to pressureLevel [{cIntLv - 50},{cIntLv + 50})");
                        }
                        _lastPressureLevel = level;
                        _pm.SetMfcRampByGroupAndPressure(MfcGroupName.M28293140, Convert.ToDouble((int)level));
                    }
                }
            }
            catch
            {

            }
        }

      
        protected void SetPcModeToNormal(int id, List<int> _lstPcList)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                string str = null;
                foreach (var item in _lstPcList)
                {
                    str += item.ToString() + ",";
                }
                str=str.Remove(str.Length-1);
                Notify($"Set PC{str} mode to normal");

                if (!_pm.SetPcModelToNormal(_lstPcList))
                {
                    Stop($"Set PC{str} mode to normal failed");
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

        protected void SetPcModel(int id, List<int> _lstPcList,PcCtrlMode mode)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                string str = null;
                foreach (var item in _lstPcList)
                {
                    str += item.ToString() + ",";
                }
                str = str.Remove(str.Length - 1);
                Notify($"Set PC{str} mode to {mode.ToString()}");

                if (!_pm.SetPcModel(_lstPcList,mode))
                {
                    Stop($"Set PC{str} mode to {mode.ToString()} failed");
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

        protected void SetPcToDefault(int id, List<int> _lstPcList)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                string str = null;
                foreach (var item in _lstPcList)
                {
                    str += item.ToString() + ",";
                }
                str = str.Remove(str.Length - 1);
                Notify($"Set PC{str} value to default");
                if (!_pm.SetPCValueToDefault(_lstPcList))
                {
                    Stop($"Set PC{str} value to default failed");
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

        //protected void SetThrottlePositionToZero(int id, IoThrottleValve2 _IoThrottle)
        //{
        //    Tuple<bool, Result> ret = Execute(id, () =>
        //    {
        //        Notify($"Set Throttle pozition to 0");

        //        _IoThrottle.SetPositionToZero("", null);
        //        return true;
        //    });

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        //protected void SetThrottlePressureToCurrent(int id, IoThrottleValve2 _IoThrottle)
        //{
        //    Tuple<bool, Result> ret = Execute(id, () =>
        //    {
        //        double currentPressure = _pm.PT1.FeedBack;
        //        Notify($"Set Throttle value to {currentPressure}");

        //        string reason = String.Empty;

        //        if (!_IoThrottle.SetPressure(out reason, 0, new object[] { currentPressure }))
        //        {
        //            Stop($"Set Throttle value to {currentPressure} failed");
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
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        //protected void SetThrottlePressureAndSetMfcRamp(int id, IoThrottleValve2 _IoThrottle, MfcGroupName groupName, double pressure, double _pmPressureMaxDiff, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Set Throttle value to {pressure}");
        //        string reason = String.Empty;

        //        _pm.SetMfcRampByGroupAndPressure(groupName, pressure);
        //        _IoThrottle.SetPressure(out reason, 0, new object[] { pressure });
        //        return true;
        //    },
        //    () =>
        //    {
        //        return Math.Abs(_IoThrottle.PressureFeedback - pressure) <= _pmPressureMaxDiff;
        //    },
        //    timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.FAIL)
        //        {
        //            throw (new RoutineFaildException());
        //        }
        //        else if (ret.Item2 == Result.TIMEOUT)
        //        {
        //            Stop($"Set Throttle Pressure to {pressure} timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        /// <summary>
        /// 设置压力是上升还是下降趋势，以便调节特殊MFC的流量
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pressureUpOrD"></param>
        protected void SetPressureUpOrDown(int id,PressureUpOrDown pressureUpOrD)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _setMfc291519to38Flag = false;
                currentPressureUpOrDown = pressureUpOrD;
                if (currentPressureUpOrDown == PressureUpOrDown.Uping)
                {
                    _lastPressureLevel = PressureLevel.Leavel10;
                }
                else if (currentPressureUpOrDown == PressureUpOrDown.Dowing)
                {
                    _lastPressureLevel = PressureLevel.Leavel10;
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


        protected void SetPressureUpOrDown(int id,double targetPressure)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                if (_pm.PT1.FeedBack > targetPressure)
                {
                    currentPressureUpOrDown = PressureUpOrDown.Dowing;
                }
                else
                {
                    currentPressureUpOrDown = PressureUpOrDown.Uping;
                }

                if (currentPressureUpOrDown == PressureUpOrDown.Uping)
                {
                    _lastPressureLevel = PressureLevel.Leavel0;
                }
                else if (currentPressureUpOrDown == PressureUpOrDown.Dowing)
                {
                    _lastPressureLevel = PressureLevel.Leavel9;
                }

                Notify($"Set PressureUpOrDown {currentPressureUpOrDown.ToString()}");
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

        /// <summary>
        /// 检查PM压力是否超过定值,以便省略步骤
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pmPressure"></param>
        protected void GetPressureCondition(int id,double pmPressure)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Check PM Pressure over {pmPressure}");
                _checkPMPressureOver1000 = _pm.GetChamberPressure() > pmPressure;
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

        /// <summary>
        /// 检查PM压力是否超过定值,以便省略步骤
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pmPressure"></param>
        protected void GetPressureConditionPT2(int id, double pmPressure)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _checkPMPt2Over1000 = _pm.PT2.FeedBack > pmPressure;
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

        /// <summary>
        /// 等待PM压力达到特定值
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pressure"></param>
        /// <param name="timeout"></param>
        protected void WaitPmPressureUPto(int id, double pressure, int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                return _pm.GetChamberPressure() > pressure; 
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
                    Stop($"Wait Pm Pressure to {pressure} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetThrottlePressureAndWaitSetPoint(int id, IoThrottleValve2 _IoThrottle, double pressure, double _pmPressureMaxDiff, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle pressure to {pressure} and wait");
                string reason = String.Empty;

                //将当前值赋值给设定值
                _IoThrottle.PressureSetpoint = _IoThrottle.PressureFeedback;

                if (!_IoThrottle.SetPressure(out reason, 0, new object[] { pressure }))
                {
                    Stop($"Set Throttle pressure to { pressure} failed, { reason}");
                    return false;
                }

                return true;
            },
            () =>
            {
                return Math.Abs(_IoThrottle.PressureSetpoint - pressure) <= 0.1;
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
                    Stop($"Set Throttle Pressure to {pressure} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }



        protected void SetThrottlePressureAndWait(int id, IoThrottleValve2 _IoThrottle, double pressure, double _pmPressureMaxDiff, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle pressure to {pressure} and wait");
                string reason = String.Empty;
               
                if (!_IoThrottle.SetPressure(out reason, 0, new object[] { pressure }))
                {
                    Stop($"Set Throttle pressure to { pressure} failed, { reason}");
                    return false;
                }

                return true;
            },
            () =>
            {
                return  Math.Abs(_IoThrottle.PressureFeedback - pressure) <= _pmPressureMaxDiff;
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
                    Stop($"Set Throttle Pressure to {pressure} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
        
        protected void SetThrottleToTargetAndNoWait(int id, IoThrottleValve2 _IoThrottle, double pressure)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set Throttle pressure to {pressure}");
                string reason = String.Empty;

                if (!_IoThrottle.SetPressure(out reason, 0, new object[] { pressure }))
                {
                    Stop($"Set Throttle pressure to {pressure} failed, {reason}");
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

        protected void WaitThrottleToPressureAndSetMfcSpecial(int id, IoThrottleValve2 _IoThrottle, double pressure, double _pmPressureMaxDiff, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                return true;
            },
           () =>
           {
               SetMfcRampByGroupAndPressureAlways(currentPressureUpOrDown);
               return Math.Abs(_IoThrottle.PressureFeedback - pressure) <= _pmPressureMaxDiff;
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
                    Stop($"Set Throttle Pressure to {pressure} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


       
        /// <summary>
        /// 等待压力上升下降过程中(顺便调节MFC2,9,15,19到38 以及特殊MFC流量)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="_IoThrottle"></param>
        /// <param name="pressure"></param>
        /// <param name="_pmPressureMaxDiff"></param>
        /// <param name="timeout"></param>
        protected void WaitThrottleToPressureAndSetMfcSpecialForLidOpen(int id, IoThrottleValve2 _IoThrottle, double pressure, double _pmPressureMaxDiff, int timeout)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                if (_IoThrottle.PressureFeedback > 20)
                {
                    SetMfcRampByGroupAndPressureAlways(currentPressureUpOrDown);
                }

                if (!_setMfc291519to38Flag && currentPressureUpOrDown == PressureUpOrDown.Dowing && _IoThrottle.PressureFeedback < 20)
                {
                    Notify($"Set M2toM40 value to 0 ");
                    _pm.SetMfcByGroup(MfcGroupName.M2toM40, 0, 2);
                    _setMfc291519to38Flag = true;
                }
                else if (!_setMfc291519to38Flag && currentPressureUpOrDown == PressureUpOrDown.Uping && _IoThrottle.PressureFeedback < 20)
                {
                    Notify($"Set M2toM40 value to default");
                    _pm.SetMfcToDefaultByGroupRamp(MfcGroupName.M2toM40, 2);
                    _setMfc291519to38Flag = true;
                }
                
                return Math.Abs(_IoThrottle.PressureFeedback - pressure) <= _pmPressureMaxDiff;
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
                    Stop($"Set Throttle Pressure to {pressure} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        /// <summary>
        /// 在Wait步骤之前打印日志
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        protected void NotifyInfo(int id,string message)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify(message);
                return true;
            });
        }

        protected void SetThrottleToPressModeAndWait(int id, IoThrottleValve2 _IoThrottle, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle To PressMode"); 
                if (!_IoThrottle.SetMode(Aitex.Core.Common.DeviceData.PressureCtrlMode.TVPressureCtrl, out string reason))
                {
                    Stop($"Set Throttle To PressMode failed, {reason}");
                    return false;
                }
                return true;
            }, () =>
            {
                return _IoThrottle.ControlModeFeedback == Aitex.Core.Common.DeviceData.PressureCtrlMode.TVPressureCtrl;
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
                    Stop($"Set Throttle To PressMode timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetThrottleToCloseMode(int id, IoThrottleValve2 _IoThrottle, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle To CloseMode");
                if (!_IoThrottle.SetMode(Aitex.Core.Common.DeviceData.PressureCtrlMode.TVClose, out string reason))
                {
                    Stop($"Set Throttle To CloseMode failed, {reason}");
                    return false;
                }
                return true;
            }, () =>
            {
                return _IoThrottle.ControlModeFeedback == PressureCtrlMode.TVClose;
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
                    Stop($"Set Throttle To CloseMode timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        //设置蝶阀为位置模式
        protected void SetThrottleToPositionMode(int id, IoThrottleValve2 _IoThrottle, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle To PositionMode");
                if (!_IoThrottle.SetMode(PressureCtrlMode.TVPositionCtrl, out string reason))
                {
                    Stop($"Set Throttle To PositionMode failed, {reason}");
                    return false;
                }
                return true;
            }, () =>
            {
                return _IoThrottle.ControlModeFeedback == PressureCtrlMode.TVPositionCtrl;
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
                    Stop($"Set Throttle To PositionMode timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetThrottleSetPositionNoWait(int id, IoThrottleValve2 _IoThrottle, float position, int timeout)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set Throttle To Position to {position}");
                if (!_IoThrottle.SetPosition(out string reason, timeout, new object[] { position }))
                {
                    Stop($"Set Throttle To Position to {position} failed, {reason}");
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

        protected void SetThrottleSetPosition(int id, IoThrottleValve2 _IoThrottle, float position,int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle To Position to {position}");
                if (!_IoThrottle.SetPosition(out string reason,timeout,new object[] { position}))
                {
                    Stop($"Set Throttle To Position to {position} failed, {reason}");
                    return false;
                }
                return true;
            }, () =>
            {
                return _IoThrottle.PositionFeedback == position;
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
                    Stop($"Set Throttle To Position to {position} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetThrottleEnableAndWait(int id, IoThrottleValve2 _IoThrottle, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle Enable");
                if (!_IoThrottle.SetTVEnableState(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _IoThrottle.TVValveEnable == true ;
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
                    Stop($"Set Throttle Enable timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void SetThrottleDisable(int id, IoThrottleValve2 _IoThrottle,int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Throttle to Disable");
                string reason = String.Empty;

                if (!_IoThrottle.SetTVEnableState(false, out reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            },
            () => _IoThrottle.TVValveEnable == false,
            timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Set Throttle to Disable timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected bool IsCmdSkip(string cmd)
        {
            if (cmd == "StepUid" || cmd == "SiSourTotalFlow" || cmd == "CSourTotalFlow" || cmd == "DopeTotalFlow" || cmd == "TotalVentFlow")
                return true;

            return false;
        }

        protected string GetMfcTipsByGroup(MfcGroupName groupName)
        {
            return groupName.ToString();
        }

        protected void SetConfinementRingUpAndWait(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set ConfinementRing Up");

                if (!_pm.ConfinementRing.MoveUpPos(out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _pm.ConfinementRing.RingDone && _pm.ConfinementRing.RingUpSensor;
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
                    Stop($"Set ConfinementRing Up, over {timeout} seconds");
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

                if(!_pm.ConfinementRing.MoveDownPos(out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _pm.ConfinementRing.RingDone && _pm.ConfinementRing.RingDownSensor;
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


        protected void ConfinementServoOn(int id,int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set ConfinementRing Servo On");

                if (!_pm.ConfinementRing.ServoOn(out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            },()=>
            {
                return _pm.ConfinementRing.RingServoOn;
            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Set ConfinementRing Servo On Failed, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        protected void RotationServoOn(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Set Rotation Servo On");

                if (!_pm._sicServo.SetServoEnable(true,out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            }, () =>
            {
                return _pm._sicServo.ServoEnable;
            },timeout *1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT)
                {
                    Stop($"Set Rotation Servo On Failed, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
