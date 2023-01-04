using System.Collections.Generic;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using SicRT.Modules.Schedulers;
using SicRT.Scheduler;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;

namespace SicRT.Modules
{
    public class ReturnAllWafer : SchedulerModuleFactory
    {
        //private bool needAligner = false; 
        Dictionary<ModuleName, int> _checkPosition = new Dictionary<ModuleName, int>()
        {
            {ModuleName.PM1, 1},
            {ModuleName.LoadLock, 1},
            {ModuleName.UnLoad, 1},
            {ModuleName.WaferRobot, 1},
            {ModuleName.TrayRobot, 1},
            {ModuleName.TMRobot, 1},
            {ModuleName.Aligner, 1},
        };

        private readonly R_TRIG _trigLoadUnLoadAllPumpAlarm = new R_TRIG();
        private readonly R_TRIG _trigWaferNoOriginWarn = new R_TRIG();
        

        public ReturnAllWafer()
        {
        }

        public Result Start()
        {
            if (!SchTmRobot.IsAvailable)
            {
                EV.PostWarningLog("Scheduler", "can not return wafer, TmRobot is not available!");
                return Result.FAIL;
            }
            if (!SchTrayRobot.IsAvailable)
            {
                EV.PostWarningLog("Scheduler", "can not return wafer, TrayRobot is not available!");
                return Result.FAIL;
            }
            if (!SchWaferRobot.IsAvailable)
            {
                EV.PostWarningLog("Scheduler", "can not return wafer, WaferRobot is not available!");
                return Result.FAIL;
            }
            if (!SchLoadLock.IsAvailable)
            {
                EV.PostWarningLog("Scheduler", "can not return wafer, Load is not available!");
                return Result.FAIL;
            }
            if (!SchUnLoad.IsAvailable)
            {
                EV.PostWarningLog("Scheduler", "can not return wafer, UnLoad is not available!");
                return Result.FAIL;
            }
            if (!SchBuffer.IsAvailable)
            {
                EV.PostWarningLog("Scheduler", "can not return wafer, Buffer is not available!");
                return Result.FAIL;
            }

            return Result.RUN;
        }

        public Result Monitor()
        {
            MonitorModuleTasks();

            if (MonitorTaskDone())
            {
                return Result.DONE;
            }


            return Result.RUN;
        }

        public Result MonitorModuleTasks()
        {
            //Load和UnLoad不能同时抽气
            //System.Diagnostics.Debug.Assert(!((_tmRobot.IsInPumping && SchLoadLock.IsInPumping) || (_tmRobot.IsInPumping && SchUnLoad.IsInPumping) || (SchUnLoad.IsInPumping && SchLoadLock.IsInPumping)), $"检测到 SchLoadLock.IsInPumping :{SchLoadLock.IsInPumping}, SchUnLoad.IsInPumping :{SchUnLoad.IsInPumping}, _tmRobot.IsInPumping :{_tmRobot.IsInPumping}");
            _trigLoadUnLoadAllPumpAlarm.CLK = (SchUnLoad.IsInPumping && SchLoadLock.IsInPumping);
            if (_trigLoadUnLoadAllPumpAlarm.Q)
                EV.PostAlarmLog("Scheduler", $"Detected that Load and UnLoad are pumping at the same time，Load Task: {SchLoadLock.GetTaskRunning()}, UnLoad Task: {SchUnLoad.GetTaskRunning()}");


            MonitorPMTask();
            MonitorLoadTask();
            MonitorUnLoadTask();
            MonitorBufferTask();
            MonitorAlignerTask();

            MonitorTmRobotTask();
            MonitorWaferRobotTask();
            MonitorTrayRobotTask();


            return Result.RUN;
        }

