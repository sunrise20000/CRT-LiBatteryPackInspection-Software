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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mainframe.Buffers.Routines;

namespace Mainframe.Buffers
{ 
    public class BufferModule : BufferModuleBase
    {
        public enum STATE
        {
            NotInstall,

            Init,

            Idle,

            Homing,

            Cooling,
            WarmUp,

            Error,
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            Cooling,
            WarmUp,
            ToInit,
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

        public int CurrentCoolingTime
        {
            get
            {
                if (FsmState == (int)STATE.Cooling)
                    return _coolingRoutine.ElapsedTime;  

                return 0;
            }
        }

        public int TotalCoolingTime
        {
            get
            {
                if (FsmState == (int)STATE.Cooling)
                    return (int)_coolingRoutine._coolingValue;  

                return 0;
            }
        }

        public bool CoolingTypeIsTime
        {
            get
            {
                if (FsmState == (int)STATE.Cooling)
                    return _coolingRoutine._coolingTypeIsTime;

                return false;
            }
        }
        

        public event Action<string> OnEnterError;

        private bool _isInit;

        public SicBuffer _bufferDevice { get; set; }

        private BufferHomeRoutine _homeRoutine;
        private BufferCoolingRoutine _coolingRoutine;

        public BufferModule(ModuleName module) : base(3)
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

            _homeRoutine = new BufferHomeRoutine(module);
            _coolingRoutine = new BufferCoolingRoutine(module);
        }

        private void InitDevice()
        {
            _bufferDevice = DEVICE.GetDevice<SicBuffer>($"{Module}.{Module}");
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
            
            //cooling
            Transition(STATE.Idle, MSG.Cooling, FsmStartCooling, STATE.Cooling);
            Transition(STATE.Cooling, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Cooling, MSG.Abort, FsmAbortTask, STATE.Idle);

            //WarmUp
            Transition(STATE.Idle, MSG.Cooling, FsmStartWarmUp, STATE.WarmUp);
            Transition(STATE.WarmUp, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.WarmUp, MSG.Abort, FsmAbortTask, STATE.Idle);
        }

        private void InitOp()
        {
            OP.Subscribe($"{Name}.Home", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Home));
            OP.Subscribe($"{Name}.Reset", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Reset));
            OP.Subscribe($"{Name}.Abort", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Abort));
            
            OP.Subscribe($"{Name}.Cooling", (string cmd, object[] args) =>{ return CheckToPostMessage((int)MSG.Cooling, args); });

            OP.Subscribe($"{Module}.WarmUp", (string cmd, object[] args) => CheckToPostMessage((int)MSG.WarmUp));
        }

        private void InitData()
        {
            DATA.Subscribe($"{Name}.Status", () => StringFsmStatus);
            DATA.Subscribe($"{Name}.IsOnline", () => IsOnline);
            DATA.Subscribe($"{Name}.IsBusy", () => IsBusy);

            DATA.Subscribe($"{Name}.ElapseCoolingTime", () => CurrentCoolingTime);
            DATA.Subscribe($"{Name}.TotalCoolingTime", () => TotalCoolingTime);
            DATA.Subscribe($"{Name}.CoolingTypeIsTime", () => CoolingTypeIsTime);
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
            Result ret = StartRoutine(_homeRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            _isInit = false;

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

        private bool FsmStartCooling(object[] param)
        {
            _coolingRoutine.Init((bool)param[0],(int)param[1]);

            Result ret = StartRoutine(_coolingRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartWarmUp(object[] param)
        {
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

        
        public override int InvokeCooling(bool coolingTypeIsTime, int time)
        {
            if (CheckToPostMessage((int)MSG.Cooling, coolingTypeIsTime, time))
                return (int)MSG.Cooling;

            return (int)FSM_MSG.NONE;
        }


        public override int InvokeWarmUp(int temp)
        {
            if (CheckToPostMessage((int)MSG.WarmUp))
                return (int)MSG.WarmUp;

            return (int)FSM_MSG.NONE;
        }
    }
}
