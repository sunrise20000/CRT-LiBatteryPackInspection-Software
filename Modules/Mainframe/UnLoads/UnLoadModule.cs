using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Sorter.Common;
using Mainframe.TMs;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using System;
using Mainframe.UnLoads.Routines;

namespace Mainframe.UnLoads
{
    public class UnLoadModule : UnLoadModuleBase
    {
        public enum STATE
        {
            NotInstall,
            Init,
            Idle,
            Homing,
            Pump,
            Vent,
            MoveLift,
            Purge,
            LeakCheck,
            Transfer,
            Error,
            PrepareTransfer,
            OpenDoor,
            CloseDoor,
            Cooling,
            CoolingAndPurge,
            Separating,
            Grouping,
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            Transfer,
            PrepareTransfer,
            MoveLift,
            Pump,
            Vent,
            Purge,
            LeakCheck,
            OpenDoor,
            CloseDoor,
            Cooling,
            CoolingAndPurge,
            ToInit,
            Separate,
            Group,
        }


        public bool IsAlarm
        {
            get
            {
                return FsmState == (int)STATE.Error;
            }
        }

        public override bool IsReady
        {
            get { return FsmState == (int)STATE.Idle && CheckAllMessageProcessed(); }
        }

        public override bool IsError
        {
            get { return FsmState == (int)STATE.Error; }
        }

        public override bool IsInit
        {
            get { return FsmState == (int)STATE.Init; }
        }

        public bool IsBusy
        {
            get { return !IsInit && !IsError && !IsReady; }
        }

        public override bool IsIdle
        {
            get { return FsmState == (int)STATE.Idle && CheckAllMessageProcessed(); }
        }

        public int CurrentRoutineLoop
        {
            get
            {
                if (FsmState == (int)STATE.Purge)
                {
                    return _purgeRoutine.LoopCounter + 1; // CurrentLoop从0开始
                }
                else if (FsmState == (int)STATE.CoolingAndPurge)
                {
                    return _unloadCoolingAndPurgeRoutine.LoopCounter + 1; // CurrentLoop从0开始
                }
                else if (FsmState == (int)STATE.LeakCheck)
                {
                    return _unloadLeakCheckRoutine.LoopCounter + 1;
                }

                return 0;
            }
        }


        public int CurrentRoutineLoopTotal
        {
            get
            {
                if (FsmState == (int)STATE.Purge)
                {
                    return _purgeRoutine.LoopTotalTime;
                }
                else if (FsmState == (int)STATE.CoolingAndPurge)
                {
                    return _unloadCoolingAndPurgeRoutine.LoopTotalTime;
                }
                else if (FsmState == (int)STATE.LeakCheck)
                {
                    return _unloadLeakCheckRoutine.LoopTotalTime;
                }

                return 0;
            }
        }

        public event Action<string> OnEnterError;

        private bool _isInit;

        public SicUnLoad _unLoadDevice { get; set; }

        private UnLoadHomeRoutine _homeRoutine;
        private UnLoadPumpRoutine _pumpRoutine;
        private UnLoadVentRoutine _ventRoutine;
        private UnLoadPurgeRoutine _purgeRoutine;
        private UnLoadPrepareTransferRoutine _prepareTransferRoutine;
        //private UnLoadLiftRoutine _liftRoutine;
        private UnLoadCoolingRoutine _unloadCoolingRoutine;
        private UnLoadCoolingAndPurgeRoutine _unloadCoolingAndPurgeRoutine;
        private UnLoadSeparateRoutine _unloadSeparateRoutine;
        private UnLoadLeakCheckRoutine _unloadLeakCheckRoutine;

        public UnLoadModule(ModuleName module) : base(1)
        {
            Module = module.ToString();
            Name = module.ToString();
            IsOnline = false;

            EnumLoop<STATE>.ForEach((item) =>
            {
                MapState((int)item, item.ToString());
            });

            EnumLoop<MSG>.ForEach((item) =>
            {
                MapMessage((int)item, item.ToString());
            });

            EnableFsm(50, IsInstalled ? STATE.Init : STATE.NotInstall);
        }

        public override bool Initialize()
        {
            InitRoutine();

            InitDevice();

            InitFsm();

            InitOp();

            InitData();

            return base.Initialize();
        }

