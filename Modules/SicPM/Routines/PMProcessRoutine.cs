using System;
using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs;
using SicPM.Devices;

namespace SicPM.Routines
{
    public class PMProcessRoutine : ModuleRoutine, IRoutine
    {
        enum RoutineStep
        {  
            Process,
            NoteInProcess,
            WaitProcess,
            NoteProcessComplete,
            NoteProcessFailed,
        }

        private int _timeout;
 
        private PM _pm;
        private IoInterLock _pmIoInterLock;

        private string _paramRecipeName;
        private bool _paramIsCleanRecipe;
        private bool _paramWithWafer;
        private string _recipeContent;

  //      private Guid _guidProcess;

        public PMProcessRoutine(ModuleName module )
        {
            Module = module.ToString();  
            Name   = "Process";
 
            _pm = DEVICE.GetDevice<PM>(Module);
            _pmIoInterLock = DEVICE.GetDevice<IoInterLock>($"{Module}.PMInterLock");
        }

        public Result Init(string recipeName, bool isClean, bool withWafer)
        {
            _paramRecipeName = recipeName;
            _paramIsCleanRecipe = isClean;
            _paramWithWafer = withWafer;

            return Result.DONE;
        }

        public Result Start(params object[] objs)
        {
            Reset();

            if (_paramWithWafer && !WaferManager.Instance.CheckHasWafer(_pm.Module, 0))
            {
                EV.PostWarningLog(Module, $"can not run process, No wafer at {Module}");
                return Result.FAIL;
            }

            if (!_paramWithWafer && WaferManager.Instance.CheckHasWafer(_pm.Module, 0))
            {
                EV.PostWarningLog(Module, $"can not run waferless process, Has wafer at {Module}");
                return Result.FAIL;
            }

            if (string.IsNullOrEmpty(_paramRecipeName))
            {
                EV.PostWarningLog(Module, $"can not run process, recipe name is empty");
                return Result.FAIL;
            }

            _recipeContent = RecipeFileManager.Instance.LoadRecipe(Module, _paramRecipeName, false);
            if (string.IsNullOrEmpty(_recipeContent))
            {
                EV.PostWarningLog(Module, $"can not run process, load recipe {_paramRecipeName} failed");
                return Result.FAIL;
            }

            //_guidProcess = Guid.NewGuid();

            if (!_pmIoInterLock.SetPMProcessRunning(true, out string reason))
            {
                EV.PostAlarmLog(Module, $"can not Purge,{reason}");
                return Result.FAIL;
            }


            _timeout = SC.GetValue<int>("PM.ProcessTimeout");
            Notify("Start");
            return Result.RUN;
        }


        public Result Monitor()
        {
            try
            {
                Process((int)RoutineStep.Process, _pm, _paramRecipeName, _recipeContent, _timeout);

                NoteInProcess((int) RoutineStep.NoteInProcess, _pm.Module, 0);

                WaitProcess((int)RoutineStep.WaitProcess, _pm,_timeout);

                NoteProcessComplete((int)RoutineStep.NoteProcessComplete, _pm.Module, 0);
            }
            catch (RoutineBreakException)
            {
                return Result.RUN;
            }
            catch (RoutineFaildException)
            {
                NoteProcessFailed((int) RoutineStep.NoteProcessFailed, _pm.Module, 0);

                return Result.FAIL;
            }

            Notify("Finished");
            SetRoutineRuningDo();
            return Result.DONE;
        }

        public void Abort()
        {
            SetRoutineRuningDo();
        }

        private void SetRoutineRuningDo()
        {
            _pmIoInterLock.DoProcessRunning = false;
        }


        protected void NoteInProcess(int id, string module, int slot)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update process information, process in {module} start");

                ModuleName mod = ModuleHelper.Converter(module);

                WaferManager.Instance.UpdateWaferProcessStatus(mod, slot, EnumWaferProcessStatus.InProcess);
                WaferManager.Instance.GetWafer(mod, slot).TrayProcessCount--;
 
//                _pm.OnProcessStart(_guidProcess.ToString(), _paramRecipeName);

                return true;
            });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }

        protected void NoteProcessFailed(int id, string module, int slot)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update process information, process in {module} failed");

                ModuleName mod = ModuleHelper.Converter(module);

                WaferManager.Instance.UpdateWaferProcessStatus(mod, slot, EnumWaferProcessStatus.Failed);

 //               _pm.OnProcessEnd(_guidProcess.ToString(), "Failed");

                return true;
            });

            //if (ret.Item1)
            //{
            //    throw (new RoutineBreakException());
            //}
        }

        protected void NoteProcessComplete(int id, string module, int slot)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                Notify($"Update process information, process in {module} completed");

                ModuleName mod = ModuleHelper.Converter(module);

                WaferManager.Instance.UpdateWaferProcessStatus(mod, slot, EnumWaferProcessStatus.Completed);

                //_pm.OnProcessEnd(_guidProcess.ToString(), "Completed");

                return true;
            });

            if (ret.Item1)
            {
                throw (new RoutineBreakException());
            }
        }
        public void Process(int id, PM pm , string recipeName, string recipeContent,  int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
                {
                    Notify($"Run {pm.Name} recipe {recipeName}");

                    pm.StartProcess(recipeName, recipeContent, _paramIsCleanRecipe);

                    return true;
                }, () => {
                    if (pm.IsError)
                    {
                        return null;
                    }

                    return true;

                }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"{pm.Name} error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} run process timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

        public void WaitProcess(int id, PM pm, int timeout)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, () =>
                {
                    return true;
                }, () =>
                {
                    if (pm.IsError)
                    {
                        return null;
                    }

                    return pm.IsIdle;

                }, timeout * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    Stop($"{pm.Name} error");
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    Stop($"{pm.Name} run process timeout, over {timeout} seconds");
                    throw (new RoutineFaildException());
                }
                else
                    throw (new RoutineBreakException());
            }
        }

    }
}
