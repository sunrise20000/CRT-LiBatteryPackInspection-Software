using System;
using System.Threading;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;

namespace Mainframe.TMs.Routines
{
    public class TMVerifySlitValveRoutine : ModuleRoutine, IRoutine
    {
        enum RoutineStep
        {
            VerifySlitValve,
        }
 

        private TM _tm;
 
        public TMVerifySlitValveRoutine( )
        {
            Module = ModuleName.TM.ToString();
            Name = "Verify Slit Valve Status";
            _tm = DEVICE.GetDevice<TM>($"{ ModuleName.System.ToString()}.{ Module}");
        }

        public void Init()
        {

        }

        public Result Start(params object[] objs)
        {
            Reset();

            Notify("Start");
            return Result.RUN;
        }


        public Result Monitor()
        {
            try
            {
                VerifySlitValve((int)RoutineStep.VerifySlitValve, _tm);
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

        public void Abort()
        {
             
        }

        public void VerifySlitValve(int id, TM tm)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
                {
                    Notify($"Check slot valve status {tm.Name}");

                    ModuleName[] lstModule = new ModuleName[] { ModuleName.LLA, ModuleName.LLB, ModuleName.PM1, ModuleName.PM2, ModuleName.PM3 };
                    foreach (var moduleName in lstModule)
                    {
                        if (_tm.CheckSlitValveOpen(moduleName))
                        {
                            if (!CheckPressureCondition(tm, moduleName.ToString(), out string reason))
                            {
                                EV.PostWarningLog(Module, $"Can not set {moduleName} slit valve open, {reason}");
                                continue;
                            }
                            if (!_tm.SetSlitValve(moduleName, true, out reason))
                            {
                                EV.PostWarningLog(Module, $"Can not set {moduleName} slit valve open, {reason}");
                            }
                        }
                        else
                        {
                            if (_tm.CheckSlitValveClose(moduleName))
                            {
                                if (!_tm.SetSlitValve(moduleName, false, out string reason))
                                {
                                    EV.PostWarningLog(Module, $"Can not set {moduleName} slit valve close, {reason}");
                                }
                            }
                        }

                        Thread.Sleep(500);
                    }
 
                    return true;
                });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }
 
        public bool CheckPressureCondition( TM tm, string target, out string reason)
        {
            reason = string.Empty;
 
                double tmPressure = tm.ChamberPressure;
                double targetPressure = 0.0;
                if (ModuleHelper.IsLoadLock(target))
                {
                    LoadLock ll = DEVICE.GetDevice<LoadLock>($"{target}.{target}");
                    targetPressure = ll.ChamberPressure;
                }
                else if (ModuleHelper.IsPm(target))
                {
                    PM pm = DEVICE.GetDevice<PM>(target);
                    targetPressure = pm.ChamberPressure;
                }
                else
                {
                    reason = ($"{target} not define pressure condition");
                    return false;
                }

                double maxPressureDiffOpenSlitValve = SC.GetValue<double>("TM.MaxPressureDiffOpenSlitValve");
                double pressureDiff = Math.Abs(tmPressure - targetPressure);
                if (pressureDiff > maxPressureDiffOpenSlitValve)
                {
                    reason = ($"pressure difference {pressureDiff:F3}  between TM and {target} exceed tolerance {maxPressureDiffOpenSlitValve}");
                    return false;
                }

                return true;
 
        }

    }


}
