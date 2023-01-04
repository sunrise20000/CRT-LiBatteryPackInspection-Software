using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Scheduler;

namespace SicRT.Modules.Schedulers
{
    public class SchedulerModule
    {
        protected enum TaskType
        {
            None,

            PrepareTransfer,

            Pick,

            Place,

            PickAndPlace,

            Preprocess,
            Process,
            PostProcess,

            OpenCover,
            CloseCover,

            Load,
            Unload,

            Align,

            TransferTarget,

            Cooling,
            CoolingAndPurge,
            WarmUp,

            Vent,
            Pump,
            Purge,
            Group,
            Separate,
            Map
        }

        /// <summary>
        /// 访问变量时先锁定，避免多线程竞争问题。
        /// </summary>
        protected readonly object SyncRoot = new object();

        public ModuleName Module
        {
            get { return ModuleHelper.Converter(_module); }
        }
        public virtual bool IsAvailable { get; }
        public virtual bool IsOnline { get; }
        public virtual bool IsError { get; }
        public virtual bool IsReady { get; }

        public virtual bool IsService { get; }
        
        private R_TRIG[] firstDetectWaferArriveTrigs = new R_TRIG[25];
        private R_TRIG[] firstDetectWaferLeaveTrigs = new R_TRIG[25];
        private R_TRIG[] firstDetectTrayArriveTrigs = new R_TRIG[25];
        private R_TRIG[] firstDetectTrayLeaveTrigs = new R_TRIG[25];

        protected string _module;

        protected TaskType _task = TaskType.None;

        protected ModuleName _inProcessRobot;

        public SchedulerModule(string module)
        {
            _module = module;
            for (int i = 0; i < firstDetectWaferArriveTrigs.Length; i++)
            {
                firstDetectWaferArriveTrigs[i] = new R_TRIG();
            }
            for (int i = 0; i < firstDetectWaferLeaveTrigs.Length; i++)
            {
                firstDetectWaferLeaveTrigs[i] = new R_TRIG();
            }
            for (int i = 0; i < firstDetectTrayArriveTrigs.Length; i++)
            {
                firstDetectTrayArriveTrigs[i] = new R_TRIG();
            }
            for (int i = 0; i < firstDetectTrayLeaveTrigs.Length; i++)
            {
                firstDetectTrayLeaveTrigs[i] = new R_TRIG();
            }
        }

        /// <summary>
        /// 检查Module的任务是否完成。
        /// <para>完成的条件除关键动作完成，例如Wafer到位以外，还需要判断Module的状态是否为Idle或Error。</para>
        /// <para>1. 如果关键动作完成，但Module还未Idle，则说明上个Routine还未执行完毕，则认为Task还未完成。</para>
        /// <para>2. 如果关键动作未完成，但Module已经Idle或Error，则说明Routine已执行完毕，但可能出错，则认为Task完成。</para>
        /// </summary>
        /// <param name="keyActionDone"></param>
        /// <param name="fsmStopped"></param>
        /// <returns></returns>
        protected bool SuperCheckTaskDone(bool keyActionDone, bool fsmStopped)
        {
            //TODO 需进一步抽象，将Module.IsIdle和Module.IsError的判断写在基类。
            
            keyActionDone &= fsmStopped;
            
            if (keyActionDone)
            {
                if (_task != TaskType.None)
                {
                    LogTaskDone(_task, "");
                    _task = TaskType.None;
                }

                return true;
            }

            if (fsmStopped)
            {
                LogTaskDone(_task, "Task Failed");
                _task = TaskType.None;
                return true;
            }

            return false;
        }

        protected void LogTaskStart(TaskType cmd, string message)
        {
            EV.PostInfoLog("Scheduler", $"Task start:{_module},{cmd} {message}");

        }
        protected void LogTaskDone(TaskType cmd, string message)
        {
            EV.PostInfoLog("Scheduler", $"Task done:{_module},{cmd} {message}");
        }
        public void ResetTask()
        {
            lock (SyncRoot)
            {
                _task = TaskType.None;
            }
        }

        public bool WaitTransfer(ModuleName robot)
        {
            lock (SyncRoot)
            {
                _task = TaskType.TransferTarget;
                _inProcessRobot = robot;
                LogTaskStart(_task, $"Note {robot} in transfer");
                return true;
            }
        }

        public bool IsWaitTransfer(ModuleName robot)
        {
            lock (SyncRoot)
            {
                return _task == TaskType.TransferTarget && _inProcessRobot == robot;
            }
        }

        public bool StopWaitTransfer(ModuleName robot)
        {
            lock (SyncRoot)
            {
                LogTaskDone(_task, $"Note {robot} transfer complete");
                _inProcessRobot = ModuleName.System;

                _task = TaskType.None;
                return true;
            }
        }

