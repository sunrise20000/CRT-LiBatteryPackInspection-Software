using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.Utilities;
using Mainframe.EFEMs.Routines;
using Mainframe.TMs.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.HonghuAligners;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using Aitex.Sorter.Common;

namespace Mainframe.EFEMs
{
    public class RobotModuleBase : OfflineTimeoutNotifiableModuleBase, IModuleDevice
    {
         public enum STATE
        {
            NotInstall,
            Init,
            Idle,
            Homing,
            Error,

            Picking, 
            Placing,
            Mapping,
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            ToInit,
            Pick,
            Place,
            Map,
        };

        #region Variables

        public event Action<string> OnEnterError;

        protected bool isInit;
        protected SicEFEM CassetteDevice;
        protected RobotBaseDevice RobotDevice;

        protected EfemBaseRoutine HomeRoutine;
        protected EfemBaseRoutine MapRoutine;
        protected EfemBaseRoutine PickRoutine;
        protected EfemBaseRoutine PlaceRoutine;
        

        #endregion

        #region Constructor

        public RobotModuleBase(ModuleName module)
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

        public virtual bool IsReady => FsmState == (int)STATE.Idle && CheckAllMessageProcessed();

        public virtual bool IsError => FsmState == (int)STATE.Error;

        public virtual bool IsInit => FsmState == (int)STATE.Init;

        public bool IsBusy => !IsInit && !IsError && !IsReady;

        public bool IsIdle => FsmState == (int)STATE.Idle && CheckAllMessageProcessed();

        public bool IsAlarm => FsmState == (int)STATE.Error;

        #endregion

        #region Private Methods

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
            if (!isInit)
            {
                PostMsg(MSG.ToInit);
                return false;
            }
            
            RobotDevice.RobotReset();
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
            Result ret = StartRoutine(HomeRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartMap(object[] param)
        {
            MapRoutine.Init(ModuleHelper.Converter((string)param[0]));
            Result ret = StartRoutine(MapRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartPick(object[] param)
        {
            PickRoutine.Init(ModuleHelper.Converter((string)param[0]), (int)param[1]);
            Result ret = StartRoutine(PickRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }

        private bool FsmStartPlace(object[] param)
        {
            PlaceRoutine.Init(ModuleHelper.Converter((string)param[0]), (int)param[1]);
            Result ret = StartRoutine(PlaceRoutine);
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
                isInit = true;
                OP.DoOperation($"{Module}.ResetTask");
                return true;
            }

            return false;
        }

        private bool FsmAbortTask(object[] param)
        {
            AbortRoutine();
            RobotDevice.Abort();
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
            RobotDevice.RobotReset();
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

        public bool Abort()
        {
            CheckToPostMessage((int)MSG.Abort);
            return true;
        }

        #endregion

        #region Protected Methods

        protected virtual void InitRoutine()
        {
            
        }

        protected virtual void InitDevice()
        {
            CassetteDevice = DEVICE.GetDevice<SicEFEM>($"{Module}.{Module}");
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

            Transition(STATE.Idle, MSG.Map, FsmStartMap, STATE.Mapping);
            Transition(STATE.Mapping, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Mapping, MSG.Abort, FsmAbortTask, STATE.Idle);


            Transition(STATE.Idle, MSG.Pick, FsmStartPick, STATE.Picking);
            Transition(STATE.Picking, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Picking, MSG.Abort, FsmAbortTask, STATE.Idle);

            Transition(STATE.Idle, MSG.Place, FsmStartPlace, STATE.Placing);
            Transition(STATE.Placing, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.Placing, MSG.Abort, FsmAbortTask, STATE.Idle);



        }

        private void InitOp()
        {
            OP.Subscribe($"{Module}.Home", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Home));
            OP.Subscribe($"{Module}.Reset", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Reset));
            OP.Subscribe($"{Module}.Abort", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Abort));

            OP.Subscribe($"{Module}.Pick", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Pick, args[0], args[1]));
            OP.Subscribe($"{Module}.Place", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Place, args[0], args[1]));
            OP.Subscribe($"{Module}.Map", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Map, args[0]));
        }

        private void InitData()
        {
            DATA.Subscribe($"{Module}.Status", () => StringFsmStatus);
            DATA.Subscribe($"{Module}.IsOnline", () => IsOnline);
            DATA.Subscribe($"{Module}.IsBusy", () => IsBusy);
            DATA.Subscribe($"{Module}.IsAlarm", () => IsAlarm);
        }

        #endregion

        #region Methods

        public override bool Initialize()
        {
            InitRoutine();

            InitDevice();

            InitFsm();

            InitOp();

            InitData();

            return base.Initialize();
        }

        public virtual bool Home(out string reason)
        {
            CheckToPostMessage((int)MSG.Home);
            reason = string.Empty;
            return true;
        }

        public virtual bool Pick(ModuleName target, Hand blade, int targetSlot)
        {
            if (CheckToPostMessage((int)MSG.Pick, target.ToString(), targetSlot, blade))
                return true;
            return false;
        }

        public virtual bool Place(ModuleName target, Hand blade, int targetSlot)
        {
            if (CheckToPostMessage((int)MSG.Place, target.ToString(), targetSlot, blade))
                return true;
            return false;
        }

        public bool Map(ModuleName target)
        {
            if (CheckToPostMessage((int)MSG.Map, target.ToString()))
                return true;
            return false;
        }

        public bool CheckAcked(int entityTaskToken)
        {
            return FsmState == (int)STATE.Idle && CheckAllMessageProcessed();
            //return true;
        }


        #endregion

    }
}
