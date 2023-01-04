using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;

namespace Mainframe.TMs.Routines
{
    public class TMPressureBalanceRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            CheckPressureCondition,

            PumpDownToBase,

            CloseIsoValve,
            OpenTmSlowVent,

            VentToSetPoint,
            StopVent,

            WaitPressureToSetPoint,
            SlowPump, FastPump, 
            VaccumDelay, 
            CloseValves, 
            CLoseSlowValue,
            CheckNeedVent,
            DelayBeforeDone
        }


        private TM _tm;
        private TMPumpRoutine _pumpDownRoutine;

        private ModuleName _paramTarget;        //每个模块包含以下三个参数↓
        double maxDifferent = 0;
        private bool _needMfcVent;             //是否需要Vent操作
        private double _ventMfcFlow;           //MFC在Vent时设置的值
        private double _balancePressure;       //目标压力值

        private int _ventTimeout;              //MFC 超时 
        private int _waitPressureTimeout;      //不需要Vent操作时，等待压力平衡时间

        private bool _needPumpDown;             //执行Routine之前已经是真空则不需要抽真空
        private bool _needBalance;              //执行Routine之前已经在设定压力值则不需要继续平衡
        private double _slowFastPumpSwitchPressure; //慢快抽切换压力
        private int _fastPumpTime;
        private int _slowPumpTimeout;
        private bool isAtmMode = false;

        public TMPressureBalanceRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "Pressure Balance";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ ModuleName.TM.ToString()}");

            _pumpDownRoutine = new TMPumpRoutine();
            _paramTarget = ModuleName.System;
        }

        public void Init(ModuleName target)
        {
            Name = "Pressure Balance with " + target;
            _paramTarget = target;
        }

        public override Result Start(params object[] objs)
        {
            if (ModuleHelper.IsPm(_paramTarget))
            {
                _balancePressure = SC.GetValue<double>($"TM.PressureBalance.BalancePressure") + +SC.GetValue<double>("TM.TMPressureBigThanPM");                
            }
            else 
            {
                _balancePressure = SC.GetValue<double>("TM.PressureBalance.BalancePressure");
            }

            _ventMfcFlow = SC.GetValue<double>($"TM.PressureBalance.MfcFlow");
            _ventTimeout = SC.GetValue<int>("TM.PressureBalance.VentTimeout");
            _waitPressureTimeout = SC.GetValue<int>("TM.PressureBalance.WaitPressureAboveSetPointTimeout");
            _slowFastPumpSwitchPressure = SC.GetValue<double>("TM.Pump.SlowFastPumpSwitchPressure");
            _fastPumpTime = SC.GetValue<int>("TM.Pump.FastPumpTimeout");
            _slowPumpTimeout = SC.GetValue<int>("TM.Pump.PumpSlowTimeout");

            //_needPumpDown = !_tm.CheckVacuum();
            maxDifferent = SC.GetValue<double>("TM.PressureBalance.BalanceMaxDiffPressure");

            isAtmMode = SC.GetValue<bool>("System.IsATMMode");

            if ((Math.Abs(_tm.ChamberPressure - _balancePressure) < maxDifferent) && _balancePressure != 0)
            {
                return Result.DONE;
            }

            _needPumpDown = (_tm.ChamberPressure > _balancePressure) || _balancePressure == 0;

            _needMfcVent = _balancePressure > 10;

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

                if (_needPumpDown)
                {
                    if (_balancePressure < _slowFastPumpSwitchPressure)
                    {
                        OpenSlowPump((int)RoutineStep.SlowPump, _tm, _slowFastPumpSwitchPressure, _slowPumpTimeout);
                        OpenFastPump((int)RoutineStep.FastPump, _tm, _balancePressure, _fastPumpTime);
                        CloseFastPump((int)RoutineStep.CloseValves, _tm);
                        CloseSlowPump((int)RoutineStep.CLoseSlowValue, _tm);
                    }
                    else
                    {
                        OpenSlowPump((int)RoutineStep.SlowPump, _tm, _balancePressure, _slowPumpTimeout);
                        CloseSlowPump((int)RoutineStep.CLoseSlowValue, _tm);
                    }
                }
                CheckNeedVent((int)RoutineStep.CheckNeedVent);

                if (_needMfcVent)
                {
                    VentToSetPoint((int)RoutineStep.VentToSetPoint, _tm, _balancePressure, _ventMfcFlow, _ventTimeout);
                    StopVent((int)RoutineStep.StopVent, _tm, 2);
                }
                
                // 等待一会确保DI到正确状态，主要针对模拟器DI问题
                TimeDelay((int)RoutineStep.DelayBeforeDone, 3);
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
            _tm.SetSlowVentValve(false, out string reason);
            _tm.SetFastVentValve(false, out reason);
            _tm.SetSlowPumpValve(false, out reason);
            _tm.SetFastPumpValve(false, out reason);
            Notify("Aborted");

            base.Abort();
        }

        private void CheckNeedVent(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                if ((Math.Abs(_tm.ChamberPressure - _balancePressure) < maxDifferent) && _balancePressure != 0)
                {
                    _needMfcVent = false;
                }
                else if (_tm.ChamberPressure < _balancePressure)
                {
                    _needMfcVent = true;
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

        public void OpenTMSlowVent(int id,int delayTime)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                Notify($"Open tm slowVent");
                if (!_tm.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }
                return true;
            }, delayTime * 1000);

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

        public void VentToSetPoint(int id, TM tm, double balancePressure, double ventFlow, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"vent to {balancePressure} mbar with {ventFlow} sccm");
                if (!_tm.SetSlowVentValve(true, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                if (!_tm.SetVentMfc(ventFlow, out reason))
                {
                    Stop(reason);
                    return false;
                }

                return true;
            }, () =>
            {
                return _tm.ChamberPressure >= balancePressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"can not vent to {balancePressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }


        public void StopVent(int id, TM tm, int delayTime)
        {
            Tuple<bool, Result> ret = Wait(id, () =>
            {
                Notify($"stop vent");

                if (!_tm.SetSlowVentValve(false, out string reason))
                {
                    Stop(reason);
                    return false;
                }

                if (!_tm.SetVentMfc(0, out reason))
                {
                    Stop(reason);
                    return false;
                }

               
                return true;
            }, delayTime * 1000);

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


        public void WaitPressureToSetPoint(int id, TM tm, double pressure, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                Notify($"Wait pressure above {pressure} mbar");

                return true;
            }, () =>
            {
                return tm.ChamberPressure >= pressure;

            }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{tm.Name} pressure can not above {pressure} in {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }
    }
}