        private bool MonitorTaskDone()
        {
            foreach (var positions in _checkPosition)
            {
                for (int i = 0; i < positions.Value; i++)
                {
                    if (CheckWaferCouldBeReturn(positions.Key, i))
                    {
                        return false;
                    }
                }
            }

            if (SchBuffer.HasWafer(0) || SchBuffer.HasWafer(1) || SchBuffer.HasWafer(2))
                return false;
            if (SchBuffer.HasTrayAndExceedProcessCount(0) || SchBuffer.HasTrayAndExceedProcessCount(1) || SchBuffer.HasTrayAndExceedProcessCount(2))
                return false;

            if (!SchTmRobot.IsAvailable)
                return false;
            if (!SchWaferRobot.IsAvailable)
                return false;
            if (!SchTrayRobot.IsAvailable)
                return false;

            return true;
        }

        private void MonitorTmRobotTask()
        {
            if (!SchTmRobot.IsAvailable)
                return;

            foreach (var pm in LstPmSchedulers)
            {
                if (pm.IsWaitTransfer(ModuleName.TMRobot))
                    pm.StopWaitTransfer(ModuleName.TMRobot);
            }

            if (SchBuffer.IsWaitTransfer(ModuleName.TMRobot))
                SchBuffer.StopWaitTransfer(ModuleName.TMRobot);

            if (SchLoadLock.IsWaitTransfer(ModuleName.TMRobot))
                SchLoadLock.StopWaitTransfer(ModuleName.TMRobot);

            if (SchUnLoad.IsWaitTransfer(ModuleName.TMRobot))
                SchUnLoad.StopWaitTransfer(ModuleName.TMRobot);

            MonitorTmRobotPMTask();

            MonitorTmRobotBufferTask();

            MonitorTmRobotLoadTask();

            MonitorTmRobotUnLoadTask();
        }

        private void MonitorWaferRobotTask()
        {
            foreach (var cass in LstCassetteSchedulers)
            {
                if (cass.IsWaitTransfer(ModuleName.WaferRobot))
                    cass.StopWaitTransfer(ModuleName.WaferRobot);
            }

            if (SchAligner.IsWaitTransfer(ModuleName.WaferRobot))
                SchAligner.StopWaitTransfer(ModuleName.WaferRobot);

            if (SchLoadLock.IsWaitTransfer(ModuleName.WaferRobot))
                SchLoadLock.StopWaitTransfer(ModuleName.WaferRobot);

            if (SchUnLoad.IsWaitTransfer(ModuleName.WaferRobot))
                SchUnLoad.StopWaitTransfer(ModuleName.WaferRobot);

            MonitorWaferRobotCassetteTask();
            MonitorWaferRobotAligerTask();
            MonitorWaferRobotLoadTask();
            MonitorWaferRobotUnLoadTask();
        }

        private void MonitorTrayRobotTask()
        {
            if (SchCassBL.IsWaitTransfer(ModuleName.TrayRobot))
                SchCassBL.StopWaitTransfer(ModuleName.TrayRobot);

            if (SchLoadLock.IsWaitTransfer(ModuleName.TrayRobot))
                SchLoadLock.StopWaitTransfer(ModuleName.TrayRobot);

            MonitorTrayRobotLoadTask();

            MonitorTrayRobotCassetteTask();
        }


