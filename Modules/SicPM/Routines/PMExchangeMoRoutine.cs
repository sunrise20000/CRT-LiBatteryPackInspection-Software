using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using SicPM.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SicPM.Routines
{
    public class PMExchangeMoRoutine : PMBaseRoutine
    {
        private enum RoutineStep
        {
            SetGroupE,
            SetGroupK,

            SetM7,
            SetM8,
            SetM11,
            SetPc2,

            OpenV73,
            CloseV73,
            SetGroup1,
            SetGroup11,
            SetGroup2,
            SetGroup3,
            SetGroup4,
            SetGroup5,
            SetGroup6,

            StartLoop,
            EndLoop,

            SetM7Default,
            SetM8Default,
            SetPc2Default,
            SetM11Default,
            SetGroupV25,


            TimeDelay1,
            TimeDelay2,
            TimeDelay3,
            TimeDelay4,
            TimeDelay5,
            TimeDelay6,
            TimeDelay7,
            TimeDelay8,
            TimeDelay9,
            TimeDelay10,
            TimeDelay11,
        }

        private bool _isTCS = false;        //标记是TMA换源还是TCS换源
        private PMModule _pmModule;
        private ModuleName moduleName;
        private IoInterLock _pmIoInterLock;

        private int _IoValueOpenCloseTimeout = 10; //开关超时时间
        private int _loopCount;
        private double _OpenTime = 10;
        private double _CLoseTime = 10;
        private int _routineTimeOut;

        //默认为TMA换源参数
        private int mfc7Or10 = 7;
        private int mfc8Or12 = 8;
        private int mfc11 = 11;
        private int pc2Or3 = 2;
        private List<string> _lstGroupV73 = new List<string>() { "V73" };
        private List<string> _lstGroupV50 = new List<string>() { "V50" };
        private List<string> _lstGroupV48 = new List<string>() { "V48" };
        private List<string> _lstGroupV49 = new List<string>() { "V49" };

        private List<string> _lstGroup1 = new List<string>() { "V46","V46s","V73" };
        private List<string> _lstGroup2 = new List<string>() { "V46", "V46s" };
        private List<string> _lstGroup3 = new List<string>() { "V43", "V43s", "V45" };

        private Stopwatch _swTimer = new Stopwatch();
        public PMExchangeMoRoutine(ModuleName module, PMModule pm) : base(module, pm)
        {
            moduleName = module;
            _pmModule = pm;
            Name = "MoExchange";
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }

        public void Init(string moName)
        {
            if (moName.ToUpper().Contains("TCS"))
            {
                _isTCS = true;
            }
            else
            {
                _isTCS = false;
            }
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            if (_pmModule.V69.Status)
            {
                EV.PostAlarmLog(Module, $"can not Exchange Mo,V69 should be Closed");
                return Result.FAIL;
            }
            if (_pmModule.V72.Status)
            {
                EV.PostAlarmLog(Module, $"can not Exchange Mo,V72 should be Closed");
                return Result.FAIL;
            }
            if (_pmModule.V74.Status)
            {
                EV.PostAlarmLog(Module, $"can not Exchange Mo,V74 should be Closed");
                return Result.FAIL;
            }
            if (_pmModule.EPV2.Status)
            {
                EV.PostAlarmLog(Module, $"can not Exchange Mo,EPV2 should be Closed");
                return Result.FAIL;
            }

            _loopCount = SC.GetValue<int>($"PM.{Module}.MoExchange.CycleCount");
            _OpenTime= SC.GetValue<int>($"PM.{Module}.MoExchange.PumpTime");
            _CLoseTime = SC.GetValue<int>($"PM.{Module}.MoExchange.VentTime");
            _routineTimeOut = SC.GetValue<int>($"PM.{Module}.MoExchange.RoutineTimeOut");


            //设置TCS换源参数
            if (_isTCS)
            {
                mfc7Or10 = 10;
                mfc8Or12 = 12;
                pc2Or3 = 3;

                _lstGroup1 = new List<string>() { "V50"};
                _lstGroup2 = new List<string>() { "V48" };
                _lstGroup3 = new List<string>() { "V49" };
            }
            else
            {
                mfc7Or10 = 7;
                mfc8Or12 = 8;
                pc2Or3 = 2;

                _lstGroup1 = new List<string>() { "V46" };
                _lstGroup2 = new List<string>() { "V43" };
                _lstGroup3 = new List<string>() { "V45" };
            }

            if (!_pmIoInterLock.SetPMExchangeMoRoutineRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Exchange Mo,{reason}");
                return Result.FAIL;
            }
            _swTimer.Restart();
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

                CheckRoutineTimeOut();

                // 关闭E组和K组的阀门
                SetIoValueByGroup((int)RoutineStep.SetGroupE, IoGroupName.E, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupK, IoGroupName.K, false, _IoValueOpenCloseTimeout);
                SetIoValueByGroup((int)RoutineStep.SetGroupV25, IoGroupName.V25, true, _IoValueOpenCloseTimeout);


                //设定M7,M8 80%,PC2设定Max
                SetMfcValueByPercent((int)RoutineStep.SetM7, mfc7Or10, 80);
                SetMfcValueByPercent((int)RoutineStep.SetM8, mfc8Or12, 80);
                if (_isTCS)
                {
                    SetMfcValueByPercent((int)RoutineStep.SetM11, mfc11, 80);
                }
                SetPCValueByPercent((int)RoutineStep.SetPc2, pc2Or3, 100);

                //打开V73,V46,V46s，保持30s
                SetIoValueByList((int)RoutineStep.OpenV73, _lstGroupV73, true, _IoValueOpenCloseTimeout);
                SetIoValueByList((int)RoutineStep.SetGroup11, _lstGroup1, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay1, 30);

                //Loop
                Loop((int)RoutineStep.StartLoop, _loopCount);
                SetIoValueByList((int)RoutineStep.SetGroup1, _lstGroup1, false, _IoValueOpenCloseTimeout);
                SetIoValueByList((int)RoutineStep.SetGroup2, _lstGroup2, true, _IoValueOpenCloseTimeout);
                SetIoValueByList((int)RoutineStep.SetGroup3, _lstGroup3, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay2, _OpenTime);

                //SetIoValueByList((int)RoutineStep.SetGroup4, _lstGroup3, false, _IoValueOpenCloseTimeout);
                SetIoValueByList((int)RoutineStep.SetGroup5, _lstGroup2, false, _IoValueOpenCloseTimeout);
                SetIoValueByList((int)RoutineStep.SetGroup6, _lstGroup1, true, _IoValueOpenCloseTimeout);
                TimeDelay((int)RoutineStep.TimeDelay3, _CLoseTime);

                EndLoop((int)RoutineStep.EndLoop);

                //关闭V73,V46,V46s
                SetIoValueByList((int)RoutineStep.SetGroup6, _lstGroup1, false, _IoValueOpenCloseTimeout);
                SetIoValueByList((int)RoutineStep.CloseV73, _lstGroupV73, false, _IoValueOpenCloseTimeout);

                SetMfcValueToDefaultByID((int)RoutineStep.SetM7Default, mfc7Or10);
                SetMfcValueToDefaultByID((int)RoutineStep.SetM8Default, mfc8Or12);
                if (_isTCS)
                {
                    SetMfcValueToDefaultByID((int)RoutineStep.SetM11Default, mfc11);
                }
                SetPcToDefault((int)RoutineStep.SetPc2Default, new List<int> { pc2Or3 });

            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                return Result.FAIL;
            }

            Notify($"Finished ! Elapsed time: {(int)(_swTimer.ElapsedMilliseconds / 1000)} s");
            _swTimer.Stop();
            SetRoutineRuningDo();
            return Result.DONE;
        }

        private void SetRoutineRuningDo()
        {
            _pmIoInterLock.DoExchangeMoRoutineRunning = false;
        }

        public override void Abort()
        {
            SetRoutineRuningDo();

            PMDevice.SetIoValue(_lstGroup1,false);
            PMDevice.SetIoValue(_lstGroup2, false);
            PMDevice.SetMfcValueToDefault(new List<int> { mfc7Or10 });
            PMDevice.SetMfcValueToDefault(new List<int> { mfc8Or12 });
            PMDevice.SetMfcValueToDefault(new List<int> { mfc11 });
            PMDevice.SetPCValueToDefault(new List<int> { pc2Or3 });
            PMDevice.SetRotationServo(0, 0);

            base.Abort();
        }
        private void CheckRoutineTimeOut()
        {
            if (_routineTimeOut > 10)
            {
                if ((int)(_swTimer.ElapsedMilliseconds / 1000) > _routineTimeOut)
                {
                    EV.PostAlarmLog(Module, $"Routine TimeOut! over {_routineTimeOut} s");
                    throw (new RoutineFaildException());
                }
            }
        }

        private void SetIoValueByList(int id, List<string> lstIo, bool close, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
            {
                _pmModule.SetIoValue(lstIo, close);
                return true;
            }, () =>
            {
                return _pmModule.CheckIoValue(lstIo, close);
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
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        private void SetMfcValueToDefaultByID(int id, int mfcID)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                List<int> lst = new List<int> { mfcID };
                _pmModule.SetMfcModelToNormal(lst);
                _pmModule.SetMfcValueToDefault(lst);
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


        private void SetMfcValueByPercent(int id,int mfcID, double percent)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _pmModule.SetMfcModelToNormal(new List<int> { mfcID });
                _pmModule.SetMfcValueByPercent(mfcID, percent);
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

        private void SetPCValueByPercent(int id,int pcID,double percent)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                _pmModule.SetPcModelToNormal(new List<int> { pcID });
                _pmModule.SetPCValueByPercent(pcID, percent);
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
