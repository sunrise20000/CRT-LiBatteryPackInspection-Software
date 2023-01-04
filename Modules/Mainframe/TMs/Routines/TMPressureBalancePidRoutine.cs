using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using SicPM.Devices;

namespace Mainframe.TMs.Routines
{
    public class TMPressureBalancePidRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {       
            OpenValve,
            RunPid,
            SetM40,
            CloseValve,
            OpenSlowVent,
            CloseSlowVent,
            OpenSlowPump,
            CloseSlowPump,
            Delay1,
        }


        private bool _pidAlways = false;
        private SicTM _tm;
        private IoMFC _m40;

        private IoValve _TMTOLLVent;
        private ModuleName _paramTarget;
        private double _pidP;
        private double _pidI;
        private double _pidD;

        //private bool _needMfcVent;             //是否需要Vent操作
        //private double _ventMfcFlow;           //MFC在Vent时设置的值
        private double _balancePressure;       //目标压力值
        private int _ventTimeout;              //MFC 超时 
        private double _pidMaxDiffPressure;    //PID与目标压力的差

        //private int _waitPressureTimeout;      //不需要Vent操作时，等待压力平衡时间

        private bool _isPidSucceed = false;
        private bool _isPidRunning = true;
        private int _pidSuccessCount;      //压力到达范围内比较多少次就认为PID成功
        private int _pidSleepTime;
        private bool _pressureAboveTarget = false;
        private bool _needVentOrPump = false;

        List<double> lstPressure = new List<double>();

