using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.Utilities;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using System;
using Mainframe.EFEMs.Routines;

namespace Mainframe.EFEMs
{
    public class EFEMModule : EFEMModuleBase
    {
        public enum STATE
        {
            NotInstall,
            Init,
            Idle,
            Homing,
            Cooling,
            Error,

            OpenSlitValve,
            CloseSlitValve,
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            ToInit,

            OpenSlitValve,
            CloseSlitValve,
        };


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

        public bool IsAlarm
        {
            get
            {
                return FsmState == (int)STATE.Error;
            }
        }

        public event Action<string> OnEnterError;
        
        private EFEMHomeRoutine _robotHomeRoutine;
        private EfemSlitValveRoutine _slitValveRoutine;

        private bool _isInit;

        public SicEFEM _cassetteDevice { get; set; }

        public EFEMModule(ModuleName module) : base(1)
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
            _robotHomeRoutine = new EFEMHomeRoutine();
            _slitValveRoutine = new EfemSlitValveRoutine();

        }

        private void InitDevice()
        {
            _cassetteDevice = DEVICE.GetDevice<SicEFEM>($"{Module}.{Module}");
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
            
            //open SlitValve
            Transition(STATE.Idle, MSG.OpenSlitValve, FsmStartOpenSlitValve, STATE.OpenSlitValve);
            Transition(STATE.OpenSlitValve, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.OpenSlitValve, MSG.Abort, FsmAbortTask, STATE.Idle);

            //close SlitValve
            Transition(STATE.Idle, MSG.CloseSlitValve, FsmStartCloseSlitValve, STATE.CloseSlitValve);
            Transition(STATE.CloseSlitValve, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.CloseSlitValve, MSG.Abort, FsmAbortTask, STATE.Idle);



        }

        private void InitOp()
        {
            OP.Subscribe($"{Name}.Home", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Home));

            OP.Subscribe($"{Name}.Reset", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Reset));
            OP.Subscribe($"{Name}.Abort", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Abort));
            
            OP.Subscribe($"{Module}.OpenSlitValve", (string cmd, object[] args) =>{return CheckToPostMessage((int)MSG.OpenSlitValve, args[0], args[1]);});
            OP.Subscribe($"{Module}.CloseSlitValve", (string cmd, object[] args) =>{return CheckToPostMessage((int)MSG.CloseSlitValve, args[0], args[1]);});
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
            if (OnEnterError != null)
                OnEnterError(Module);
            return true;
        }

        private bool FsmStartHome(object[] param)
        {
            return true;
        }

        private bool FsmMonitorHomeTask(object[] param)
        {
           
            return true;
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
            Result ret = MonitorRoutine();
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

        private bool FsmStartOpenSlitValve(object[] param)
        {
            _slitValveRoutine.Init(ModuleHelper.Converter(param[0].ToString()), ModuleHelper.Converter(param[1].ToString()), true);

            Result ret = StartRoutine(_slitValveRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartCloseSlitValve(object[] param)
        {
            _slitValveRoutine.Init(ModuleHelper.Converter(param[0].ToString()), ModuleHelper.Converter(param[1].ToString()), false);

            Result ret = StartRoutine(_slitValveRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        public int InvokeError()
        {
            if (CheckToPostMessage((int)MSG.Error))
                return (int)MSG.Error;

            return (int)FSM_MSG.NONE;
        }

        protected override bool PutOnline()
        {
            base.PutOnline();
            return true;
        }

        protected override bool PutOffline()
        {
            base.PutOffline();
            return true;
        }
    }
}