        public WaferInfo GetWaferInfo(int slot)
        {
            lock (SyncRoot)
            {
                return WaferManager.Instance.GetWafer(ModuleHelper.Converter(_module), slot);
            }
        }

        public bool CheckWaferStatus(int slot, WaferStatus waferStatus)
        {
            lock (SyncRoot)
            {
                return WaferManager.Instance.CheckWafer(ModuleHelper.Converter(_module), slot, waferStatus);
            }
        }

        public bool HasWafer(int slot)
        {
            lock (SyncRoot)
            {
                return WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(_module), slot);
            }
        }

        public bool NoWafer(int slot)
        {
            lock (SyncRoot)
            {
                return WaferManager.Instance.CheckNoWafer(ModuleHelper.Converter(_module), slot);
            }
        }

        public bool HasTray(int slot)
        {
            lock (SyncRoot)
            {
                return WaferManager.Instance.CheckHasTray(ModuleHelper.Converter(_module), slot);
            }
        }

        public bool NoTray(int slot)
        {
            lock (SyncRoot)
            {
                return WaferManager.Instance.CheckNoTray(ModuleHelper.Converter(_module), slot);
            }
        }

        public bool FirstDetectWaferArrive(int slot)
        {
                firstDetectWaferArriveTrigs[slot].CLK = HasWafer(slot);
                return firstDetectWaferArriveTrigs[slot].Q;
        }

        public bool FirstDetectWaferLeave(int slot)
        {
                firstDetectWaferLeaveTrigs[slot].CLK = NoWafer(slot);
                return firstDetectWaferLeaveTrigs[slot].Q;
        }

        public bool FirstDetectTrayArrive(int slot)
        {
                firstDetectTrayArriveTrigs[slot].CLK = HasTray(slot);
                return firstDetectTrayArriveTrigs[slot].Q;
        }

        public bool FirstDetectTrayLeave(int slot)
        {
            firstDetectTrayLeaveTrigs[slot].CLK = NoTray(slot);
            return firstDetectTrayLeaveTrigs[slot].Q;
        }

        public bool HasTrayAndExceedProcessCount(int slot)
        {
            lock (SyncRoot)
            {
                WaferInfo wi = WaferManager.Instance.GetWafer(ModuleHelper.Converter(_module), slot);
                if (wi == null)
                    return false;

                return wi.TrayState == WaferTrayStatus.Normal && wi.TrayProcessCount <= 0;
            }
        }

        public bool HasTrayAndNotExceedProcessCount(int slot)
        {
            lock (SyncRoot)
            {
                WaferInfo wi = WaferManager.Instance.GetWafer(ModuleHelper.Converter(_module), slot);
                if (wi == null)
                    return false;

                return wi.TrayState == WaferTrayStatus.Normal && wi.TrayProcessCount > 0;
            }
        }

