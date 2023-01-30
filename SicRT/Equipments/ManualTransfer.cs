using System.Collections.Generic;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using Mainframe.Aligners.Routines;
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
                        _moveTaskQueue.Enqueue(new FeederFeedRoutine());
                        if (target != ModuleName.Aligner)
                            _moveTaskQueue.Enqueue(new EfemRobotMover(new MoveItemEx(ModuleName.Aligner, 0, target, ds, 0)));
                    }
                    else
                    {
                        _moveTaskQueue.Enqueue(new FeederFeedRoutine()); 
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

            return Result.RUN;
        }
        public Result Start(params object[] objs)
        {
            return this.Start();
        }

        public Result Monitor()
        {
            

            return Result.RUN;
        }
        public void Abort()
        {
            Clear();
        }

        public void Clear()
        {
          
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
        public TmRobotMover(MoveItemEx moveTask)
        {
           
        }

        public Result Start()
        {
          
            return Result.RUN;
        }
        public Result Start(params object[] objs)
        {
            return this.Start();
        }

        public Result Monitor()
        {
          
            return Result.RUN;
        }
        public void Abort()
        {
            Clear();
        }

        public void Clear()
        {
           
        }
    }
}
