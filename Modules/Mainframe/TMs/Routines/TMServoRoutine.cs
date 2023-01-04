using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace Mainframe.TMs.Routines
{
    public class TMServoRoutine : TMBaseRoutine
    {
        enum RoutineStep
        {
            ChamberBalance,
            OpenTMToLLVent,
            CheckPressureCondition,

            SetSlitValve,
            LLRoutine,

            Delay,
            Delay1,
            CloseTMToLLVent,
            PumpDownToBase,
        }

        private TMPressureBalanceRoutine _balanceRoutine;
        private TMPressureBalancePidRoutine _pidBalanceRoutine;

        private SicTM _tm;

        private bool _needPressureBalance;
        private bool _userPidBalance;

        public TMServoRoutine()
        {
            Module = ModuleName.TM.ToString();
            Name = "ServoPressure";
            _tm = DEVICE.GetDevice<SicTM>($"{ ModuleName.System.ToString()}.{ ModuleName.TM.ToString()}");
        }

        public override Result Start(params object[] objs)
        {
            Reset();

            _balanceRoutine = new TMPressureBalanceRoutine();
            _pidBalanceRoutine = new TMPressureBalancePidRoutine();
            _balanceRoutine.Init(ModuleName.PM1);
            _pidBalanceRoutine.Init(ModuleName.PM1);

            /*_needPressureBalance = SC.GetValue<bool>("TM.NeedPressureBalance");
            _userPidBalance = SC.GetValue<bool>("TM.PressureBalanceUsePid");*/
            _needPressureBalance = true;
            _userPidBalance = false;

            ModuleName[] modules = { ModuleName.LoadLock, ModuleName.PM1, ModuleName.UnLoad};
            foreach (var moduleName in modules)
            {
                if (!_tm.CheckSlitValveClose(moduleName))
                {
                    EV.PostAlarmLog(Module, $"Can not pump, {moduleName} slit valve not closed");
                    return Result.FAIL;
                }
            }

            string reason;
            if (!_tm.SetFastPumpValve(false, out reason) || !_tm.SetFastVentValve(false, out reason))
            {
                EV.PostAlarmLog(Module, $"Can not turn off valves, {reason}");
                return Result.FAIL;
            }

            if (SC.GetValue<bool>("System.IsATMMode"))
            {
                return Result.DONE;
            }

            Notify("Start");

            return Result.RUN;
        }


        public override Result Monitor()
        {
            try
            {
                if (_userPidBalance)
                {
                    ExecuteRoutine((int)RoutineStep.ChamberBalance, _pidBalanceRoutine);
                }
                else
                {
                    ExecuteRoutine((int)RoutineStep.ChamberBalance, _balanceRoutine);
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
            if (_userPidBalance)
            {
                _pidBalanceRoutine.Abort();
            }
            else
            {
                _balanceRoutine.Abort();
            }
        }
    }
}