        public virtual bool CheckWaferNextStepIsThis(ModuleName module, int slot)
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(module, slot))
                    return false;

                WaferInfo wafer = WaferManager.Instance.GetWafer(module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return false;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return false;

                if (!wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules.Contains(Module))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// 判断下一步的模块是否有Wafer
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual bool CheckWaferNextStepModuleNoWafer(int slot)
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(Module, slot))
                    return false;

                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return false;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return false;

                List<ModuleName> lstModuleName = wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules;
                if (lstModuleName.Count > 0)
                {
                    for (int i = 0; i < lstModuleName.Count; i++)
                    {
                        if (WaferManager.Instance.CheckNoWafer(lstModuleName[i], 0))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public virtual bool CheckWaferNextStepModuleNoTray(int slot)
        {
            lock (SyncRoot)
            {
                if (!WaferManager.Instance.CheckHasWafer(Module, slot))
                    return false;

                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                    return false;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count)
                    return false;

                List<ModuleName> lstModuleName = wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules;
                if (lstModuleName.Count > 0)
                {
                    for (int j = 0; j < lstModuleName.Count; j++)
                    {
                        if (lstModuleName[j] == ModuleName.Buffer)
                        {
                            string strSots = wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep]
                                .StepParameter["SlotSelection"].ToString();
                            if (string.IsNullOrEmpty(strSots))
                            {
                                return false;
                            }

                            string[] bufferSlots = strSots.Split(',');
                            for (int k = 0; k < bufferSlots.Length; k++)
                            {
                                if (Int32.TryParse(bufferSlots[k], out int bufferSlot))
                                {
                                    if (WaferManager.Instance.CheckNoTray(ModuleName.Buffer, bufferSlot - 1))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                        else if (WaferManager.Instance.CheckNoTray(lstModuleName[j], 0))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }


        /// <summary>
        /// 判断Wafer的步骤是否都完成了
        /// </summary>
        /// <param name="waferModule"></param>
        /// <param name="waferSlot"></param>
        /// <returns></returns>
        public virtual bool CheckWaferSequenceStepDone(int slot)
        {
            lock (SyncRoot)
            {
                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, slot);

                if (wafer.IsEmpty)
                {
                    return false;
                }

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null)
                {
                    return true;
                }

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count &&
                    wafer.ProcessJob.Sequence.Steps.Count > 0)
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool CheckWaferNeedProcess(int waferSlot, ModuleName processIn = ModuleName.System)
        {
            lock (SyncRoot)
            {
                WaferInfo wafer = WaferManager.Instance.GetWafer(Module, waferSlot);

                if (wafer.IsEmpty)
                    return false;

                if (wafer.Status == WaferStatus.Dummy && wafer.ProcessState == EnumWaferProcessStatus.Wait)
                {
                    if (ModuleHelper.IsPm(processIn))
                    {
                        return (CheckNeedRunClean(out bool withWafer, out _) && withWafer);
                    }

                    return true;
                }

                if (wafer.ProcessJob == null || wafer.ProcessJob.Sequence == null ||
                    wafer.ProcessJob.Sequence.Steps == null)
                    return false;

                if (wafer.NextSequenceStep >= wafer.ProcessJob.Sequence.Steps.Count ||
                    wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep].StepModules == null)
                    return false;

                if (processIn != ModuleName.System && !wafer.ProcessJob.Sequence.Steps[wafer.NextSequenceStep]
                        .StepModules.Contains(processIn))
                    return false;

                bool hasPm = false;
                for (int i = wafer.NextSequenceStep; i < wafer.ProcessJob.Sequence.Steps.Count; i++)
                {
                    foreach (var stepModule in wafer.ProcessJob.Sequence.Steps[i].StepModules)
                    {
                        if (ModuleHelper.IsPm(stepModule))
                        {
                            hasPm = true;
                            break;
                        }
                    }

                    if (hasPm)
                        break;
                }

                if (processIn == ModuleName.System && !hasPm)
                    return false;

                return true;
            }
        }

        public virtual bool CheckNeedRunClean(out bool withWafer, out string recipeName)
        {
            lock (SyncRoot)
            {
                recipeName = string.Empty;
                withWafer = false;

                //SequenceInfo seq = GetCurrentSequenceInfo();
                //int waferProcessed = StatsDataManager.Instance.GetValue($"{Module}.WaferProcessedSincePreviousClean");

                //if (seq == null)
                //    return false;


                //foreach (var stepInfo in seq.Steps)
                //{
                //    if (!stepInfo.StepModules.Contains(Module))
                //        continue;

                //    if (stepInfo.StepParameter.ContainsKey("CleanIntervalWaferless")
                //        && int.TryParse((string)stepInfo.StepParameter["CleanIntervalWaferless"], out int interval)
                //        && stepInfo.StepParameter.ContainsKey("CleanRecipeWaferless")
                //        && !string.IsNullOrEmpty((string)stepInfo.StepParameter["CleanRecipeWaferless"]))
                //    {
                //        if (interval > 0 && waferProcessed >= interval)
                //        {
                //            recipeName = (string)stepInfo.StepParameter["CleanRecipeWaferless"];
                //            withWafer = false;
                //            return true;
                //        }
                //    }

                //    if (stepInfo.StepParameter.ContainsKey("CleanIntervalWafer")
                //        && int.TryParse((string)stepInfo.StepParameter["CleanIntervalWafer"], out interval)
                //        && stepInfo.StepParameter.ContainsKey("CleanRecipeWafer")
                //        && !string.IsNullOrEmpty((string)stepInfo.StepParameter["CleanRecipeWafer"]))
                //    {
                //        if (interval > 0 && waferProcessed >= interval)
                //        {
                //            recipeName = (string)stepInfo.StepParameter["CleanRecipeWafer"];
                //            withWafer = true;
                //            return true;
                //        }
                //    }
                //}

                return false;
            }
        }

        public virtual bool IsReadyForPick(ModuleName robot, int slot)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool IsReadyForPlace(ModuleName robot, int slot)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool PrepareTransfer(ModuleName robot, EnumTransferType type, int slot)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool PostTransfer(ModuleName robot, EnumTransferType type, int slot)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool Process(string recipeName, bool isCleanRecipe, bool withDummyWafer)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool Cooling(bool coolingType, int coolingTime)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool CoolingAndPurge(bool coolingType, int coolingTime, int purgeLoopCount, int purgePumpDelay)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool Aligning()
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool WarmUp(int warmUpTime)
        {
            lock (SyncRoot)
            {
                return true;
            }
        }


        public virtual bool CheckWaferTraySeparated()
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool CheckWaferTrayGrouped()
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

        public virtual bool CheckSlitValveClosed() //检查闸板阀是否关闭
        {
            lock (SyncRoot)
            {
                return true;
            }
        }

    }

}