        public TMPressureBalancePidRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "Pressure Balance PID";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ Module}");
            _m40 = DEVICE.GetDevice<IoMFC>($"TM.Mfc40");
        }

        public void Init(ModuleName target)
        {
            Name = "Pressure Balance PID with " + target;
            _paramTarget = target;
            _pidAlways = false;
        }

        public void Init(ModuleName target, bool alwaysPID)
        {
            Name = "Pressure Balance PID with " + target;
            _paramTarget = target;
            _pidAlways = alwaysPID;
        }

        public override Result Start(params object[] objs)
        {
            if (ModuleHelper.IsPm(_paramTarget))
            {
                if (!SC.ContainsItem($"TM.PressureBalance.BalancePressure"))
                {
                    EV.PostAlarmLog(Module, $"did not define balance pressure for {_paramTarget}");
                    return Result.FAIL;
                }
                _balancePressure = SC.GetValue<double>($"TM.PressureBalance.BalancePressure") + SC.GetValue<double>("TM.TMPressureBigThanPM"); //TM的压力需要大于PM数值
            }
            else
            {
                _balancePressure = SC.GetValue<double>("TM.PressureBalance.BalancePressure");
            }

            _ventTimeout = SC.GetValue<int>("TM.PressureBalance.VentTimeout");

            _pidP = SC.GetValue<double>("TM.PressureBalance.PidP");
            _pidI = SC.GetValue<double>("TM.PressureBalance.PidI");
            _pidD = SC.GetValue<double>("TM.PressureBalance.PidD");

            _pidMaxDiffPressure = SC.GetValue<double>("TM.PressureBalance.BalanceMaxDiffPressure");
            _pidSuccessCount = SC.GetValue<int>("TM.PressureBalance.SuccessCount");
            _pidSleepTime = SC.GetValue<int>("TM.PressureBalance.SleepTime");

            if (_balancePressure <= 1)
            {
                return Result.DONE;
            }

            _isPidSucceed = false;

            _needVentOrPump = true;
            if (Math.Abs(_tm.ChamberPressure - _balancePressure) < 10)
            {
                _needVentOrPump = false;
            }
            else
            {
                if (_tm.ChamberPressure > _balancePressure)
                {
                    _pressureAboveTarget = true;
                }
                else
                {
                    _pressureAboveTarget = false;
                }
            }

            _tm.SetVentMfc(10, out string reasonxx);

            Notify("Start");
            return Result.RUN;
        }

        public override Result Monitor()
        {
            try
            {
                if (SC.GetValue<bool>("System.IsATMMode"))
                {
                    return Result.DONE;
                }

                if (_needVentOrPump)
                {
                    if (_pressureAboveTarget)
                    {
                        OpenSlowPumpToTarget((int)RoutineStep.OpenSlowPump, _tm, _balancePressure, 200);
                        CloseSlowPump((int)RoutineStep.CloseSlowPump, _tm);
                        TimeDelay((int)RoutineStep.Delay1, 1);
                    }
                    else
                    {
                        OpenSlowVentToTarget((int)RoutineStep.OpenSlowVent,_tm, _balancePressure,200);
                    }
                }

                SetPIDValve((int)RoutineStep.OpenValve, _tm, true, 2);

                RunPid((int)RoutineStep.RunPid, _ventTimeout);

                //不需要关闭阀门
                if (!_pidAlways)
                {
                    SetPIDValve((int)RoutineStep.CloseValve, _tm, false, 2);
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

            return Result.DONE;
        }

        public override void Abort()
        {
            _isPidRunning = false;
            _tm.SetPIDValve(false, out string reason);

            _tm.SetSlowVentValve(false, out reason);
            _tm.SetFastVentValve(false, out reason);
            _tm.SetSlowPumpValve(false, out reason);
            _tm.SetFastPumpValve(false, out reason);
        }



        private bool PidPressureReach()
        {
            if (lstPressure.Count >= _pidSuccessCount)
            {
                for (int i = 0; i < _pidSuccessCount; i++)
                {
                    if (Math.Abs(lstPressure[i]) > _pidMaxDiffPressure)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RunPid(int id, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Run PID for {_paramTarget}");

                Task.Factory.StartNew(() => TMPressureBalancePid(timeout));

                return true;
            }, () =>
            {
                return _isPidSucceed;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Run PID for {_paramTarget} timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void TMPressureBalancePid(int timeOut)
        {
            bool firstReachTarget = false;
            bool isLowThanTarget = _tm.ChamberPressure <_balancePressure;

            DeviceTimer timer = new DeviceTimer();
            timer.Start(timeOut * 1000);

            double MP = 0;
            double MI = 0;
            double MD = 0;
            double M = 0;
            
            double LE = 0;
            double E = _balancePressure - _tm.ChamberPressure;

            while (_isPidRunning)
            {
                //第一次穿过目标值时,重新开始PID,以节省调节时间
                if (firstReachTarget == false)
                {
                    if (isLowThanTarget && _tm.ChamberPressure > _balancePressure)
                    {                        
                        MP = 0;
                        MI = 0;
                        MD = 0;
                        M = 0;
                        LE = 0;
                        E = _balancePressure - _tm.ChamberPressure;
                        lstPressure = new List<double>();
                        firstReachTarget = true;
                    }
                    else if(!isLowThanTarget &&  _tm.ChamberPressure < _balancePressure)
                    {
                        MP = 0;
                        MI = 0;
                        MD = 0;
                        M = 0;
                        LE = 0;
                        E = _balancePressure - _tm.ChamberPressure;
                        lstPressure = new List<double>();
                        firstReachTarget = true;
                    }
                }

                if (lstPressure.Count > _pidSuccessCount)
                {
                    lstPressure.RemoveAt(0);
                }
                LE = E;
                E = _balancePressure - _tm.ChamberPressure;
                lstPressure.Add(E);

                MP = _pidP * E;
                MI += _pidI * E;
                MD = _pidD * (E - LE);
                M = MP + MI + MD;

                _tm.SetVentMfc(M, out string reason);


                _isPidRunning = !PidPressureReach();
                if (timer.IsTimeout())
                {
                    _isPidSucceed = false;
                    _isPidRunning = false;
                }

                Thread.Sleep(_pidSleepTime);
            }

            _isPidSucceed = true;
        }

        public void SetM40(int id, IoMFC m40)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Set M40 flow default value");

                m40.Terminate();

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

        public void SetPIDValve(int id, SicTM tm, bool isOpen, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open Slow Vent Valve and V121 for PID");

                if (!tm.SetPIDValve(isOpen, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return tm.CheckPIDValve(isOpen);

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"Set PID valve timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        //public void RunPid(int id, int timeout)
        //{
        //    Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
        //    {
        //        Notify($"Run PID for {_paramTarget}");

        //        Task.Factory.StartNew(() => TMPressureBalancePid(timeout));

        //        return true;
        //    }, () =>
        //    {
        //        return _isPidSucceed;

        //    }, timeout * 1000);

        //    if (ret.Item1)
        //    {
        //        if (ret.Item2 == Result.TIMEOUT) //timeout
        //        {
        //            Stop($"Run PID for {_paramTarget} timeout, over {timeout} seconds");
        //            throw (new RoutineFaildException());
        //        }
        //        else
        //            throw (new RoutineBreakException());
        //    }
        //}

        //private void TMPressureBalancePid(int timeout)
        //{
        //    string reason = string.Empty;

        //    try
        //    {
        //        _isPidRunning = true;
        //        double tolerance = _maxPressureDiffOpenSlitValve;
        //        double lowLimit = _balancePressure - 0.5;
        //        double upLimit = _balancePressure + tolerance - 1;

        //        double FP, FD;
        //        double FI = _tm.ChamberPressure;
        //        double Pn = FI;
        //        double Fn;

        //        int step = 0;
        //        double mfcFlow = 0;

        //        List<double> lstPressure = new List<double>();

        //        DeviceTimer timer = new DeviceTimer();
        //        timer.Start(timeout * 1000);

        //        while (_isPidRunning)
        //        {
        //            if (_tm.ChamberPressure >= lowLimit && _tm.ChamberPressure < upLimit)
        //            {
        //                lstPressure.Add(_tm.ChamberPressure);

        //                if (lstPressure.Count >= 100)
        //                {
        //                    double avg = lstPressure.Average();
        //                    lstPressure.RemoveAt(0);

        //                    if (avg >= lowLimit && avg < upLimit)
        //                    {
        //                        if (!_isPidSucceed)
        //                        {
        //                            _isPidSucceed = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }

        //            switch (step)
        //            {
        //                case 0:
        //                    if (_tm.ChamberPressure >= _balancePressure + 2)
        //                    {
        //                        _tm.SetVentMfc(0, out reason);
        //                        _tm.SetMfcVentValve(false, out reason);

        //                        if (!_tm.SetFastPumpValve(true, out reason))
        //                        {
        //                            Stop(reason);
        //                            step = 10;
        //                        }
        //                        else
        //                        {
        //                            step = 2; // 1  为了防止气流不稳，不能关闭Pump阀门
        //                        }
        //                    }
        //                    else
        //                    {
        //                        step = 2; // 1 为了防止气流不稳，不能关闭Pump阀门
        //                    }
        //                    break;
        //                case 2:
        //                    if (_tm.ChamberPressure < _balancePressure - 1)
        //                    {
        //                        if (mfcFlow < _ventMfcFlow)
        //                        {
        //                            mfcFlow += _ventMfcStepFlow;
        //                        }
        //                        else
        //                        {
        //                            mfcFlow = 0;
        //                        }
        //                        _tm.SetVentMfc(mfcFlow, out reason);
        //                        _tm.SetMfcVentValve(true, out reason);
        //                    }
        //                    else
        //                    {
        //                        step = 4; // 3，case 3 没用了
        //                    }

        //                    break;
        //                case 4:
        //                    FP = _pidP * (_balancePressure - _tm.ChamberPressure);
        //                    if (Math.Abs(Pn - _tm.ChamberPressure) < 3.0 * tolerance)
        //                    {
        //                        FI = FI + _pidI * (_balancePressure - _tm.ChamberPressure);
        //                    }
        //                    FD = _pidD * (Pn - _tm.ChamberPressure);
        //                    Fn = FP + FI + FD;

        //                    Fn = Math.Min(Fn, 7000.0);
        //                    Fn = Math.Max(Fn, 5000.0);

        //                    if (Fn < 0)
        //                    {
        //                        Fn = 0;
        //                    }
        //                    else if (Fn > _ventMfcFlow)
        //                    {
        //                        Fn = _ventMfcFlow;
        //                    }

        //                    _tm.SetVentMfc(Fn, out reason);
        //                    _tm.SetMfcVentValve(true, out reason);

        //                    Pn = _tm.ChamberPressure;

        //                    step = _tm.ChamberPressure > (_balancePressure + 3) ? 0 : 2; // 0 : 1  为了防止气流不稳，不能关闭Pump阀门

        //                    if (timer.IsTimeout())
        //                    {
        //                        step = 10;
        //                    }
        //                    break;
        //                case 10:
        //                    _isPidRunning = false;
        //                    _isPidSucceed = false;
        //                    break;
        //            }
        //            Thread.Sleep(100);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _isPidRunning = false;
        //        _isPidSucceed = false;
        //        LOG.Write(ex, $"Run PID for {_paramTarget} failed");
        //    }

        //    _tm.SetVentMfc(0, out reason);
        //    _tm.SetMfcVentValve(false, out reason);
        //    _tm.SetFastPumpValve(false, out reason);

        //    if (_isPidSucceed)
        //    {
        //        Notify($"Succeed");
        //    }
        //    else
        //    {
        //        Notify($"Failed");
        //    }
        //}

        public void OpenSlowPumpToTarget(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} slow pump valve");

                if (!tm.SetSlowPumpValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return Math.Abs(tm.ChamberPressure - basePressure) < 10;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastPumpValve(false, out string _);

                    Stop($"{tm.Name} pressure can not pump to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowPump(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} pump valves");

                if (!_tm.SetSlowPumpValve(false, out string reason))
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

        public void OpenSlowVentToTarget(int id, TM tm, double basePressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Open {tm.Name} slow vent valve");

                if (!tm.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return Math.Abs(tm.ChamberPressure - basePressure) < 10;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    tm.SetFastPumpValve(false, out string _);

                    Stop($"{tm.Name} pressure can not vent to {basePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void CloseSlowVent(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Close {tm.Name} vent valves");

                if (!_tm.SetSlowVentValve(false, out string reason))
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

    }
}