        private void MonitorTmRobotLoadTask()
        {
            if (!SchLoadLock.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            //place Robot有次数耗尽的Tray无Wafer,Load无Tray
            bool canPlace = SchTmRobot.HasTrayAndExceedProcessCount(0) && SchTmRobot.NoWafer(0) && SchLoadLock.NoTray(0);
            if (canPlace)
            {
                if (SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0))
                {
                    if (SchTmRobot.Place(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }
        }

        private void MonitorTmRobotUnLoadTask()
        {
            if (!SchUnLoad.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            //UnLoad取盘只关心LoadLock是否有空位

            //place Robot有Wafer,UnLoad无Tray无Wafer
            bool canPlaceUnLoad = SchTmRobot.HasWafer(0)
                                  && SchUnLoad.NoTray(0)
                                  && SchUnLoad.NoWafer(0);
            if (canPlaceUnLoad)
            {
                if (SchUnLoad.IsReadyForPlace(ModuleName.TMRobot, 0))
                {
                    if (SchTmRobot.Place(SchUnLoad.Module, 0, Hand.Blade1))
                    {
                        SchUnLoad.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }


            if (!SchUnLoad.IsAvailable)
                return;
            if (!SchTmRobot.IsAvailable)
                return;

            //pick UnLoad有Tray,TM无Tray,UnLoad分离完成
            bool canPickUnLoad = SchTmRobot.NoTray(0)
                                 && SchUnLoad.HasTray(0)
                                 && SchUnLoad.CheckWaferTraySeparated();
            if (canPickUnLoad)
            {
                if (SchUnLoad.IsReadyForPick(ModuleName.TMRobot, 0) && !SchLoadLock.IsInPumping)
                {
                    if (SchTmRobot.Pick(SchUnLoad.Module, 0, Hand.Blade1))
                    {
                        SchUnLoad.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }
            }
        }

        private void MonitorTmRobotBufferTask()
        {
            if (!SchTmRobot.IsAvailable)
                return;
            if (!SchBuffer.IsAvailable)
                return;

            //place 次数未耗尽的Tray放入Buffer中
            bool canPalce = SchTmRobot.NoWafer(0) && SchTmRobot.HasTrayAndNotExceedProcessCount(0);
            if (canPalce)
            {
                SlotItem bufferEmptySlot = null;
                for (int i = 0; i < 3; i++)
                {
                    if (SchBuffer.NoTray(i) && SchBuffer.NoWafer(i))
                    {
                        bufferEmptySlot = new SlotItem(ModuleName.Buffer, i);
                        break;
                    }
                }

                if (bufferEmptySlot == null)
                {
                    return;
                }

                if (SchBuffer.IsReadyForPlace(ModuleName.TMRobot, bufferEmptySlot.Slot))
                {
                    if (SchTmRobot.Place(SchBuffer.Module, bufferEmptySlot.Slot, Hand.Blade1))
                    {
                        SchBuffer.WaitTransfer(ModuleName.TMRobot);
                        return;
                    }
                }

            }


            if (!SchTmRobot.IsAvailable)
                return;
            if (!SchBuffer.IsAvailable)
                return;

            //pick 当前步(UnLoad没有Wafer),Buffer有Tray,机械手没有Tray
            bool canPick = SchTmRobot.NoTray(0) && SchUnLoad.NoTray(0) && SchUnLoad.NoWafer(0);
            if (canPick)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (SchBuffer.HasTray(i) && SchBuffer.HasWafer(i))
                    {
                        if (SchBuffer.IsReadyForPick(ModuleName.TMRobot, i))
                        {
                            if (SchTmRobot.Pick(SchBuffer.Module, i, Hand.Blade1))
                            {
                                SchBuffer.WaitTransfer(ModuleName.TMRobot);
                                return;
                            }
                        }
                    }

                    if (SchBuffer.HasTrayAndExceedProcessCount(i))
                    {
                        if (SchBuffer.IsReadyForPick(ModuleName.TMRobot, i))
                        {
                            if (SchTmRobot.Pick(SchBuffer.Module, i, Hand.Blade1))
                            {
                                SchBuffer.WaitTransfer(ModuleName.TMRobot);
                                return;
                            }
                        }
                    }

                }
            }

        }

        private void MonitorTmRobotPMTask()
        {
            if (!SchTmRobot.IsAvailable)
                return;

            //pick from pm
            if (SchTmRobot.NoWafer(0) && SchTmRobot.NoTray(0) && SchUnLoad.NoWafer(0) && SchUnLoad.NoTray(0))
            {
                Hand pickBlade = Hand.Blade1;

                ModuleName pickPm = ModuleName.System;
                foreach (var schedulerPm in LstPmSchedulers)
                {
                    //增加温度低于900才能Pick的限制
                    if (!schedulerPm.IsAvailable
                        || GetModule(schedulerPm.Module).NoWafer(0)
                        || SchTmRobot.HasWafer((int)pickBlade)
                        || !schedulerPm.CheckTempBelow900())
                        continue;

                    pickPm = schedulerPm.Module;
                    break;
                }

                if (pickPm != ModuleName.System)
                {
                    SchedulerModule pm = GetModule(pickPm.ToString());
                    if (pm.IsReadyForPick(ModuleName.TMRobot, 0))
                    {
                        if (SchTmRobot.Pick(pm.Module, 0, pickBlade))
                        {
                            pm.WaitTransfer(ModuleName.TMRobot);

                            return;
                        }
                    }
                }
            }
        }

        private void MonitorWaferRobotLoadTask()
        {
            if (!SchWaferRobot.IsAvailable)
                return;
            if (!SchLoadLock.IsAvailable)
                return;

            bool canPick = SchWaferRobot.NoWafer(0)
                            && SchLoadLock.HasWafer(0);
            //&& SchLoadLock.CheckWaferTraySeparated();

            if (canPick)
            {
                if (SchLoadLock.IsReadyForPick(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Pick(SchLoadLock.Module, 0, Hand.Blade1))
                    {
                        SchLoadLock.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }
        }

        private void MonitorWaferRobotUnLoadTask()
        {
            if (!SchWaferRobot.IsAvailable)
                return;
            if (!SchUnLoad.IsAvailable)
                return;

            //pick UnLoad有Wafer没有Tray,Robot没有Wafer,步骤都完成了或者下一步的模块没有片子  (其它:UnLoad 夹爪夹住,顶针下降,SlitValve都关了,大气)
            bool canPick = SchWaferRobot.NoWafer(0)
                           && SchUnLoad.HasWafer(0)
                           && SchUnLoad.NoTray(0);

            if (canPick)
            {
                if (SchUnLoad.IsReadyForPick(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Pick(SchUnLoad.Module, 0, Hand.Blade1))
                    {
                        SchUnLoad.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }
        }

        private void MonitorWaferRobotAligerTask()
        {
            if (SchLoadLock.HasWafer(0) && SchLoadLock.GetWaferInfo(0).NextStation != (int)ModuleName.Aligner)
                SchLoadLock.GetWaferInfo(0).NextStation = (int)ModuleName.Aligner;

            if (SchUnLoad.HasWafer(0) && SchUnLoad.GetWaferInfo(0).NextStation != (int)ModuleName.Aligner)
                SchUnLoad.GetWaferInfo(0).NextStation = (int)ModuleName.Aligner;

            if (SchWaferRobot.HasWafer(0) && SchWaferRobot.GetWaferInfo(0).NextStation != (int)ModuleName.Aligner && SchWaferRobot.GetWaferInfo(0).NextStation != (int)ModuleName.System)
                SchWaferRobot.GetWaferInfo(0).NextStation = (int)ModuleName.Aligner;

            if (!SchWaferRobot.IsAvailable)
                return;

            if (!SchAligner.IsAvailable)
                return;

            //place 任务还没完成,下一步是Aligner,Robot有片子,Aligner没片子
            bool canPlaceAligner = SchAligner.NoWafer(0)
                && SchWaferRobot.HasWafer(0)
                && SchWaferRobot.GetWaferInfo(0).NextStation == (int)ModuleName.Aligner;
            if (canPlaceAligner)
            {
                if (SchAligner.IsReadyForPlace(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Place(SchAligner.Module, 0, Hand.Blade1))
                    {
                        SchAligner.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }


            if (!SchWaferRobot.IsAvailable)
                return;

            if (!SchAligner.IsAvailable)
                return;

            //pick Robot没片子,Aligner有片子
            bool canPickAligner = SchAligner.HasWafer(0)
                && SchWaferRobot.NoWafer(0);

            if (canPickAligner)
            {
                if (SchAligner.IsReadyForPick(ModuleName.WaferRobot, 0))
                {
                    if (SchWaferRobot.Pick(SchAligner.Module, 0, Hand.Blade1))
                    {
                        SchAligner.WaitTransfer(ModuleName.WaferRobot);
                        return;
                    }
                }
            }
        }

        private void MonitorWaferRobotCassetteTask()
        {
            if (!SchWaferRobot.IsAvailable)
                return;

            //place
            bool canPlaceCassette = SchWaferRobot.HasWafer(0)&& SchWaferRobot.GetWaferInfo(0).NextStation!=(int)ModuleName.Aligner;
            if (canPlaceCassette)
            {
                WaferInfo wafer = SchWaferRobot.GetWaferInfo(0);

                _trigWaferNoOriginWarn.CLK = (wafer.OriginStation != (int)ModuleName.CassAL &&
                                              wafer.OriginStation != (int)ModuleName.CassAR);

                // Wafer来源不明，提示用户
                if (_trigWaferNoOriginWarn.Q)
                {
                    EV.PostWarningLog("ReturnAllWafer", 
                        "Unknown origin station of wafer on Wafer Robot");
                    return;
                }

                if (wafer.OriginStation == (int)ModuleName.CassAL)
                {
                    if (SchCassAL.IsReadyForPlace(ModuleName.WaferRobot, wafer.OriginSlot) &&
                        WaferManager.Instance.CheckNoWafer(ModuleName.CassAL, wafer.OriginSlot))
                    {
                        if (SchWaferRobot.Place(SchCassAL.Module, wafer.OriginSlot, Hand.Blade1))
                        {
                            SchCassAL.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }
                else if (wafer.OriginStation == (int)ModuleName.CassAR)
                {
                    if (SchCassAR.IsReadyForPlace(ModuleName.WaferRobot, wafer.OriginSlot) &&
                        WaferManager.Instance.CheckNoWafer(ModuleName.CassAR, wafer.OriginSlot))
                    {
                        if (SchWaferRobot.Place(SchCassAR.Module, wafer.OriginSlot, Hand.Blade1))
                        {
                            SchCassAR.WaitTransfer(ModuleName.WaferRobot);
                            return;
                        }
                    }
                }
            }
        }

        private void MonitorTrayRobotLoadTask()
        {
            if (!SchTrayRobot.IsAvailable)
                return;
            if (!SchLoadLock.IsAvailable)
                return;

            //先取Wafer再取石墨盘
            bool canPick = SchLoadLock.HasTray(0) && SchLoadLock.NoWafer(0);
            if (canPick)
            {
                SlotItem emptyTraySlot = GetTrayOrignSlot(ModuleName.LoadLock, 0);
                if (emptyTraySlot != null)
                {
                    if (SchLoadLock.IsReadyForPick(ModuleName.TrayRobot, 0))
                    {
                        if (SchTrayRobot.Pick(SchLoadLock.Module, 0, Hand.Blade1))
                        {
                            SchLoadLock.WaitTransfer(ModuleName.TrayRobot);
                            return;
                        }
                    }
                }
            }

        }

        private void MonitorTrayRobotCassetteTask()
        {
            if (!SchTrayRobot.IsAvailable)
                return;

            if (!SchCassBL.IsAvailable)
                return;

            //place Robot有盘 其它逻辑跟Load的Pick一致
            bool canPlace = SchTrayRobot.HasTray(0);
            if (canPlace)
            {
                SlotItem slotItem = GetTrayOrignSlot(ModuleName.TrayRobot, 0); //GetEmptyTraySlot(13);
                if (slotItem != null)
                {
                    if (SchCassBL.IsReadyForPlace(ModuleName.TrayRobot, slotItem.Slot))
                    {
                        if (SchTrayRobot.Place(slotItem.Module, slotItem.Slot, Hand.Blade1))
                        {
                            SchCassBL.WaitTransfer(ModuleName.TrayRobot);
                            return;
                        }
                    }
                }
            }

        }


        private void MonitorBufferTask()
        {
            if (!SchBuffer.IsAvailable)
            {
                return;
            }

            if (!SchBuffer.IsReadyForPick(ModuleName.TMRobot, 0))
            {
                SchBuffer.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
            }
        }

        private void MonitorAlignerTask()
        {
            if (!SchAligner.IsAvailable)
            {
                return;
            }

            if (SchAligner.HasWafer(0) && SchAligner.GetWaferInfo(0).NextStation==(int)ModuleName.Aligner)
            {
                SchAligner.Aligning();
                SchAligner.GetWaferInfo(0).NextStation = (int)ModuleName.System;
            }
        }

        private void MonitorPMTask()
        {
            foreach (var pm in LstPmSchedulers)
            {
                if (!pm.IsAvailable)
                    continue;
                if (pm.NoTray(0) && pm.NoWafer(0))
                    continue;

                if (!pm.IsReadyForPick(ModuleName.TMRobot, 0))
                {
                    pm.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Pick, 0);
                }
            }
        }

        private void MonitorLoadTask()
        {
            if (!SchLoadLock.IsAvailable)
            {
                return;
            }

            if (SchLoadLock.HasWafer(0))
            {
                if (/*SchLoadLock.CheckWaferTraySeparated() &&*/ !SchLoadLock.IsReadyForPick(ModuleName.WaferRobot, 0) && !SchUnLoad.IsInPumping && !SchTmRobot.IsInPumping)
                {
                    SchLoadLock.PrepareTransfer(ModuleName.WaferRobot, EnumTransferType.Pick, 0);
                    return;
                }
            }
            else if (SchLoadLock.HasTray(0))
            {
                if (/*SchLoadLock.CheckWaferTraySeparated() &&*/ !SchLoadLock.IsReadyForPick(ModuleName.TrayRobot, 0) && !SchUnLoad.IsInPumping && !SchTmRobot.IsInPumping)
                {
                    SchLoadLock.PrepareTransfer(ModuleName.TrayRobot, EnumTransferType.Pick, 0);
                    return;
                }
            }

            else if (SchTmRobot.HasTrayAndExceedProcessCount(0) && SchTmRobot.NoWafer(0))
            {
                if (!SchLoadLock.IsReadyForPlace(ModuleName.TMRobot, 0) && !SchUnLoad.IsInPumping && !SchTmRobot.IsInPumping)
                {
                    SchLoadLock.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
                    return;
                }
            }

        }

        private void MonitorUnLoadTask()
        {
            if (!SchUnLoad.IsAvailable)
            {
                return;
            }

            // 如果有Wafer有Tray，先检查是否分离
            if (SchUnLoad.HasWafer(0) && SchUnLoad.HasTray(0))
            {
                if (!SchUnLoad.CheckWaferTraySeparated())
                {
                    SchUnLoad.SeparateWaferTray();
                    return;
                }
            }

            
            if (SchUnLoad.HasTray(0)) // UnLoad有Tray，先让TMRobot取Tray
            {
                if (SchUnLoad.CheckWaferTraySeparated())
                {
                    // 如果Sequence下一步是UnLoad,并且W&T已冷却和分离
                    if (!SchUnLoad.CheckPurgedBeforeTrayPicking() 
                        && !SchLoadLock.IsInPumping)
                    {
                        var cycle = SC.GetValue<int>($"{ModuleName.UnLoad}.Purge.CyclePurgeCount");
                        var pumpDelay = SC.GetValue<int>($"{ModuleName.UnLoad}.Purge.PumpDelayTime");
                        SchUnLoad.PurgeBeforeTrayPicking(cycle, pumpDelay);
                        return;
                    }

                    if (SchUnLoad.CheckPurgedBeforeTrayPicking() 
                        && !SchUnLoad.IsReadyForPick(ModuleName.TMRobot, 0) 
                        && !SchLoadLock.IsInPumping)
                    {
                        // UnLoad中有Tray，则准备好让TMRobot来取
                        SchUnLoad.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Pick, 0);
                        return;
                    }
                }
            }
            else if (SchUnLoad.HasWafer(0)) // 如果没Tray，WaferRobot取Wafer
            {
                if (SchUnLoad.CheckWaferTraySeparated() )
                {
                    // 如果取Wafer前还未Purge，先Purge
                    if (!SchUnLoad.CheckPurgedBeforeWaferPicking() 
                        && !SchLoadLock.IsInPumping)
                    {
                        var cycle = SC.GetValue<int>($"{ModuleName.UnLoad}.Purge.CyclePurgeCount");
                        var pumpDelay = SC.GetValue<int>($"{ModuleName.UnLoad}.Purge.PumpDelayTime");
                        SchUnLoad.PurgeBeforeWaferPicking(cycle, pumpDelay);
                        return;
                    }

                    if (SchUnLoad.CheckPurgedBeforeWaferPicking() 
                        && !SchUnLoad.IsReadyForPick(ModuleName.WaferRobot, 0)
                        && !SchLoadLock.IsInPumping)
                    {
                        // 准备好让WaferRobot来取
                        SchUnLoad.PrepareTransfer(ModuleName.WaferRobot, EnumTransferType.Pick, 0);
                    }
                }

                return;
            }
            else // 如果UnLoad里啥也没有，则准备好下次TMRobot喂 W&T
            {
                // 如果Wafer取完后还没有Purge，先Purge
                if (!SchUnLoad.CheckPurgedAfterWaferPicked() && !SchLoadLock.IsInPumping)
                {
                    var cycle = SC.GetValue<int>($"{ModuleName.UnLoad}.Purge.CyclePurgeCount");
                    var pumpDelay = SC.GetValue<int>($"{ModuleName.UnLoad}.Purge.PumpDelayTime");
                    SchUnLoad.PurgeAfterWaferPicked(cycle, pumpDelay);
                }
                
                // 如果Wafer取完后还没有Purge，则终止后去操作。
                if (!SchUnLoad.CheckPurgedAfterWaferPicked())
                    return;
                
                if (!SchLoadLock.IsInPumping
                    && !SchUnLoad.IsReadyForPlace(ModuleName.TMRobot, 0)
                    && !SchLoadLock.IsInPumping)
                {
                    SchUnLoad.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place, 0);
                    return;
                }
            }
        }

        private SlotItem GetEmptyTraySlot()
        {
            for (int i = 0; i < WaferManager.Instance.GetWafers(ModuleName.CassBL).Length; i++)
            {
                if (WaferManager.Instance.CheckNoTray(ModuleName.CassBL, i))
                {
                    return new SlotItem(ModuleName.CassBL, i);
                }
            }
            return null;
        }

        private SlotItem GetTrayOrignSlot(ModuleName module, int slot)
        {
            WaferInfo wafer = GetModule(module).GetWaferInfo(slot);
            if (wafer != null && GetModule(module).HasTray(slot) && SchCassBL.NoTray(wafer.TrayOriginSlot))
            {
                return new SlotItem(ModuleName.CassBL, wafer.TrayOriginSlot);
            }
            return null;
        }

        private bool CheckWaferCouldBeReturn(ModuleName module, int slot)
        {
            WaferInfo wafer = WaferManager.Instance.GetWafer(module, slot);
            if (!wafer.IsEmpty)
            {
                return true;
            }
            else if (wafer.TrayState == WaferTrayStatus.Normal)
            {
                return true;
            }

            return false;
        }
    }

}
