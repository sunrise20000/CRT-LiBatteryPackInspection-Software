using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mainframe.LLs.Routines;

namespace Mainframe.LLs
{
    public class LoadLockModule : LoadLockModuleBase
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
            Separating,
            Grouping,
            RotationHomeOffset,
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
            ToInit,
            Separate,
            Group,
            RelativeHomeOffset,
        }


        private R_TRIG _trigAlarm = new R_TRIG();
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
                    return _purgeRoutine.LoopCounter + 1;
                }
                else if (FsmState == (int)STATE.LeakCheck)
                {
                    return _leakCheckRoutine.LoopCounter + 1;
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
                else if (FsmState == (int)STATE.LeakCheck)
                {
                    return _leakCheckRoutine.LoopTotalTime;
                }


                return 0;
            }
        }

        public event Action<string> OnEnterError;

        private bool _isInit;

        public SicLoadLock _llDevice { get; set; }

        private LoadHomeRoutine _homeRoutine;
        private LoadLockPumpRoutine _pumpRoutine;
        private LoadLockVentRoutine _ventRoutine;
        private LoadLockPurgeRoutine _purgeRoutine;
        private LoadLockLeakCheckRoutine _leakCheckRoutine;
        private LoadLockServoToRoutine _llServoTo;
        private LoadLockPrepareTransferRoutine _prepareTransferRoutine;
        //private LoadLockLiftRoutine _loadLockLiftRoutine;
        private LoadLockGroupRoutine _loadLockGroupRoutine;
        private LoadSeparateRoutine _loadSeparateRoutine;

        private LoadRotationHomeOffsetRoutine _loadRotationHomeOffsetRoutine;


        public LoadLockModule(ModuleName module) : base(1)
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

            _pumpRoutine = new LoadLockPumpRoutine(module);
            _homeRoutine = new LoadHomeRoutine(module);
            _ventRoutine = new LoadLockVentRoutine(module);
            _purgeRoutine = new LoadLockPurgeRoutine(module);
            _leakCheckRoutine = new LoadLockLeakCheckRoutine(module);
            _llServoTo = new LoadLockServoToRoutine(module);
            _prepareTransferRoutine = new LoadLockPrepareTransferRoutine(module);
            _loadLockGroupRoutine = new LoadLockGroupRoutine(module);
            _loadSeparateRoutine = new LoadSeparateRoutine(module);
            _loadRotationHomeOffsetRoutine = new LoadRotationHomeOffsetRoutine();
        }

        private void InitDevice()
        {
            _llDevice = DEVICE.GetDevice<SicLoadLock>($"{Module}.{Module}");
        }

        private void InitFsm()
        {
            //Error
            AnyStateTransition(MSG.Error, FsmOnError, STATE.Error);
            AnyStateTransition(FSM_MSG.ALARM, FsmOnError, STATE.Error);
            AnyStateTransition(MSG.ToInit, FsmToInit, STATE.Init);
            //EnterExitTransition<STATE, FSM_MSG>(STATE.Error, FsmEnterError, FSM_MSG.NONE, FsmExitError);

            //Home
            Transition(STATE.Init, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Error, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Idle, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Homing, FSM_MSG.TIMER, FsmMonitorHomeTask, STATE.Idle);
            Transition(STATE.Homing, MSG.Error, null, STATE.Init);
            Transition(STATE.Homing, MSG.Abort, FsmAbortTask, STATE.Init);
            EnterExitTransition((int)STATE.Homing, FsmEnterIdle, (int)FSM_MSG.NONE, FsmExitIdle);

            Transition(STATE.Error, MSG.Reset, FsmReset, STATE.Idle);


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

            //Leak Check
            Transition(STATE.Idle, MSG.LeakCheck, FsmStartLeakCheck, STATE.LeakCheck);
            Transition(STATE.LeakCheck, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.LeakCheck, MSG.Abort, FsmAbortTask, STATE.Idle);


            //Separate
            Transition(STATE.Idle, MSG.Separate, FsmStartSeparate, STATE.Separating);
            Transition(STATE.Separating, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Separating, MSG.Abort, FsmAbortTask, STATE.Idle);

            //Group
            Transition(STATE.Idle, MSG.Group, FsmStartGroup, STATE.Grouping);
            Transition(STATE.Grouping, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Grouping, MSG.Abort, FsmAbortTask, STATE.Idle);

            //LoadRotationHome
            Transition(STATE.Idle, MSG.RelativeHomeOffset, FsmStartRelativeHomeOffset, STATE.RotationHomeOffset);
            Transition(STATE.RotationHomeOffset, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.RotationHomeOffset, MSG.Abort, FsmAbortTask, STATE.Idle);
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
            OP.Subscribe($"{Name}.Group", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Group);
            });

            OP.Subscribe($"{Name}.Separate", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Separate);
            });

            OP.Subscribe($"{Name}.RotationRelativeHomeOffset", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RelativeHomeOffset);
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
            DATA.Subscribe($"{Name}.CurrentRoutineLoop", () => CurrentRoutineLoop, SubscriptionAttribute.FLAG.IgnoreSaveDB);
            DATA.Subscribe($"{Name}.CurrentRoutineLoopTotal", () => CurrentRoutineLoopTotal, SubscriptionAttribute.FLAG.IgnoreSaveDB);
            DATA.Subscribe($"{Module}.IsAlarm", () => IsAlarm);

            /*DATA.Subscribe($"{Name}.LeakCheckElapseTime", () =>
            {
                if (FsmState == (int)STATE.LeakCheck)
                    return _leakCheckRoutine.ElapsedTime;
                return 0;
            });*/

            /*DATA.Subscribe($"{Name}.AtATM", () =>
            {
                if (_llDevice.CheckAtm())
                {
                    return true;
                }
                return false;

            });*/
            DATA.Subscribe($"{Name}.UnderVAC", () =>
            {
                if (_llDevice.CheckVacuum())
                {
                    return true;
                }
                return false;

            });
            DATA.Subscribe($"{Name}.LidState", () =>
            {
                if (_llDevice.CheckLidClose())
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
            _ventRoutine.ResetLocker();
            _purgeRoutine.ResetLocker();
            _leakCheckRoutine.ResetLocker();
            _prepareTransferRoutine.ResetLocker();
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

            if (mod == ModuleName.TrayRobot || mod == ModuleName.WaferRobot)
            {
                //if ((EnumTransferType)param[2] == EnumTransferType.Place)
                {
                    QueueRoutine.Enqueue(_prepareTransferRoutine);
                }
            }
            if (mod == ModuleName.TMRobot)
            {
                double tragetPressure = SC.GetConfigItem("TM.PressureBalance.BalancePressure").DoubleValue;
                if (tragetPressure > 20)  //20mbar以上需要充到一定气压
                {
                    QueueRoutine.Enqueue(_prepareTransferRoutine);
                }
                else
                {
                    _pumpRoutine.Init();
                    QueueRoutine.Enqueue(_pumpRoutine);
                }
            }
            else
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
            //Load腔切成Online时需检查Rotation状态是否异常
            if (!_llDevice.CheckRotationState())
            {
                EV.PostAlarmLog(Module, "Load Rotation State abnormal,Please Check the state!");
                return false;
            }

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

        private bool FsmStartLeakCheck(object[] param)
        {
            if (param != null && param.Length >= 2)
            {
                _leakCheckRoutine.Init((int)param[0], (int)param[1]);
            }
            else
            {
                _leakCheckRoutine.Init();
            }

            Result ret = StartRoutine(_leakCheckRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        //private bool FsmStartSeparate(object[] param)
        //{
        //    Result ret = StartRoutine(_loadLockSeparateRoutine);
        //    if (ret == Result.FAIL || ret == Result.DONE)
        //        return false;
        //    return ret == Result.RUN;
        //}
        private bool FsmStartRelativeHomeOffset(object[] param)
        {
            Result ret = StartRoutine(_loadRotationHomeOffsetRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartGroup(object[] param)
        {
            Result ret = StartRoutine(_loadLockGroupRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartSeparate(object[] param)
        {
            Result ret = StartRoutine(_loadSeparateRoutine);
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
            if (CheckToPostMessage((int)MSG.Purge,objs))
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

            if (robot == ModuleName.TrayRobot || robot == ModuleName.WaferRobot || robot == ModuleName.EfemRobot)
            {
                return _llDevice.CheckAtm();
            }
            else
            {
                return (_llDevice.CheckVacuum() && !SC.GetValue<bool>("System.IsATMMode")) 
                    || (_llDevice.CheckAtm() && SC.GetValue<bool>("System.IsATMMode"));
            }

        }

        public override bool CheckReadyForMap(ModuleName robot, Hand blade, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool CheckSlitValveClosed()
        {
            return _llDevice.CheckSlitValveClosed();
        }

        public override bool CheckLiftUp()
        {
            return _llDevice.CheckLiftUp();
        }

        public override bool CheckLiftDown()
        {
            return _llDevice.CheckLiftDown();
        }

        public override bool CheckTrayClamped()
        {
            throw new NotImplementedException();
        }

        public override bool CheckTrayUnClamped()
        {
            throw new NotImplementedException();
        }

        public override bool CheckWaferClamped()
        {
            return _llDevice.CheckWaferClamped();
        }

        public override bool CheckWaferUnClamped()
        {
            throw new NotImplementedException();
        }
        public override bool CheckWaferPlaced()
        {
            throw new NotImplementedException();
        }
        public override bool CheckTrayPlaced()
        {
            throw new NotImplementedException();
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
    }
}