        private void InitRoutine()
        {
            ModuleName module = ModuleHelper.Converter(Module);

            _homeRoutine = new UnLoadHomeRoutine();
            _pumpRoutine = new UnLoadPumpRoutine();
            _ventRoutine = new UnLoadVentRoutine();
            _purgeRoutine = new UnLoadPurgeRoutine();
            _prepareTransferRoutine = new UnLoadPrepareTransferRoutine();
            _unloadSeparateRoutine = new UnLoadSeparateRoutine();
            _unloadCoolingRoutine = new UnLoadCoolingRoutine();
            _unloadCoolingAndPurgeRoutine = new UnLoadCoolingAndPurgeRoutine();
            _unloadLeakCheckRoutine = new UnLoadLeakCheckRoutine();
        }

        private void InitDevice()
        {
            _unLoadDevice = DEVICE.GetDevice<SicUnLoad>($"{Module}.{Module}");
        }

        private void InitFsm()
        {
            //Error
            AnyStateTransition(MSG.Error, FsmOnError, STATE.Error);
            AnyStateTransition(FSM_MSG.ALARM, FsmOnError, STATE.Error);
            Transition(STATE.Error, MSG.Reset, FsmReset, STATE.Idle);
            EnterExitTransition<STATE, FSM_MSG>(STATE.Error, FsmEnterError, FSM_MSG.NONE, FsmExitError);

            //Home
            Transition(STATE.Init, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Error, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Idle, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Homing, FSM_MSG.TIMER, FsmMonitorHomeTask, STATE.Idle);
            Transition(STATE.Homing, MSG.Error, null, STATE.Init);
            Transition(STATE.Homing, MSG.Abort, FsmAbortTask, STATE.Init);

            EnterExitTransition((int)STATE.Homing, FsmEnterIdle, (int)FSM_MSG.NONE, FsmExitIdle);

            AnyStateTransition(MSG.ToInit, FsmToInit, STATE.Init);

            //PrepareTransfer
            Transition(STATE.Idle, MSG.PrepareTransfer, FsmStartPrepareTransfer, STATE.PrepareTransfer);
            Transition(STATE.PrepareTransfer, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.PrepareTransfer, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Pump
            Transition(STATE.Idle, MSG.Pump, FsmStartPump, STATE.Pump);
            Transition(STATE.Pump, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Pump, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Vent
            Transition(STATE.Idle, MSG.Vent, FsmStartVent, STATE.Vent);
            Transition(STATE.Vent, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Vent, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Purge
            Transition(STATE.Idle, MSG.Purge, FsmStartPurge, STATE.Purge);
            Transition(STATE.Purge, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Purge, MSG.Abort, FsmAbortTask, STATE.Idle);

            //CoolingAndPurge
            Transition(STATE.Idle, MSG.CoolingAndPurge, FsmStartCoolingAndPurge, STATE.CoolingAndPurge);
            Transition(STATE.CoolingAndPurge, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.CoolingAndPurge, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Leak Check
            Transition(STATE.Idle, MSG.LeakCheck, FsmStartLeakCheck, STATE.LeakCheck);
            Transition(STATE.LeakCheck, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.LeakCheck, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Separate
            Transition(STATE.Idle, MSG.Separate, FsmStartSeparate, STATE.Separating);
            Transition(STATE.Separating, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Separating, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Cooling
            Transition(STATE.Idle, MSG.Cooling, FsmStartCooling, STATE.Cooling);
            Transition(STATE.Cooling, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Cooling, MSG.Abort, FsmAbortTask, STATE.Idle);



        }

        private void InitOp()
        {
            OP.Subscribe($"{Name}.Home", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Home);
            });

            OP.Subscribe($"{Name}.Pump", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Pump);
            });

            OP.Subscribe($"{Name}.Vent", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Vent);
            });
            OP.Subscribe($"{Name}.Purge", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Purge);
            });

