using Aitex.Core.Common;
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
using MECF.Framework.Common.SubstrateTrackings;
using System;

namespace Mainframe.Cassettes
{
    public class CassetteModule : CassetteModuleBase
    {
        public enum STATE
        {
            NotInstall,

            Init,

            Idle,

            Homing,

            Cooling,

            Error,
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            Cooling,
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

        public event Action<string> OnEnterError;

        private bool _isInit;

        public SicCassette _cassetteDevice { get; set; }

        public CassetteModule(ModuleName module,int slot) : base(slot)
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

        }

        private void InitDevice()
        {
            _cassetteDevice = DEVICE.GetDevice<SicCassette>($"{Module}.{Module}");
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
        }

        private void InitOp()
        {
            OP.Subscribe($"{Name}.Home", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Home));
            OP.Subscribe($"{Name}.Reset", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Reset));
            OP.Subscribe($"{Name}.Abort", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Abort));

            OP.Subscribe($"{Name}.Cooling", (string cmd, object[] args) => CheckToPostMessage((int)MSG.Cooling));
        }

        private void InitData()
        {
            DATA.Subscribe($"{Name}.Status", () => StringFsmStatus);
            DATA.Subscribe($"{Name}.IsOnline", () => IsOnline);
            //DATA.Subscribe($"{Name}.IsBusy", () => IsBusy);

            OP.Subscribe($"{Name}.CreateAll", (string cmd, object[] args) =>
            {
                return CreateSlot(Convert.ToInt32(args[0].ToString()), Convert.ToInt32(args[1].ToString()));
            });
            OP.Subscribe($"{Name}.DeleteAll", (string cmd, object[] args) =>
            {
                return DeleteSlot(Convert.ToInt32(args[0].ToString()), Convert.ToInt32(args[1].ToString()));
            });
        }

        public bool CreateSlot(int startSlot,int endSlot)
        {
            startSlot = startSlot - 1;
            int slotFrom = 0;
            int slotTo = 0;
            WaferStatus state = WaferStatus.Normal;
            ModuleName chamber = ModuleHelper.Converter(Module);
            if (chamber == ModuleName.CassBL)
            {
                slotTo = endSlot > 8 ? 8 : endSlot;
                slotFrom = startSlot > 8 ? 8 : startSlot;
            }
            else
            {
                slotTo = endSlot > 25 ? 25 : endSlot;
                slotFrom = startSlot > 25 ? 25 : startSlot;
            }
            if (slotTo >= slotFrom)
            {
                if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, 0) && WaferManager.Instance.IsWaferSlotLocationValid(chamber, slotTo - 1))
                {
                    WaferManager.Instance.DeleteWafer(chamber, 0, slotTo);
                }
                else
                {
                    EV.PostWarningLog("Cassette", string.Format($"Invalid slot，{Module} slot from {slotFrom}, slot to {slotTo}"));
                    return false;
                }


                for (int slot = slotFrom; slot < slotTo; slot++)
                {
                    if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, slot))
                    {
                        if (WaferManager.Instance.CreateWafer(chamber, slot, state) == null)
                        {
                            EV.PostWarningLog("Cassette", string.Format($"Create slot {slot + 1} wafer failed."));
                            return false;
                        }
                    }
                    else
                    {
                        EV.PostWarningLog("Cassette", string.Format("Invalid slot，{0}，{1}", chamber.ToString(), slot.ToString()));
                        return false;
                    }
                }

                EV.PostInfoLog("Cassette", $"Create wafer from {slotFrom} to {slotTo} successed.");

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool DeleteSlot(int startSlot, int endSlot)
        {
            int slotFrom = 0;
            int slotTo = 0;
            WaferStatus state = WaferStatus.Normal;
            ModuleName chamber = ModuleHelper.Converter(Module);
            if (chamber == ModuleName.CassBL)
            {
                slotTo = endSlot > 13 ? 13 : endSlot;
                slotFrom = startSlot > 13 ? 13 : startSlot;
            }
            else
            {
                slotTo = endSlot > 25 ? 25 : endSlot;
                slotFrom = startSlot > 25 ? 25 : startSlot;
            }
            if (slotTo >= slotFrom)
            {
                if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, 0) && WaferManager.Instance.IsWaferSlotLocationValid(chamber, slotTo - 1))
                {
                    WaferManager.Instance.DeleteWafer(chamber, 0, slotTo);
                }
                else
                {
                    EV.PostWarningLog("Cassette", string.Format($"Invalid slot，{Module} slot from {slotFrom}, slot to {slotTo}"));
                    return false;
                }


                for (int slot = startSlot; slot < slotTo; slot++)
                {
                    if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, slot))
                    {
                        WaferManager.Instance.DeleteWafer(chamber, slot);
                    }
                    else
                    {
                        EV.PostWarningLog("Cassette", string.Format("Invalid slot，{0}，{1}", chamber.ToString(), slot.ToString()));
                        return false;
                    }
                }

                EV.PostInfoLog("Cassette", $"Delete wafer from {slotFrom} to {slotTo} successed.");

                return true;
            }
            else
            {
                return false;
            }
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

        public override void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType)
        {
            throw new NotImplementedException();
        }

        public override void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType)
        {
            throw new NotImplementedException();
        }
    }
}
