using System.Collections.Generic;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using Mainframe.Aligners;
using Mainframe.Aligners.Routines;
using Mainframe.Buffers;
using Mainframe.TMs;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using SicRT.Modules.Schedulers;
namespace SicRT.Equipments
{
    public class MoveItemEx : MoveItem
    {
        public int CoolingTime { get; set; }

        public MoveItemEx(ModuleName sourceModule, int sourceSlot, ModuleName destinationModule, int destinationSlot, int coolingTime)
            : base(sourceModule, sourceSlot, destinationModule, destinationSlot, Hand.Blade1)
        {
            CoolingTime = coolingTime;
        }
    }

    public class ManualTransfer : SchedulerModuleFactory
    {
        private Queue<IRoutine> _moveTaskQueue = new Queue<IRoutine>();
        private IRoutine _activeTask;

        public Result Start(object[] objs)
        {
            _moveTaskQueue.Clear();

            ModuleName source = (ModuleName)objs[0];
            int ss = (int)objs[1];
            ModuleName target = (ModuleName)objs[2];
            int ds = (int)objs[3];

            bool autoCooling = false;
            bool autoAlign = SC.GetValue<bool>("System.AutoAlignManualTransfer");
            int coolingTime = 0;
            if (objs.Length >= 8)
            {
                autoCooling = (bool)objs[6];
                coolingTime = (int)objs[7];
            }

            //判断是否可以传盘
            if (!CheckCanMoveWafer(source, target))
            {
                EV.PostWarningLog("System", $"Invalid path,can not transfer from {source} to {target}");
                return Result.DONE;
            }
            //TM范围内传盘
            if (IsTmRobotRange(source) && IsTmRobotRange(target))
            {
                if (!WaferManager.Instance.CheckHasTray(source, ss))
                {
                    EV.PostWarningLog("System", "source no tray");
                    return Result.FAIL;
                }
                if (!WaferManager.Instance.CheckNoTray(target, ds))
                {
                    EV.PostWarningLog("System", "destination has tray");
                    return Result.FAIL;
                }

                _moveTaskQueue.Enqueue(new TmRobotMover(new MoveItemEx(source, ss, target, ds, 0)));
            }
            else if (IsTrayRobotRange(source) && IsTrayRobotRange(target))
            {
                if (!WaferManager.Instance.CheckHasTray(source, ss))
                {
                    EV.PostWarningLog("System", "source no tray");
                    return Result.FAIL;
                }
                if (!WaferManager.Instance.CheckNoTray(target, ds))
                {
                    EV.PostWarningLog("System", "destination has tray");
                    return Result.FAIL;
                }

                _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(source, ss, target, ds, 0)));
            }
            else if (IsWaferRobotRange(source) && IsWaferRobotRange(target))
            {
                if (!WaferManager.Instance.CheckHasWafer(source, ss))
                {
                    EV.PostWarningLog("System", "source no wafer");
                    return Result.FAIL;
                }
                if (!WaferManager.Instance.CheckNoWafer(target, ds))
                {
                    EV.PostWarningLog("System", "destination has wafer");
                    return Result.FAIL;
                }
              

                if (autoAlign)
                {
                    if ((target == ModuleName.WaferRobot || target == ModuleName.EfemRobot) && source != ModuleName.Aligner)
                    {
                        _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(source, ss, target, ds, 0)));
                    }
                    else if (source == ModuleName.WaferRobot || source == ModuleName.EfemRobot)
                    {
                        _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(source, ss, target, ds, 0)));
                    }
                    else if (source != ModuleName.Aligner)
                    {
                        _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(source, ss, ModuleName.Aligner, 0, 0)));
                        _moveTaskQueue.Enqueue(new AlignerAlignRoutine());
                        if (target != ModuleName.Aligner)
                            _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(ModuleName.Aligner, 0, target, ds, 0)));
                    }
                    else
                    {
                        _moveTaskQueue.Enqueue(new AlignerAlignRoutine()); 
                        if (target != ModuleName.Aligner)
                            _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(ModuleName.Aligner, 0, target, ds, 0)));
                    }
                }
                else
                {
                    _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(source, ss, target, ds, 0)));
                }
            }
            _activeTask = _moveTaskQueue.Dequeue();

            return _activeTask.Start();
        }


        public Result Monitor(object[] objs)
        {
            //System.Diagnostics.Debug.Assert(_activeTask != null, "mover should not be null, call start first");

            Result ret = _activeTask.Monitor();
            if (ret == Result.FAIL)
                return ret;

            if (ret == Result.DONE)
            {
                if (_moveTaskQueue.Count > 0)
                {
                    _activeTask = _moveTaskQueue.Dequeue();
                    return _activeTask.Start();
                }

                return Result.DONE;
            }

            return Result.RUN;
        }

        public void Clear()
        {
            _moveTaskQueue.Clear();
            _activeTask = null;
        }


        private bool IsWaferRobotRange(ModuleName module)
        {
            return module == ModuleName.CassAL || module == ModuleName.CassAR || module == ModuleName.LoadLock || module == ModuleName.Aligner || module == ModuleName.UnLoad || module == ModuleName.WaferRobot;
        }

        private bool IsTrayRobotRange(ModuleName module)
        {
            return module == ModuleName.CassBL || module == ModuleName.LoadLock || module == ModuleName.TrayRobot;
        }

        private bool IsTmRobotRange(ModuleName module)
        {
            return module == ModuleName.LoadLock || ModuleHelper.IsPm(module) || module == ModuleName.UnLoad || module == ModuleName.Buffer || module == ModuleName.TMRobot;
        }

        private bool CheckCanMoveWafer(ModuleName fromModule, ModuleName desModule)
        {
            if (IsTmRobotRange(fromModule) && IsTmRobotRange(desModule))
            {
                return true;
            }
            if (IsTrayRobotRange(fromModule) && IsTrayRobotRange(desModule))
            {
                return true;
            }
            if (IsWaferRobotRange(fromModule) && IsWaferRobotRange(desModule)) 
            {
                return true;
            }

            return false;
        }
    }


    public class EfemRobotMover : SchedulerModuleFactory, IRoutine
    {
        private MoveItemEx _moveTask;

        private SchedulerModule _source;
        private SchedulerModule _destination;
        private bool IsWaferRobot = true;

        public EfemRobotMover(MoveItemEx moveTask)
        {
            _moveTask = moveTask;
        }

        public Result Start()
        {
            _source = GetModule(_moveTask.SourceModule.ToString());
            _destination = GetModule(_moveTask.DestinationModule.ToString());

            System.Diagnostics.Debug.Assert(_source != null, $"{_moveTask.SourceModule} not valid");
            System.Diagnostics.Debug.Assert(_destination != null, $"{_moveTask.DestinationModule} not valid");
            if (_moveTask.SourceModule == ModuleName.LoadLock || _moveTask.SourceModule == ModuleName.CassBL || _moveTask.SourceModule == ModuleName.TrayRobot)
            {
                if (_moveTask.DestinationModule == ModuleName.LoadLock || _moveTask.DestinationModule == ModuleName.CassBL || _moveTask.DestinationModule == ModuleName.TrayRobot)
                {
                    IsWaferRobot = false;
                }
            }

            _source.ResetTask();
            _destination.ResetTask();

            if (IsWaferRobot)
            {
                SchWaferRobot.ResetTask();
            }
            else
            {
                SchTrayRobot.ResetTask();
            }

            //如果是TrayRobot向LoadLock中手动传盘，不检查是否有Wafer，应该是有盘
            
            if(IsTrayRobotRange(_moveTask.SourceModule)&& IsTrayRobotRange(_moveTask.DestinationModule))
            {
                if (!WaferManager.Instance.CheckHasTray(_moveTask.SourceModule, _moveTask.SourceSlot))
                {
                    EV.PostWarningLog("System", $"Failed transfer, source {_moveTask.SourceModule} slot {_moveTask.SourceSlot + 1} not has tray");
                    return Result.FAIL;
                }

                if (WaferManager.Instance.CheckHasTray(_moveTask.DestinationModule, _moveTask.DestinationSlot))
                {
                    EV.PostWarningLog("System", $"Failed transfer, destination {_moveTask.DestinationModule} slot {_moveTask.DestinationSlot + 1} has tray");
                    return Result.FAIL;
                }
            }
            else if (IsWaferRobotRange(_moveTask.SourceModule) && IsWaferRobotRange(_moveTask.DestinationModule))
            {
                if (!WaferManager.Instance.CheckHasWafer(_moveTask.SourceModule, _moveTask.SourceSlot))
                {
                    EV.PostWarningLog("System", $"Failed transfer, source {_moveTask.SourceModule} slot {_moveTask.SourceSlot + 1}  not has wafer");
                    return Result.FAIL;
                }

                if (WaferManager.Instance.CheckHasWafer(_moveTask.DestinationModule, _moveTask.DestinationSlot))
                {
                    EV.PostWarningLog("System", $"Failed transfer, destination {_moveTask.DestinationModule} slot {_moveTask.DestinationSlot + 1} has wafer");
                    return Result.FAIL;
                }
            }
            

            if (IsWaferRobot)
            {
                if (!SchWaferRobot.IsOnline)
                {
                    EV.PostWarningLog("System", $"Failed transfer, WaferRobot is not online");
                    return Result.FAIL;
                }
            }
            else
            {
                if (!SchTrayRobot.IsOnline)
                {
                    EV.PostWarningLog("System", $"Failed transfer, TrayRobot is not online");
                    return Result.FAIL;
                }
            }
            if (!_source.IsAvailable)
            {
                EV.PostWarningLog("System", $"Failed transfer, source {_moveTask.SourceModule} not ready");
                return Result.FAIL;
            }
            if (!_destination.IsAvailable)
            {
                EV.PostWarningLog("System", $"Failed transfer, Destination {_moveTask.DestinationModule} not ready");
                return Result.FAIL;
            }

            _moveTask.RobotHand = Hand.Blade1;

            return Result.RUN;
        }
        public Result Start(params object[] objs)
        {
            return this.Start();
        }

        public Result Monitor()
        {
            if (IsWaferRobot)
            {
                if (_source.NoWafer(_moveTask.SourceSlot) && _destination.HasWafer(_moveTask.DestinationSlot) &&
                SchWaferRobot.IsAvailable)
                {
                    if (_source.IsWaitTransfer(ModuleName.WaferRobot))
                        _source.StopWaitTransfer(ModuleName.WaferRobot);

                    if (_destination.IsWaitTransfer(ModuleName.WaferRobot))
                        _destination.StopWaitTransfer(ModuleName.WaferRobot);

                    return Result.DONE;
                }

                //Pick
                if (_moveTask.SourceModule != ModuleName.WaferRobot)
                {
                    if (_source.HasWafer(_moveTask.SourceSlot))
                    {
                        if (!_source.IsAvailable)
                        {
                            return Result.RUN;
                        }

                        if (!_source.IsReadyForPick(ModuleName.WaferRobot, _moveTask.SourceSlot))
                        {
                            if (!_source.PrepareTransfer(ModuleName.WaferRobot, EnumTransferType.Pick, _moveTask.SourceSlot))
                                return Result.FAIL;
                        }

                        if (!_source.IsAvailable)
                        {
                            return Result.RUN;
                        }

                        if (!SchWaferRobot.HasWafer((int)_moveTask.RobotHand))
                        {
                            if (!SchWaferRobot.Pick(_moveTask.SourceModule, _moveTask.SourceSlot, _moveTask.RobotHand))
                            {
                                return Result.FAIL;
                            }

                            _source.WaitTransfer(ModuleName.WaferRobot);
                        }

                        if (!SchWaferRobot.IsAvailable)
                            return Result.RUN;
                    }
                    else
                    {
                        if (!SchWaferRobot.IsAvailable)
                            return Result.RUN;

                        if (_source.IsWaitTransfer(ModuleName.WaferRobot))
                            _source.StopWaitTransfer(ModuleName.WaferRobot);
                    }
                }

                //Place
                if (_moveTask.DestinationModule != ModuleName.WaferRobot)
                {
                    if (!_destination.IsAvailable)
                        return Result.RUN;

                    if (_destination.NoWafer(_moveTask.DestinationSlot))
                    {
                        if (!_destination.IsReadyForPlace(ModuleName.WaferRobot, _moveTask.DestinationSlot))
                        {
                            if (!_destination.PrepareTransfer(ModuleName.WaferRobot, EnumTransferType.Place,
                                _moveTask.DestinationSlot))
                                return Result.FAIL;
                        }

                        if (!_destination.IsAvailable)
                            return Result.RUN;

                        if (SchWaferRobot.HasWafer((int)_moveTask.RobotHand))
                        {
                            if (!SchWaferRobot.Place(_moveTask.DestinationModule, _moveTask.DestinationSlot, _moveTask.RobotHand))
                                return Result.FAIL;

                            _destination.WaitTransfer(ModuleName.WaferRobot);
                        }

                        if (!SchWaferRobot.IsAvailable)
                            return Result.RUN;
                    }
                    else
                    {
                        if (!SchWaferRobot.IsAvailable)
                            return Result.RUN;

                        if (_destination.IsWaitTransfer(ModuleName.WaferRobot))
                            _destination.StopWaitTransfer(ModuleName.WaferRobot);
                    }
                }

            }
            else
            {
                if (_source.NoTray(_moveTask.SourceSlot) && _destination.HasTray(_moveTask.DestinationSlot) &&
                SchTrayRobot.IsAvailable)
                {
                    if (_source.IsWaitTransfer(ModuleName.TrayRobot))
                        _source.StopWaitTransfer(ModuleName.TrayRobot);

                    if (_destination.IsWaitTransfer(ModuleName.TrayRobot))
                        _destination.StopWaitTransfer(ModuleName.TrayRobot);

                    return Result.DONE;
                }

                //Pick
                if (_moveTask.SourceModule != ModuleName.TrayRobot)
                {
                    if (_source.HasTray(_moveTask.SourceSlot))
                    {
                        if (!_source.IsAvailable)
                        {
                            return Result.RUN;
                        }

                        if (!_source.IsReadyForPick(ModuleName.TrayRobot, _moveTask.SourceSlot))
                        {
                            if (!_source.PrepareTransfer(ModuleName.TrayRobot, EnumTransferType.Pick, _moveTask.SourceSlot))
                                return Result.FAIL;
                        }

                        if (!_source.IsAvailable)
                        {
                            return Result.RUN;
                        }

                        if (!SchTrayRobot.HasTray((int)_moveTask.RobotHand))
                        {
                            if (!SchTrayRobot.Pick(_moveTask.SourceModule, _moveTask.SourceSlot, _moveTask.RobotHand))
                            {
                                return Result.FAIL;
                            }

                            _source.WaitTransfer(ModuleName.TrayRobot);
                        }

                        if (!SchTrayRobot.IsAvailable)
                            return Result.RUN;
                    }
                    else
                    {
                        if (!SchTrayRobot.IsAvailable)
                            return Result.RUN;

                        if (_source.IsWaitTransfer(ModuleName.TrayRobot))
                            _source.StopWaitTransfer(ModuleName.TrayRobot);
                    }
                }

                //Place
                if (_moveTask.DestinationModule != ModuleName.TrayRobot)
                {
                    if (!_destination.IsAvailable)
                        return Result.RUN;

                    if (_destination.NoTray(_moveTask.DestinationSlot))
                    {
                        if (!_destination.IsReadyForPlace(ModuleName.TrayRobot, _moveTask.DestinationSlot))
                        {
                            if (!_destination.PrepareTransfer(ModuleName.TrayRobot, EnumTransferType.Place,
                                _moveTask.DestinationSlot))
                                return Result.FAIL;
                        }

                        if (!_destination.IsAvailable)
                            return Result.RUN;

                        if (SchTrayRobot.HasTray((int)_moveTask.RobotHand))
                        {
                            if (!SchTrayRobot.Place(_moveTask.DestinationModule, _moveTask.DestinationSlot, _moveTask.RobotHand))
                                return Result.FAIL;

                            _destination.WaitTransfer(ModuleName.TrayRobot);
                        }

                        if (!SchTrayRobot.IsAvailable)
                            return Result.RUN;
                    }
                    else
                    {
                        if (!SchTrayRobot.IsAvailable)
                            return Result.RUN;

                        if (_destination.IsWaitTransfer(ModuleName.TrayRobot))
                            _destination.StopWaitTransfer(ModuleName.TrayRobot);
                    }
                }
            }

            return Result.RUN;
        }
        public void Abort()
        {
            Clear();
        }

        public void Clear()
        {
            SchWaferRobot.ResetTask();
            _source?.ResetTask();
            _destination?.ResetTask();
        }

        private bool IsWaferRobotRange(ModuleName module)
        {
            return module == ModuleName.CassAL || module == ModuleName.CassAR || module == ModuleName.LoadLock || module == ModuleName.Aligner || module == ModuleName.UnLoad || module == ModuleName.WaferRobot;
        }

        private bool IsTrayRobotRange(ModuleName module)
        {
            return module == ModuleName.CassBL || module == ModuleName.LoadLock || module == ModuleName.TrayRobot;
        }

        private bool IsTmRobotRange(ModuleName module)
        {
            return module == ModuleName.LoadLock || ModuleHelper.IsPm(module) || module == ModuleName.UnLoad || module == ModuleName.Buffer || module == ModuleName.TMRobot;
        }

    }



    public class TmRobotMover : SchedulerModuleFactory, IRoutine
    {
        private MoveItemEx _moveTask;

        private SchedulerModule _source;
        private SchedulerModule _destination;

        public TmRobotMover(MoveItemEx moveTask)
        {
            _moveTask = moveTask;
        }

        public Result Start()
        {
            _source = GetModule(_moveTask.SourceModule.ToString());
            _destination = GetModule(_moveTask.DestinationModule.ToString());

            System.Diagnostics.Debug.Assert(_source != null, $"{_moveTask.SourceModule} not valid");
            System.Diagnostics.Debug.Assert(_destination != null, $"{_moveTask.DestinationModule} not valid");

            _source.ResetTask();
            _destination.ResetTask();
            SchTmRobot.ResetTask();

            if (!WaferManager.Instance.CheckHasTray(_moveTask.SourceModule, _moveTask.SourceSlot))
            {
                EV.PostWarningLog("System", $"Failed transfer, source {_moveTask.SourceModule} slot {_moveTask.SourceSlot + 1} has not tray");
                return Result.FAIL;
            }

            if (WaferManager.Instance.CheckHasTray(_moveTask.DestinationModule, _moveTask.DestinationSlot))
            {
                EV.PostWarningLog("System", $"Failed transfer, destination {_moveTask.DestinationModule} slot {_moveTask.DestinationSlot + 1} has tray");
                return Result.FAIL;
            }
            if (!SchTmRobot.IsOnline)
            {
                EV.PostWarningLog("System", $"Failed transfer, TM is not online");
                return Result.FAIL;
            }
            if (!_source.IsAvailable)
            {
                EV.PostWarningLog("System", $"Failed transfer, source {_moveTask.SourceModule} not ready");
                return Result.FAIL;
            }
            if (!_destination.IsAvailable)
            {
                EV.PostWarningLog("System", $"Failed transfer, Destination {_moveTask.DestinationModule} not ready");
                return Result.FAIL;
            }

            if (_moveTask.SourceModule == ModuleName.TMRobot)
                _moveTask.RobotHand = (Hand)_moveTask.SourceSlot;
            if (_moveTask.DestinationModule == ModuleName.TMRobot)
                _moveTask.RobotHand = (Hand)_moveTask.DestinationSlot;

            return Result.RUN;
        }
        public Result Start(params object[] objs)
        {
            return this.Start();
        }

        public Result Monitor()
        {
            if (_source.NoTray(_moveTask.SourceSlot) && _destination.HasTray(_moveTask.DestinationSlot) &&
                SchTmRobot.IsAvailable)
            {
                if (_source.IsWaitTransfer(ModuleName.TMRobot))
                    _source.StopWaitTransfer(ModuleName.TMRobot);

                if (_destination.IsWaitTransfer(ModuleName.TMRobot))
                    _destination.StopWaitTransfer(ModuleName.TMRobot);

                return Result.DONE;
            }

            //Pick
            if (_moveTask.SourceModule != ModuleName.TMRobot)
            {
                if (_source.HasTray(_moveTask.SourceSlot))
                {
                    if (!_source.IsAvailable)
                    {
                        return Result.RUN;
                    }

                    if (!_source.IsReadyForPick(ModuleName.TMRobot, _moveTask.SourceSlot))
                    {
                        if (!_source.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Pick, _moveTask.SourceSlot))
                            return Result.FAIL;
                    }

                    if (!_source.IsAvailable)
                    {
                        return Result.RUN;
                    }

                    if (!SchTmRobot.HasTray((int)_moveTask.RobotHand))
                    {
                        if (!SchTmRobot.Pick(_moveTask.SourceModule, _moveTask.SourceSlot, _moveTask.RobotHand))
                        {
                            return Result.FAIL;
                        }

                        _source.WaitTransfer(ModuleName.TMRobot);
                    }

                    if (!SchTmRobot.IsAvailable)
                        return Result.RUN;
                }
                else
                {
                    if (!SchTmRobot.IsAvailable)
                        return Result.RUN;

                    if (_source.IsWaitTransfer(ModuleName.TMRobot))
                        _source.StopWaitTransfer(ModuleName.TMRobot);
                }
            }

            //Place
            if (_moveTask.DestinationModule != ModuleName.TMRobot)
            {
                if (!_destination.IsAvailable)
                    return Result.RUN;

                if (_destination.NoTray(_moveTask.DestinationSlot))
                {
                    if (!_destination.IsReadyForPlace(ModuleName.TMRobot, _moveTask.DestinationSlot))
                    {
                        if (!_destination.PrepareTransfer(ModuleName.TMRobot, EnumTransferType.Place,
                            _moveTask.DestinationSlot))
                            return Result.FAIL;
                    }

                    if (!_destination.IsAvailable)
                        return Result.RUN;

                    if (SchTmRobot.HasTray((int)_moveTask.RobotHand))
                    {
                        if (!SchTmRobot.Place(_moveTask.DestinationModule, _moveTask.DestinationSlot, _moveTask.RobotHand))
                            return Result.FAIL;

                        _destination.WaitTransfer(ModuleName.TMRobot);
                    }

                    if (!SchTmRobot.IsAvailable)
                        return Result.RUN;
                }
                else
                {
                    if (!SchTmRobot.IsAvailable)
                        return Result.RUN;

                    if (_destination.IsWaitTransfer(ModuleName.TMRobot))
                        _destination.StopWaitTransfer(ModuleName.TMRobot);
                }
            }

            return Result.RUN;
        }
        public void Abort()
        {
            Clear();
        }

        public void Clear()
        {
            SchTmRobot.ResetTask();
            _source?.ResetTask();
            _destination?.ResetTask();
        }
    }
}