            OP.Subscribe($"{Name}.LeakCheck", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.LeakCheck, args);
            });

            OP.Subscribe($"{Name}.MoveLift", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.MoveLift, args[0]);
            });

            OP.Subscribe($"{Name}.OpenDoor", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.OpenDoor);
            });

            OP.Subscribe($"{Name}.CloseDoor", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.CloseDoor);
            });

            OP.Subscribe($"{Name}.Reset", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Reset);
            });
            OP.Subscribe($"{Name}.Separate", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Separate);
            });

            OP.Subscribe($"{Name}.Abort", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Abort);
            });
        }

        private void InitData()
        {
            DATA.Subscribe($"{Name}.Status", () => StringFsmStatus);
            DATA.Subscribe($"{Name}.IsOnline", () => IsOnline);
            DATA.Subscribe($"{Name}.IsBusy", () => IsBusy);
            DATA.Subscribe($"{Module}.IsAlarm", () => IsAlarm);

            DATA.Subscribe($"{Name}.CurrentRoutineLoop", () => CurrentRoutineLoop, SubscriptionAttribute.FLAG.IgnoreSaveDB);
            DATA.Subscribe($"{Name}.CurrentRoutineLoopTotal", () => CurrentRoutineLoopTotal, SubscriptionAttribute.FLAG.IgnoreSaveDB);

            DATA.Subscribe($"{Name}.RemainedCoolingTime", () => _unloadCoolingAndPurgeRoutine.GetRemainedTime());


            /*
            DATA.Subscribe($"{Name}.AtATM", () =>
            {
                if (_unLoadDevice.CheckAtm())
                {
                    return true;
                }
                return false;

            });*/
            DATA.Subscribe($"{Name}.UnderVAC", () =>
            {
                if (_unLoadDevice.CheckVacuum())
                {
                    return true;
                }
                return false;

            });
            DATA.Subscribe($"{Name}.LidState", () =>
            {
                if (_unLoadDevice.CheckLidClose())
                {
                    return "Closed";
                }
                return "Open";

            });
        }

        /// <summary>
        /// Reset可能等待设备锁的Routine。
        /// </summary>
        private void ResetDeviceLockers()
        {
            // Reset可能等待设备锁的Routine。
            _pumpRoutine.ResetLocker();
            _purgeRoutine.ResetLocker();
            _ventRoutine.ResetLocker();
            _prepareTransferRoutine.ResetLocker();
            _unloadLeakCheckRoutine.ResetLocker();
            _unloadCoolingAndPurgeRoutine.ResetLocker();

        }

        private bool FsmOnError(object[] param)
        {
            IsOnline = false;

            if (FsmState == (int)STATE.Error)
            {
                return false;
            }

            if (FsmState == (int)STATE.Init)
                return false;

            return true;
        }

        private bool FsmReset(object[] param)
        {
            if (FsmState == (int)STATE.Error)
            {
                EV.ClearAlarmEvent();
            }
            if (!_isInit)
            {
                PostMsg(MSG.ToInit);
                return false;
            }

            ResetDeviceLockers();

            return true;
        }

        private bool FsmExitError(object[] param)
        {
            return true;
        }

        private bool FsmEnterError(object[] param)
        {
            if (OnEnterError != null)
                OnEnterError(Module);
            return true;
        }

        private bool FsmStartHome(object[] param)
        {
            ResetDeviceLockers();

            Result ret = StartRoutine(_homeRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmMonitorHomeTask(object[] param)
        {
            Result ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.Error);
                return false;
            }

            if (ret == Result.DONE)
            {
                _isInit = true;
                return true;
            }

            return false;
        }

        private bool FsmAbortTask(object[] param)
        {
            AbortRoutine();
            return true;
        }

        private bool FsmExitIdle(object[] param)
        {
            return true;
        }

        private bool FsmEnterIdle(object[] param)
        {
            return true;
        }

        private bool FsmToInit(object[] param)
        {
            return true;
        }

        private bool FsmStartPrepareTransfer(object[] param)
        {
            QueueRoutine.Clear();
            ModuleName mod = (ModuleName)param[0];

//             if ((mod == ModuleName.TMRobot && (EnumTransferType)param[2] == EnumTransferType.Place) 
//                 || mod == ModuleName.WaferRobot)
            {
                QueueRoutine.Enqueue(_prepareTransferRoutine);
            }

            //double tragetPressure = SC.GetConfigItem("TM.PressureBalance.BalancePressure").DoubleValue;
            //if (tragetPressure > 20)  //20mbar以上需要充到一定气压
            //{
            //    QueueRoutine.Enqueue(_prepareTransferRoutine);
            //}
            //else
            //{
            //    _pumpRoutine.Init();
            //    QueueRoutine.Enqueue(_pumpRoutine);
            //}
            if (mod == ModuleName.WaferRobot)
            {
                QueueRoutine.Enqueue(_ventRoutine);
            }

            Result ret = StartRoutine();
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmMonitorTask(object[] param)
        {
            Result ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.Error);
                return false;
            }

            return ret == Result.DONE;
        }

        private bool FsmStartSetOffline(object[] param)
        {
            IsOnline = false;
            return true;
        }

        private bool FsmStartSetOnline(object[] param)
        {
            IsOnline = true;
            return true;
        }

        private bool FsmStartPump(object[] param)
        {
            Result ret = StartRoutine(_pumpRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartVent(object[] param)
        {
            Result ret = StartRoutine(_ventRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartPurge(object[] param)
        {
            Result ret = StartRoutine(_purgeRoutine, param);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartSeparate(object[] param)
        {
            Result ret = StartRoutine(_unloadSeparateRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartCoolingAndPurge(object[] param)
        {
            Result ret = StartRoutine(_unloadCoolingAndPurgeRoutine, param);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartCooling(object[] param)
        {
            _unloadCoolingRoutine.Init((int)param[0]);

            Result ret = StartRoutine(_unloadCoolingRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }


        private bool FsmStartLeakCheck(object[] param)
        {
            if (param != null && param.Length >= 2)
            {
                _unloadLeakCheckRoutine.Init((int)param[0], (int)param[1]);
            }
            else
            {
                _unloadLeakCheckRoutine.Init();
            }

            Result ret = StartRoutine(_unloadLeakCheckRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;

        }




        public override bool Home(out string reason)
        {
            CheckToPostMessage((int)MSG.Home);
            reason = string.Empty;
            return true;
        }

        public override int InvokeCooling(int time)
        {
            if (CheckToPostMessage((int)MSG.Cooling, time))
                return (int)MSG.Cooling;

            return (int)FSM_MSG.NONE;
        }

        public override int InvokeCoolingAndPurge(int timeInSec, int purgeLoopCount, int purgePumpDelay)
        {
            if (CheckToPostMessage((int)MSG.CoolingAndPurge, timeInSec, purgeLoopCount, purgePumpDelay))
                return (int)MSG.CoolingAndPurge;

            return (int)FSM_MSG.NONE;
        }

        public override int InvokeVent()
        {
            if (CheckToPostMessage((int)MSG.Vent))
                return (int)MSG.Vent;

            return (int)FSM_MSG.NONE;
        }

        public override int InvokePump()
        {
            if (CheckToPostMessage((int)MSG.Pump))
                return (int)MSG.Pump;

            return (int)FSM_MSG.NONE;
        }

        public override int InvokePurge(params object[] objs)
        {
            if (CheckToPostMessage((int)MSG.Purge, objs))
                return (int)MSG.Purge;

            return (int)FSM_MSG.NONE;
        }

        public override bool CheckAcked(int entityTaskToken)
        {
            return FsmState == (int)STATE.Idle && CheckAllMessageProcessed();
            //return true;
        }

        public override void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType)
        {
            //CheckToPostMessage(MSG.InTransfer);
        }

        public override void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType)
        {
            //if (FsmState == (int)STATE.InTransfer)
            //    CheckToPostMessage(MSG.TransferComplete);
        }

        public override bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            reason = string.Empty;

            if (CheckToPostMessage((int)MSG.PrepareTransfer, robot, targetSlot, transferType))
                return true;

            return false;
        }

        public override bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType,
            out string reason)
        {
            reason = string.Empty;

            if (robot == ModuleName.WaferRobot || robot == ModuleName.EfemRobot)
            {
                return _unLoadDevice.CheckAtm();
            }
            else
            {
                return (_unLoadDevice.CheckVacuum() && !SC.GetValue<bool>("System.IsATMMode"))
                    || (_unLoadDevice.CheckAtm() && SC.GetValue<bool>("System.IsATMMode"));
            }
        }

        public override bool CheckReadyForMap(ModuleName robot, Hand blade, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override int InvokeGroupWaferTray()
        {
            if (CheckToPostMessage((int)MSG.Group))
                return (int)MSG.Group;

            return (int)FSM_MSG.NONE;
        }

        public override int InvokeSeparateWaferTray()
        {
            if (CheckToPostMessage((int)MSG.Separate))
                return (int)MSG.Separate;

            return (int)FSM_MSG.NONE;
        }

        public int InvokeError()
        {
            if (CheckToPostMessage((int)MSG.Error))
                return (int)MSG.Error;

            return (int)FSM_MSG.NONE;
        }

        public override bool CheckSlitValveClosed()
        {
            return _unLoadDevice.CheckSlitValveClosed();
        }
    }
}
