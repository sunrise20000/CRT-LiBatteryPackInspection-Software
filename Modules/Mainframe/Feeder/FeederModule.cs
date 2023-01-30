using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.Utilities;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using System;
using Mainframe.Aligners.Routines;
using Mainframe.Feeder;

namespace Mainframe.Aligners
{
    public class FeederModule : FeederModuleBase
    {

        #region FSM State & Messages
       
        public enum STATE
        {
            NotInstall,
            Init,
            Idle,
            Homing,
            Error,
            Feeding
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            ToInit,
            Feed
        };

        #endregion

        #region Variables

        public event EventHandler<string> OnEnterError;

        private CrtDevFeeder _supportDevice;
        
        private bool _isInit;
        private FeederFeedRoutine _feederFeedRoutine;
        private FeederHomeRoutine _feederHomeRoutine;

        #endregion

        #region Constructors

        public FeederModule(ModuleName module) : base(1)
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

        #endregion


        #region Properties


        public bool IsAlarm => FsmState == (int)STATE.Error;

        public override bool IsReady => FsmState == (int)STATE.Idle && CheckAllMessageProcessed();

        public override bool IsError => FsmState == (int)STATE.Error;

        public override bool IsInit => FsmState == (int)STATE.Init;

        public bool IsBusy => !IsInit && !IsError && !IsReady;

        public override bool IsIdle => FsmState == (int)STATE.Idle && CheckAllMessageProcessed();

        #endregion
        

        

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
            var module = ModuleHelper.Converter(Module);

            _feederFeedRoutine = new FeederFeedRoutine();
            _feederHomeRoutine = new FeederHomeRoutine();

        }

        private void InitDevice()
        {
            _supportDevice = DEVICE.GetDevice<CrtDevFeeder>($"TM.HiWinAligner");
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
            
            // Feed
            Transition(STATE.Idle, MSG.Feed, FsmStartFeed, STATE.Feeding);
            Transition(STATE.Feeding, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Feeding, MSG.Abort, FsmAbortTask, STATE.Idle);

        }

        private void InitOp()
        {
            OP.Subscribe($"{Name}.Home", (cmd, args) => CheckToPostMessage((int)MSG.Home));
            OP.Subscribe($"{Name}.Reset", (cmd, args) => CheckToPostMessage((int)MSG.Reset));
            OP.Subscribe($"{Name}.Abort", (cmd, args) => CheckToPostMessage((int)MSG.Abort));

            // Action Feed
            OP.Subscribe($"{Name}.Feed", (cmd, args) => CheckToPostMessage((int)MSG.Feed));
        }

        private void InitData()
        {
            DATA.Subscribe($"{Name}.Status", () => StringFsmStatus);
            DATA.Subscribe($"{Name}.IsOnline", () => IsOnline);
            DATA.Subscribe($"{Name}.IsBusy", () => IsBusy);
            DATA.Subscribe($"{Module}.IsAlarm", () => IsAlarm);
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
                _supportDevice.Reset();
                EV.ClearAlarmEvent();
            }
            
            if (!_isInit)
            {
                PostMsg(MSG.ToInit);
                return false;
            }

            return true;
        }

        private bool FsmExitError(object[] param)
        {
            return true;
        }

        private bool FsmEnterError(object[] param)
        {
            OnEnterError?.Invoke(this, Module);
            return true;
        }

        private bool FsmStartHome(object[] param)
        {
            var ret = StartRoutine(_feederHomeRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmMonitorHomeTask(object[] param)
        {
            var ret = MonitorRoutine();
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

        
        private bool FsmStartFeed(object[] param)
        {
            var ret = StartRoutine(_feederFeedRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
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


        private bool FsmMonitorTask(object[] param)
        {
            var ret = MonitorRoutine();
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.Error);
                return false;
            }

            return ret == Result.DONE;
        }

        public override bool Home(out string reason)
        {
            CheckToPostMessage((int)MSG.Home);
            reason = string.Empty;
            return true;
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
            return true;
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
            return true;
        }

        public override bool CheckReadyForMap(ModuleName robot, Hand blade, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override int InvokeFeed()
        {
            if (CheckToPostMessage((int)MSG.Feed))
                return (int)MSG.Feed;

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
