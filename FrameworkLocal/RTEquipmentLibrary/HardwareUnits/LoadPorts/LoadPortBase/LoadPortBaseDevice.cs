using Aitex.Core.RT.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using Aitex.Core.RT.Device.Unit;
using System.Threading;
using static Aitex.Core.RT.Device.Unit.IOE84Passive;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using Aitex.Core.RT.Fsm;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase
{
    public abstract class LoadPortBaseDevice : Entity, IEntity, ILoadPort
    {
        public LoadPortBaseDevice(string module, string name, RobotBaseDevice robot = null, IE84CallBack e84 = null) : base()
        {
            Module = module;
            Name = name;
            LPModuleName = (ModuleName)Enum.Parse(typeof(ModuleName), Name);
            _robot = robot;

            if (_robot != null)
                MapRobot.OnSlotMapRead += MapRobot_OnSlotMapRead;
            _lpE84Callback = e84;


            InitializeLP();
        }
        public virtual int TimelimitAction
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.TimeLimitAction"))
                    return SC.GetValue<int>($"LoadPort.{Name}.TimeLimitAction");
                return 90;
            }
        }
        public virtual int TimelimitHome
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.TimeLimitLoadportHome"))
                    return SC.GetValue<int>($"LoadPort.{Name}.TimeLimitLoadportHome");
                return 90;
            }
        }
        public virtual bool IsAutoReadCarrierID
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.EnableAutoCarrierIdRead") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.EnableAutoCarrierIdRead") : true;
            }
        }
        public virtual bool IsAutoUnloadAfterProcess
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.EnableAutoUnload") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.EnableAutoUnload") : true;
            }
        }

        public virtual bool IsBypassCarrierIDReader
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.BypassCarrierIDReader") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.BypassCarrierIDReader") : false;

            }
        }

        public virtual bool IsAutoDetectCarrierType
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.AutoDetectCarrierType") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.AutoDetectCarrierType") : true;
            }
        }


        public virtual Tuple<int, string>[] ValidCarrierTypeList
        {
            get
            {
                List<Tuple<int, string>> ret = new List<Tuple<int, string>>();

                for (int i = 0; i <= 15; i++)
                {
                    if (!SC.ContainsItem($"CarrierInfo.CarrierName{i}"))
                        continue;
                    if (SC.ContainsItem($"CarrierInfo.EnableCarrier{i}") && !SC.GetValue<bool>($"CarrierInfo.EnableCarrier{i}"))
                        continue;
                    string carriertype = "Carrier Type: " + SC.GetStringValue($"CarrierInfo.CarrierName{i}")
                        + (SC.ContainsItem($"CarrierInfo.CarrierWaferSize{i}") ? ($";WaferSize:WS{SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{i}")}") : "");
                    ret.Add(new Tuple<int, string>(i, carriertype));
                }
                return ret.ToArray();
            }
        }
        public virtual DispatchLPType DspLpType
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.DispatchLpType"))
                {
                    int inttype = SC.GetValue<int>($"LoadPort.{Name}.DispatchLpType");
                    return (DispatchLPType)inttype;
                }
                return DispatchLPType.NA;
            }
        }


        private void MapRobot_OnSlotMapRead(ModuleName arg1, string arg2)
        {
            if (arg1 == LPModuleName)
                OnSlotMapRead(arg2);
        }

        private void InitializeLP()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            SubscribeDeviceOperation();
            RegisterOperation();
        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<LoadPortBaseDevice>(Name + ".LPStateMachine", (int)LoadPortStateEnum.Init, 50);
            AnyStateTransition(LoadPortMsg.Error, fError, LoadPortStateEnum.Error);
            AnyStateTransition((int)FSM_MSG.ALARM, fError, (int)LoadPortStateEnum.Error);
            AnyStateTransition(LoadPortMsg.Abort, fStartAbort, LoadPortStateEnum.Init);


            Transition(LoadPortStateEnum.Init, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);
            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);
            Transition(LoadPortStateEnum.Resetting, LoadPortMsg.ResetComplete, fCompleteReset, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Resetting, LoadPortMsg.ActionDone, fCompleteReset, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Init, LoadPortMsg.Init, fStartInit, LoadPortStateEnum.Initializing);
            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Init, fStartInit, LoadPortStateEnum.Initializing);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.Init, fStartInit, LoadPortStateEnum.Initializing);

            Transition(LoadPortStateEnum.Error, LoadPortMsg.InitComplete, fCompleteInit, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Init, LoadPortMsg.InitComplete, fCompleteInit, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Initializing, LoadPortMsg.ActionDone, fCompleteInit, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Initializing, LoadPortMsg.InitComplete, fCompleteInit, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Init, LoadPortMsg.StartHome, fStartHomet, LoadPortStateEnum.Homing);
            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartHome, fStartHomet, LoadPortStateEnum.Homing);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.StartHome, fStartHomet, LoadPortStateEnum.Homing);

            Transition(LoadPortStateEnum.Homing, LoadPortMsg.ActionDone, fCompleteHome, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Homing, LoadPortMsg.HomeComplete, fCompleteHome, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Init, LoadPortMsg.HomeComplete, fCompleteHome, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.HomeComplete, fCompleteHome, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Load, fStartLoad, LoadPortStateEnum.Loading);
            Transition(LoadPortStateEnum.Loading, LoadPortMsg.ActionDone, fCompleteLoad, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Loading, LoadPortMsg.LoadComplete, fCompleteLoad, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Loading, FSM_MSG.TIMER, fMonitorLoad, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Unload, fStartUnload, LoadPortStateEnum.Unloading);
            Transition(LoadPortStateEnum.Unloading, LoadPortMsg.ActionDone, fCompleteUnload, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Unloading, LoadPortMsg.UnloadComplete, fCompleteUnload, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Unloading, FSM_MSG.TIMER, fMonitorUnload, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartExecute, fStartExecute, LoadPortStateEnum.Executing);
            Transition(LoadPortStateEnum.Executing, LoadPortMsg.ActionDone, fCompleteExecute, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Executing, LoadPortMsg.MoveComplete, fCompleteExecute, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Executing, FSM_MSG.TIMER, fMonitorExecuting, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartReadData, fStartRead, LoadPortStateEnum.ReadingData);
            Transition(LoadPortStateEnum.ReadingData, LoadPortMsg.ActionDone, fCompleteRead, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.ReadingData, LoadPortMsg.ReadComplete, fCompleteRead, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartWriteData, fStartWrite, LoadPortStateEnum.WrittingData);
            Transition(LoadPortStateEnum.WrittingData, LoadPortMsg.ActionDone, fCompleteWrite, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.WrittingData, LoadPortMsg.MoveComplete, fCompleteWrite, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartAccess, fStartAccess, LoadPortStateEnum.TransferBlock);
            Transition(LoadPortStateEnum.TransferBlock, LoadPortMsg.CompleteAccess, fCompleteAccess, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.TransferBlock, LoadPortMsg.ActionDone, fCompleteAccess, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.TransferBlock, FSM_MSG.TIMER, fMonitorTransferBlock, LoadPortStateEnum.TransferBlock);
        }

        protected virtual bool fMonitorTransferBlock(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorExecuting(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorInit(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorUnload(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorLoad(object[] param)
        {
            return false;
        }

        protected virtual bool fCompleteAccess(object[] param)
        {
            return true;
        }

        protected virtual bool fStartAccess(object[] param)
        {
            return true;
        }

        protected virtual bool fCompleteHome(object[] param)
        {
            return true;
        }

        protected virtual bool fStartHomet(object[] param)
        {
            return true;
        }

        protected virtual bool fCompleteWrite(object[] param)
        {
            return true;
        }

        protected abstract bool fStartWrite(object[] param);

        protected virtual bool fCompleteRead(object[] param)
        {
            return true;
        }

        protected abstract bool fStartRead(object[] param);

        protected virtual bool fCompleteMove(object[] param)
        {
            return true;
        }

        protected abstract bool fStartExecute(object[] param);

        protected virtual bool fCompleteExecute(object[] param)
        {
            return true;
        }

        protected abstract bool fStartUnload(object[] param);
        protected virtual bool fCompleteUnload(object[] param)
        {
            OnUnloaded();
            return true;
        }
        protected virtual bool fCompleteLoad(object[] param)
        {
            OnLoaded();
            return true;
        }

        protected abstract bool fStartLoad(object[] param);

        protected virtual bool fCompleteInit(object[] param)
        {
            return true;
        }

        protected abstract bool fStartInit(object[] param);

        protected virtual bool fCompleteReset(object[] param)
        {
            return true;
        }

        protected abstract bool fStartReset(object[] param);

        protected virtual bool fStartAbort(object[] param)
        {
            return true;
        }

        protected virtual bool fError(object[] param)
        {
            return true;
        }

        private void SubscribeDeviceOperation()
        {

        }

        private void SubscribeOperation()
        {

        }

        private void SubscribeDataVariable()
        {
            DoorState = FoupDoorState.Unknown;
            WaferManager.Instance.SubscribeLocation(LPModuleName, 25);

            CarrierManager.Instance.SubscribeLocation(LPModuleName.ToString());

            DATA.Subscribe(Name, "IsPresent", () => _isPresent);
            DATA.Subscribe(Name, "IsPlaced", () => _isPlaced);
            DATA.Subscribe(Name, "IsClamped", () => ClampState == FoupClampState.Close);
            DATA.Subscribe(Name, "IsDocked", () => DockState == FoupDockState.Docked);
            DATA.Subscribe(Name, "IsDoorOpen", () => DoorState == FoupDoorState.Open);
            DATA.Subscribe(Name, "IsAccessSwPressed", () => IsAccessSwPressed);
            DATA.Subscribe(Name, "CarrierId", () => _carrierId);
            DATA.Subscribe(Name, "LPLotID", () => _lplotID);
            DATA.Subscribe(Name, "IsMapped", () => _isMapped);
            DATA.Subscribe(Name, "IsAutoDetectCarrierType", () => IsAutoDetectCarrierType);
            DATA.Subscribe(Name, "ValidCarrierTypeList", () => ValidCarrierTypeList);



            DATA.Subscribe($"{Name}.LoadportState", () => CurrentState.ToString());
            DATA.Subscribe($"{Name}.Status", () => CurrentState.ToString());

            //DATA.Subscribe($"{Name}.LoadportBusy", () =>  );
            DATA.Subscribe($"{Name}.LoadportError", () => ErrorCode);
            DATA.Subscribe($"{Name}.CassetteState", () => CassetteState);
            DATA.Subscribe($"{Name}.FoupClampState", () => ClampState);
            DATA.Subscribe($"{Name}.FoupDockState", () => DockState);
            DATA.Subscribe($"{Name}.FoupDoorState", () => DoorState);
            DATA.Subscribe($"{Name}.FoupDoorPosition", () => DoorPosition);

            DATA.Subscribe($"{Name}.SlotMap", () => SlotMap);
            DATA.Subscribe($"{Name}.IndicatiorLoad", () => IndicatiorLoad);
            DATA.Subscribe($"{Name}.IndicatiorUnload", () => IndicatiorUnload);
            DATA.Subscribe($"{Name}.IndicatiorPresence", () => IndicatiorPresence);
            DATA.Subscribe($"{Name}.IndicatiorPlacement", () => IndicatiorPlacement);
            DATA.Subscribe($"{Name}.IndicatiorAlarm", () => IndicatorAlarm);
            DATA.Subscribe($"{Name}.IndicatiorAccessAuto", () => IndicatiorAccessAuto);
            DATA.Subscribe($"{Name}.IndicatiorAccessManual", () => IndicatiorAccessManual);
            DATA.Subscribe($"{Name}.IndicatiorOpAccess", () => IndicatiorOpAccess);
            DATA.Subscribe($"{Name}.IndicatiorStatus1", () => IndicatiorStatus1);
            DATA.Subscribe($"{Name}.IndicatiorStatus2", () => IndicatiorStatus2);

            DATA.Subscribe($"{Name}.CasstleType", () => (int)CasstleType);
            DATA.Subscribe($"{Name}.InfoPadCarrierType", () => SpecCarrierType);
            DATA.Subscribe($"{Name}.InfoPadCarrierTypeInformation", () => SpecCarrierInformation);


            DATA.Subscribe($"{Name}.InfoPadCarrierIndex", () => InfoPadCarrierIndex);
            DATA.Subscribe($"{Name}.CarrierWaferSize", () => GetCurrentWaferSize().ToString());
            DATA.Subscribe($"{Name}.IsError", () => CurrentState == LoadPortStateEnum.Error);
            DATA.Subscribe($"{Name}.PreDefineWaferCount", () => PreDefineWaferCount);
            DATA.Subscribe($"{Name}.IsVerifyPreDefineWaferCount", () => IsVerifyPreDefineWaferCount);

            EV.Subscribe(new EventItem("Event", EventCarrierArrived, "Carrier arrived"));
            EV.Subscribe(new EventItem("Event", EventCarrierRemoved, "Carrier removed"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdRead, "Carrier ID read"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdReadFailed, "Carrier ID read failed"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWrite, "Carrier ID write"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWriteFailed, "Carrier ID write failed"));
            EV.Subscribe(new EventItem("Event", EventSlotMapAvailable, "Slot map available"));
            EV.Subscribe(new EventItem("Event", EventCarrierUnloaded, "Carrier unloaded"));
            EV.Subscribe(new EventItem("Event", EventCarrierloaded, "Carrier loaded"));
            EV.Subscribe(new EventItem("Event", EventRfIdRead, "Carrier RFID read"));
            EV.Subscribe(new EventItem("Event", EventRfIdReadFailed, "Carrier RFID read failed"));
            EV.Subscribe(new EventItem("Event", EventRfIdWrite, "Carrier RFID write"));
            EV.Subscribe(new EventItem("Event", EventRfIdWriteFailed, "Carrier RFID write failed"));




            SubscribeAlarm();
            IndicatorStateFeedback = new IndicatorState[20];


            if (_lpE84Callback != null)
            {
                _lpE84Callback.OnE84ActiveSignalChange += _lpE84Callback_OnE84ActiveSignalChange;
                _lpE84Callback.OnE84PassiveSignalChange += _lpE84Callback_OnE84PassiveSignalChange;

                _lpE84Callback.OnE84HandOffComplete += _lpE84Callback_OnE84HandOffComplete;
                _lpE84Callback.OnE84HandOffStart += _lpE84Callback_OnE84HandOffStart;
                _lpE84Callback.OnE84HandOffTimeout += _lpE84Callback_OnE84HandOffTimeout;
            }
        }
        private void SubscribeAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortError, $"Load Port {Name} occurred error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortCommunicationError, $"Load Port {Name}error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortWaferProtrusion, $"Load Port {Name} wafer protrusion error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortLoadTimeOut, $"Load Port {Name} load timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortLoadFailed, $"Load Port {Name} load failed.", EventLevel.Alarm, EventType.EventUI_Notify));


            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortUnloadTimeOut, $"Load Port {Name} unload timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortUnloadFailed, $"Load Port {Name} unload failed.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMappingError, $"Load Port {Name} mapping error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMappingTimeout, $"Load Port {Name} mapping Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmCarrierIDReadError, $"Load Port {Name} occurred error when read carrier ID.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmCarrierIDWriteError, $"Load Port {Name} occurred error when write carrierID.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMapCrossedWafer, $"Load Port {Name} mapped crossed wafer.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMapDoubleWafer, $"Load Port {Name} mapped double wafer.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMapUnknownWafer, $"Load Port {Name} mapped unknown wafer.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortCarrierIDIndexConfigError, $"Load Port {Name} carrierID reader configuration is invalid.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortHomeFailed, $"Load Port {Name} occurred error when home.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortHomeTimeout, $"Load Port {Name} home timeout.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP1Timeout, $"Load Port {Name} occurred TP1 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP2Timeout, $"Load Port {Name} occurred TP2 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP3Timeout, $"Load Port {Name} occurred TP3 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP4Timeout, $"Load Port {Name} occurred TP4 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP5Timeout, $"Load Port {Name} occurred TP5 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP6Timeout, $"Load Port {Name} occurred TP6 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP1Timeout, $"Load Port {Name} occurred TP1 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP2Timeout, $"Load Port {Name} occurred TP2 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP3Timeout, $"Load Port {Name} occurred TP3 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP4Timeout, $"Load Port {Name} occurred TP4 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP5Timeout, $"Load Port {Name} occurred TP5 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP6Timeout, $"Load Port {Name} occurred TP6 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
        }

        public LoadPortStateEnum CurrentState => (LoadPortStateEnum)fsm.State;
        public LoadPortStateEnum PreviousState => (LoadPortStateEnum)fsm.PrevState;
        public ModuleName LPModuleName { get; protected set; }

        public virtual int InfoPadCarrierIndex { get; set; }
        public string SpecPortName { get; set; } = string.Empty;
        public string Module { get; set; }
        public string Name { get; set; }

        protected event Action<bool> ActionDone;
        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public bool IsBusy { get; set; }
        public bool IsIdle
        {
            get
            {
                return fsm.State == (int)LoadPortStateEnum.Idle && fsm.CheckExecuted() && !IsBusy;
            }
        }
        public virtual bool IsReady()
        {
            return fsm.State == (int)LoadPortStateEnum.Idle && !IsBusy;
        }
        public IndicatorState IndicatiorLoad { get; set; }
        public IndicatorState IndicatiorUnload { get; set; }
        public IndicatorState IndicatiorPresence { get; set; }
        public IndicatorState IndicatorAlarm { get; set; }
        public IndicatorState IndicatiorPlacement { get; set; }
        public IndicatorState IndicatiorOpAccess { get; set; }
        public IndicatorState IndicatiorAccessAuto { get; set; }
        public IndicatorState IndicatiorAccessManual { get; set; }
        public IndicatorState IndicatiorStatus1 { get; set; }
        public IndicatorState IndicatiorStatus2 { get; set; }
        public IndicatorState IndicatiorManualMode { get; set; }
        public IndicatorState IndicatiorClampUnclamp { get; set; }
        public IndicatorState IndicatiorDockUndock { get; set; }

        public static Dictionary<IndicatorType, Indicator> LoadPortIndicatorLightMap =
            new Dictionary<IndicatorType, Indicator>()
            {
                {IndicatorType.Load, Indicator.LOAD},
                {IndicatorType.Unload, Indicator.UNLOAD},
                {IndicatorType.AccessAuto, Indicator.ACCESSAUTO },
                {IndicatorType.AccessManual, Indicator.ACCESSMANUL},
                {IndicatorType.Alarm, Indicator.ALARM},
                {IndicatorType.Presence, Indicator.PRESENCE},
                {IndicatorType.Placement, Indicator.PLACEMENT},
                {IndicatorType.Status1, Indicator.RESERVE1},
                {IndicatorType.Status2, Indicator.RESERVE2},
            };

        public IndicatorState[] IndicatorStateFeedback { get; set; }
        public virtual FoupClampState ClampState { get; set; }
        public virtual FoupDockState DockState { get; set; }
        public virtual FoupDoorState DoorState { get; set; }
        public virtual FoupDoorPostionEnum DoorPosition { get; set; }
        public virtual bool IsVacuumON { get; set; }
        public virtual CasstleType CasstleType { get; set; }
        public string SlotMap
        {
            get { return GetSlotMap(); }
        }
        //Hirata
        public int WaferCount { get; set; }
        public string LoadPortType { get; protected set; }

        public virtual bool IsInfoPadAOn { get; set; }
        public virtual bool IsInfoPadBOn { get; set; }
        public virtual bool IsInfoPadCOn { get; set; }
        public virtual bool IsInfoPadDOn { get; set; }
        public virtual bool IsWaferProtrude { get; set; }


        public virtual string SpecCarrierType
        {
            get; set;

        } = "";

        public virtual string SpecCarrierInformation
        {
            get
            {
                if (_isPresent)
                    return "Index:" + InfoPadCarrierIndex.ToString() + "\r\n"
                    + "Type:" + SpecCarrierType + "\r\n"
                    + "Size:" + GetCurrentWaferSize().ToString() +
                    (IsVerifyPreDefineWaferCount ? ("\r\n" + "Pre-Count:" + PreDefineWaferCount.ToString()) : "");
                return "";
            }
        }

        public virtual LoadportCassetteState CassetteState
        {
            get;
            set;
        }
        public virtual int PreDefineWaferCount
        {
            get; set;
        }

        public virtual bool IsVerifyPreDefineWaferCount
        {
            get => false;
        }


        public WaferSize Size
        {
            get; set;
        } = WaferSize.WS12;

        public virtual WaferSize GetCurrentWaferSize()
        {
            return Size;
        }
        public bool IsMapWaferByLoadPort { get; set; }

        public bool IsPresent
        {
            get { return _isPresent; }
        }
        public bool IsPlacement
        {
            get { return _isPlaced; }
        }
        public bool IsMapped
        {
            get { return _isMapped; }
        }

        public virtual bool IsLoaded
        {
            get
            {
                return DockState == FoupDockState.Docked && DoorState == FoupDoorState.Open && DoorPosition == FoupDoorPostionEnum.Down;

            }

        }
        public bool IsComplete { get; set; }

        public bool IsAccessSwPressed
        {
            get { return _isAccessSwPressed; }
        }
        public virtual bool IsError { get; set; }
        public virtual string ErrorCode { get; set; } = "";
        public bool MapError { get; protected set; }
        public bool ReadCarrierIDError { get; set; }
        /// <summary>
        /// 是否处于FOSB模式
        /// </summary>
        /// 
        private bool _isRequestFosbMode;
        public bool IsRequestFOSBMode
        {
            get => _isRequestFosbMode;
            set
            {
                if (value != _isRequestFosbMode)
                {
                    _isRequestFosbMode = value;
                    EV.PostInfoLog(Module, $"{Name} fosb mode was set to {_isRequestFosbMode}");
                }
            }
        }

        public bool IsFosbModeActual { get; set; }
        public string CarrierId
        {
            get { return _carrierId; }
        }
        public string LPLotID
        {
            get { return _lplotID; }
        }

        public RD_TRIG PresentTrig
        {
            get { return _presentTrig; }
        }

        public RD_TRIG PlacedtTrig
        {
            get { return _placetTrig; }
        }

        public RD_TRIG AccessSwPressedTrig
        {
            get { return _accessSwPressedTrig; }
        }

        public RD_TRIG ClampedTrig
        {
            get { return _clampTrig; }
        }

        public RD_TRIG DockTrig
        {
            get { return _dockTrig; }
        }

        public RD_TRIG DoorTrig
        {
            get { return _doorTrig; }
        }

        protected virtual bool _isPresent { get; set; }
        protected virtual bool _isPlaced { get; set; }
        protected bool _isDocked { get => DockState == FoupDockState.Docked; }
        protected bool _isAccessSwPressed;
        protected string _carrierId = string.Empty;
        protected string _rfid;
        protected string _lplotID;

        protected bool _isMapped;
        private readonly RD_TRIG _presentTrig = new RD_TRIG();
        private readonly RD_TRIG _placetTrig = new RD_TRIG();
        private readonly RD_TRIG _accessSwPressedTrig = new RD_TRIG();
        private readonly RD_TRIG _clampTrig = new RD_TRIG();
        private readonly RD_TRIG _dockTrig = new RD_TRIG();
        private readonly RD_TRIG _doorTrig = new RD_TRIG();

        public R_TRIG TrigWaferProtrude = new R_TRIG();
        public int PortID
        {
            get => (int)LPModuleName;
        }

        protected string EventCarrierArrived = "CARRIER_ARRIVED";
        protected string EventCarrierIdRead = "CARRIER_ID_READ";
        protected string EventCarrierIdReadFailed = "CARRIER_ID_READ_FAILED";
        protected string EventCarrierIdWrite = "CARRIER_ID_WRITE";
        protected string EventCarrierIdWriteFailed = "CARRIER_ID_WRITE_FAILED";
        protected string EventSlotMapAvailable = "SLOT_MAP_AVAILABLE";
        protected string EventCarrierRemoved = "CARRIER_REMOVED";
        protected string EventCarrierUnloaded = "CARRIER_UNLOADED";
        protected string EventCarrierloaded = "CARRIR_DOCK_COMPLETE";
        protected string EventLPHomed = "LP_HOMED";

        //protected string PORT_ID = "PORT_ID";
        //protected string CAR_ID = "CAR_ID";
        //protected string SLOT_MAP = "SLOT_MAP";
        //protected string PORT_CTGRY = "PORT_CTGRY";
        //protected string RF_ID = "RF_ID";
        //protected string PORT_CARRIER_TYPE = "PORT_CARRIER_TYPE";

        private string EventRfIdRead = "RF_ID_READ";
        private string EventRfIdReadFailed = "RF_ID_READ_FAILED";
        private string EventRfIdWrite = "RF_ID_WRITE";
        private string EventRfIdWriteFailed = "RF_ID_WRITE_FAILED";

        public string AlarmLoadPortError { get => LPModuleName.ToString() + "Error"; }
        public string AlarmLoadPortCommunicationError { get => LPModuleName.ToString() + "CommunicationError"; }
        public string AlarmLoadPortWaferProtrusion { get => LPModuleName.ToString() + "WaferProtrusion"; }
        public string AlarmLoadPortLoadTimeOut { get => LPModuleName.ToString() + "LoadTimeOut"; }
        public string AlarmLoadPortLoadFailed { get => LPModuleName.ToString() + "LoadFailed"; }
        public string AlarmLoadPortUnloadTimeOut { get => LPModuleName.ToString() + "UnloadTimeOut"; }
        public string AlarmLoadPortUnloadFailed { get => LPModuleName.ToString() + "UnloadFailed"; }
        public string AlarmLoadPortMappingError { get => LPModuleName.ToString() + "MappingError"; }
        public string AlarmLoadPortMappingTimeout { get => LPModuleName.ToString() + "MappingTimeout"; }
        public string AlarmCarrierIDReadError { get => LPModuleName.ToString() + "CarrierIDReadFail"; }
        public string AlarmCarrierIDWriteError { get => LPModuleName.ToString() + "CarrierIDWriteFail"; }

        public string AlarmLoadPortHomeFailed { get => LPModuleName.ToString() + "HomeFailed"; }
        public string AlarmLoadPortHomeTimeout { get => LPModuleName.ToString() + "HomeTimeout"; }

        public string AlarmLoadE84TP1Timeout { get => LPModuleName.ToString() + "E84LoadTP1TimeOut"; }
        public string AlarmLoadE84TP2Timeout { get => LPModuleName.ToString() + "E84LoadTP2TimeOut"; }
        public string AlarmLoadE84TP3Timeout { get => LPModuleName.ToString() + "E84LoadTP3TimeOut"; }
        public string AlarmLoadE84TP4Timeout { get => LPModuleName.ToString() + "E84LoadTP4TimeOut"; }
        public string AlarmLoadE84TP5Timeout { get => LPModuleName.ToString() + "E84LoadTP5TimeOut"; }
        public string AlarmLoadE84TP6Timeout { get => LPModuleName.ToString() + "E84LoadTP6TimeOut"; }

        public string AlarmUnloadE84TP1Timeout { get => LPModuleName.ToString() + "E84UnloadTP1TimeOut"; }
        public string AlarmUnloadE84TP2Timeout { get => LPModuleName.ToString() + "E84UnloadTP2TimeOut"; }
        public string AlarmUnloadE84TP3Timeout { get => LPModuleName.ToString() + "E84UnloadTP3TimeOut"; }
        public string AlarmUnloadE84TP4Timeout { get => LPModuleName.ToString() + "E84UnloadTP4TimeOut"; }
        public string AlarmUnloadE84TP5Timeout { get => LPModuleName.ToString() + "E84UnloadTP5TimeOut"; }
        public string AlarmUnloadE84TP6Timeout { get => LPModuleName.ToString() + "E84UnloadTP6TimeOut"; }

        public string AlarmLoadPortMapCrossedWafer => LPModuleName.ToString() + "MapCrossedWafer";
        public string AlarmLoadPortMapDoubleWafer => LPModuleName.ToString() + "MapDoubleWafer";
        public string AlarmLoadPortMapUnknownWafer => LPModuleName.ToString() + "MapUnknownWafer";
        public string AlarmLoadPortCarrierIDIndexConfigError => LPModuleName.ToString() + "LoadPortCarrierIDIndexConfigError";

        private IE87CallBack _lpcallback = null;
        public IE87CallBack LPCallBack
        {
            get { return _lpcallback; }
            set { _lpcallback = value; }
        }
        private IE84CallBack _lpE84Callback = null;
        public IE84CallBack LPE84Callback
        {
            get { return _lpE84Callback; }
            set
            {
                _lpE84Callback = value;
                _lpE84Callback.OnE84ActiveSignalChange += _lpE84Callback_OnE84ActiveSignalChange;
                _lpE84Callback.OnE84PassiveSignalChange += _lpE84Callback_OnE84PassiveSignalChange;

                _lpE84Callback.OnE84HandOffComplete += _lpE84Callback_OnE84HandOffComplete;
                _lpE84Callback.OnE84HandOffStart += _lpE84Callback_OnE84HandOffStart;
                _lpE84Callback.OnE84HandOffTimeout += _lpE84Callback_OnE84HandOffTimeout;
            }
        }
        private ICarrierIDReader _carrierIDReadercallback = null;
        public ICarrierIDReader CarrierIDReaderCallBack
        {
            get { return _carrierIDReadercallback; }
            set { _carrierIDReadercallback = value; }
        }

        public virtual CIDReaderBaseDevice[] CIDReaders
        {
            get;
            set;
        }

        private RobotBaseDevice _robot = null;
        public RobotBaseDevice MapRobot
        {
            get { return _robot; }
            set { _robot = value; }
        }



        public LoadPortBaseDevice()
        {

        }



        private void _lpE84Callback_OnE84PassiveSignalChange(E84SignalID arg2, bool arg3)
        {
            ;
        }

        private void _lpE84Callback_OnE84HandOffTimeout(E84Timeout _timeout, string LoadorUnload)
        {
            if (LoadorUnload == "Load")
            {
                switch (_timeout)
                {
                    case E84Timeout.TP1:
                        EV.Notify(AlarmLoadE84TP1Timeout);
                        //OnError(AlarmLoadE84TP1Timeout);
                        break;
                    case E84Timeout.TP2:
                        EV.Notify(AlarmLoadE84TP2Timeout);
                        //OnError(AlarmLoadE84TP2Timeout);
                        break;
                    case E84Timeout.TP3:
                        EV.Notify(AlarmLoadE84TP3Timeout);
                        //OnError(AlarmLoadE84TP3Timeout);
                        break;
                    case E84Timeout.TP4:
                        EV.Notify(AlarmLoadE84TP4Timeout);
                        //OnError(AlarmLoadE84TP4Timeout);
                        break;
                    case E84Timeout.TP5:
                        EV.Notify(AlarmLoadE84TP5Timeout);
                        //OnError(AlarmLoadE84TP5Timeout);
                        break;
                    default:
                        break;
                }
            }
            if (LoadorUnload == "Unload")
            {
                switch (_timeout)
                {
                    case E84Timeout.TP1:
                        EV.Notify(AlarmUnloadE84TP1Timeout);
                        //OnError(AlarmUnloadE84TP1Timeout);
                        break;
                    case E84Timeout.TP2:
                        EV.Notify(AlarmUnloadE84TP2Timeout);
                        //OnError(AlarmUnloadE84TP2Timeout);
                        break;
                    case E84Timeout.TP3:
                        EV.Notify(AlarmUnloadE84TP3Timeout);
                        //OnError(AlarmUnloadE84TP3Timeout);
                        break;
                    case E84Timeout.TP4:
                        EV.Notify(AlarmUnloadE84TP4Timeout);
                        //OnError(AlarmUnloadE84TP4Timeout);
                        break;
                    case E84Timeout.TP5:
                        EV.Notify(AlarmUnloadE84TP5Timeout);
                        //OnError(AlarmUnloadE84TP5Timeout);
                        break;
                    default:
                        break;
                }
            }
        }

        private void _lpE84Callback_OnE84HandOffStart(string arg2)
        {
            OnE84HandOffStart(arg2 == "Load");
        }

        private void _lpE84Callback_OnE84HandOffComplete(string arg2)
        {
            OnE84HandOffComplete(arg2 == "Load");
        }

        private void _lpE84Callback_OnE84ActiveSignalChange(E84SignalID arg2, bool arg3)
        {
            ;
        }

        public virtual bool Connect()
        {
            return true;
        }

        private void RegisterOperation()
        {
            OP.Subscribe($"{Name}.SetPreDefineWaferCount", (string cmd, object[] param) =>
            {

                PreDefineWaferCount = Convert.ToInt32(param[0]);
                EV.PostInfoLog(Module, $"{Name} set pre define wafer count to {PreDefineWaferCount}");
                return true;
            });

            OP.Subscribe($"{Name}.SetInfoPadIndex", (string cmd, object[] param) =>
            {
                if (IsAutoDetectCarrierType)
                {
                    EV.PostWarningLog(Module, $"{Name} can not set carrier type index when auto detection is enable.");
                    return false;
                }

                if (SC.ContainsItem($"LoadPort.{LPModuleName}.CarrierIndex"))
                {
                    SC.SetItemValue($"LoadPort.{LPModuleName}.CarrierIndex", Convert.ToInt32(param[0]));
                }
                InfoPadCarrierIndex = Convert.ToInt32(param[0]);
                EV.PostInfoLog(Module, $"{Name} set carrier type index to {InfoPadCarrierIndex}");
                return true;
            });


            OP.Subscribe($"{Name}.ReadCarrierIDByIndex", (string cmd, object[] param) =>
            {
                if (!ReadCarrierIDByIndex(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });
            OP.Subscribe($"{Name}.WriteCarrierIDByIndex", (string cmd, object[] param) =>
            {
                if (!WriteCarrierIDByIndex(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });



            OP.Subscribe($"{Name}.ReadCarrierID", (string cmd, object[] param) =>
            {
                if (!ReadCarrierID(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });
            OP.Subscribe($"{Name}.ReadCarrierIDByLP", (string cmd, object[] param) =>
            {
                string reason;
                if (!ReadCarrierIDByLP(param, out reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID:{reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportHome", (string cmd, object[] param) =>
            {
                if (!Home(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start home");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportHomeModule", (string cmd, object[] param) =>
            {
                if (!HomeModule(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start home");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportReset", (string cmd, object[] param) =>
            {
                if (!LoadportReset(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not reset, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start reset");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportStop", (string cmd, object[] param) =>
            {
                if (!Stop(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not stop, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} stop");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoad", (string cmd, object[] param) =>
            {
                if (!Load(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoadWithoutMap", (string cmd, object[] param) =>
            {
                if (!LoadWithoutMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load without map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load without map");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoadWithMap", (string cmd, object[] param) =>
            {
                if (!Load(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load with map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load with map");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUnload", (string cmd, object[] param) =>
            {
                if (!Unload(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unload, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unload");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportUnloadWithMap", (string cmd, object[] param) =>
            {
                if (!UnloadWithMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unload, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unload");
                return true;
            });


            OP.Subscribe($"{Name}.LoadportClamp", (string cmd, object[] param) =>
            {
                if (!Clamp(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not clamp, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start clamp");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUnclamp", (string cmd, object[] param) =>
            {
                if (!Unclamp(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unclamp, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unclamp");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportOpenDoor", (string cmd, object[] param) =>
            {
                if (!OpenDoor(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not open door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start open door");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportOpenDoorNoMap", (string cmd, object[] param) =>
            {
                if (!OpenDoorNoMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not open door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start open door");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportCloseDoor", (string cmd, object[] param) =>
            {
                if (!CloseDoor(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not close door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start close door");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportDock", (string cmd, object[] param) =>
            {
                if (!Dock(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not dock, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start dock");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUndock", (string cmd, object[] param) =>
            {
                if (!Undock(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not undock, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start undock");
                return true;
            });


            OP.Subscribe($"{Name}.LoadportQueryState", (string cmd, object[] param) =>
            {
                if (!QueryState(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not query state, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start query state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportQueryLED", (string cmd, object[] param) =>
            {
                if (!QueryIndicator(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not query led state, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start query led state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportSetLED", (string cmd, object[] param) =>
            {
                int light = (int)param[0];
                int state = (int)param[1];
                if (!SetIndicator((Indicator)light, (IndicatorState)state, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not set led state, {reason}");
                    return true;
                }

                EV.PostInfoLog(Module, $"{Name} start set led state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportMap", (string cmd, object[] param) =>
            {
                if (!LoadPortMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start loadport map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start loadport map");
                return true;

            });
            OP.Subscribe($"{Name}.MapWafer", (string cmd, object[] param) =>
            {
                if (!MapWafer(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start map");
                return true;
            });

            OP.Subscribe($"{Name}.SetCassetteType", (string cmd, object[] param) =>
            {
                if (!SetCassetteType(param, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not set type, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} cassette type have set to {CasstleType}");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportForceHome", (string cmd, object[] param) =>
            {
                if (!ForceHome(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start force home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start force home");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportFOSBMode", (string cmd, object[] param) =>
            {
                if (!FOSBMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOSB mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportFOUPMode", (string cmd, object[] param) =>
            {
                if (!FOUPMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOSB mode, {reason}");
                    return false;
                }


                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportQueryFOSBMode", (string cmd, object[] param) =>
            {
                if (!QueryFOSBMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOUP mode, {reason}");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.SetLoadportLotID", (string cmd, object[] param) =>
            {
                if (!SetLotID(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} set lotID for loadport.");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportExecuteCommand", (string cmd, object[] param) =>
            {
                if (!LoadPortExecute(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });
            OP.Subscribe($"{Name}.E84Retry", (string cmd, object[] param) =>
            {
                if (!LoadPortE84Retry(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });
            OP.Subscribe($"{Name}.E84Complete", (string cmd, object[] param) =>
            {
                if (!LoadPortE84Complete(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });
            OP.Subscribe($"{Name}.ManualSetCarrierID", (string cmd, object[] param) =>
            {
                OnCarrierIdRead(param[0].ToString());


                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });

        }

        private bool LoadPortE84Complete(object[] param, out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.Complete();
            }
            reason = "";
            return true;
        }

        private bool LoadPortE84Retry(object[] param, out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.ResetE84();
            }
            reason = "";
            return true;
        }

        private bool LoadPortMap(out string reason)
        {
            reason = "";
            if (!IsMapWaferByLoadPort)
            {
                reason = "LoadPort Map not support, pls use robot map";
                EV.PostAlarmLog("System", "LoadPort Map not support, pls use robot map");
                return false;
            }
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "MapWafer" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public bool LoadPortExecute(object[] para, out string reason)
        {
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, para);
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            reason = "";
            return true;
        }

        public bool LoadportReset(out string reason)
        {
            reason = "";
            return CheckToPostMessage((int)LoadPortMsg.Reset, null);
        }

        private bool SetLotID(object[] param, out string reason)
        {
            reason = "";
            if (param == null || param.Length == 0) return false;
            _lplotID = param[0].ToString();
            return true;
        }

        private bool SetCassetteType(object[] param, out string reason)
        {
            reason = "";
            if (param.Length != 1)
            {
                reason = "Invalid setting parameter.";
                return false;
            }
            CasstleType = (CasstleType)int.Parse(param[0].ToString());
            return true;
        }
        public virtual int ValidSlotsNumber
        {
            get => 25;
        }
        public virtual bool Load(out string reason)
        {
            if (!IsEnableLoad(out reason))
            {
                return false;
            }
            reason = "";
            bool ret;
            if (IsMapWaferByLoadPort)
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithMap" });
            else
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithoutMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public virtual bool LoadWithCloseDoor(out string reason)
        {
            if (!IsEnableLoad(out reason))
                return false;
            reason = "";
            bool ret;
            if (IsMapWaferByLoadPort)
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithCloseDoor" });
            else
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithoutMapWithCloseDoor" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public virtual bool LoadWithMap(out string reason)
        {
            if (!IsEnableLoad(out reason))
                return false;
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool LoadWithoutMap(out string reason)
        {
            if (!IsEnableLoad(out reason))
                return false;
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithoutMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public virtual bool MapWafer(out string reason)
        {
            reason = "";
            if (!IsMapWaferByLoadPort)
            {
                if (_robot == null)
                {
                    reason = "No mapping tools";
                    return false;
                }
                if (!_robot.IsReady())
                {
                    reason = "Robot not ready";
                    return false;
                }
                if (!IsEnableMapWafer(out reason))
                {
                    EV.PostAlarmLog("System", $"{LPModuleName} is not ready. {reason}");
                    return false;
                }
                return _robot.WaferMapping(LPModuleName, out reason);
            }
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "MapWafer" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool QueryWaferMap(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartReadData, new object[] { "MapData" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool QueryFOSBMode(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartReadData, new object[] { "FOSBMode" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的Dock指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Dock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的FOSBUnDock指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBUnDock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "UnDock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的开门指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorOpen(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "FOSBDoorOpen" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的关门指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorClose(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "FOSBDoorClose" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的门下移指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool DoorDown(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "DoorDown" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的门上移指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool DoorUp(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "DoorUp" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool SetIndicator(IndicatorType light, IndicatorState state)
        {
            if (LoadPortIndicatorLightMap.ContainsKey(light))
            {
                SetIndicator(LoadPortIndicatorLightMap[light], state, out string _);
                return true;
            }
            EV.PostWarningLog(Module, $"Not supported indicator {light}");
            return false;
        }

        public IndicatorState GetIndicator(IndicatorType light)
        {
            if (LoadPortIndicatorLightMap.ContainsKey(light))
            {
                return IndicatorStateFeedback[(int)LoadPortIndicatorLightMap[light]];
            }
            EV.PostWarningLog(Module, $"Not supported indicator {light}");
            return IndicatorState.OFF;
        }


        public virtual bool SetE84Available(out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetHoAutoControl(false);
                _lpE84Callback.SetHoAvailable(true);
            }
            reason = "";
            return true;
        }
        public virtual bool SetE84Unavailable(out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetHoAutoControl(false);
                _lpE84Callback.SetHoAvailable(false);
            }
            reason = "";
            return true;
        }

        public virtual bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "SetIndicator", light, state });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool QueryIndicator(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "QueryIndicator" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool QueryState(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "QueryState" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Undock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Undock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Dock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Dock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool CloseDoor(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "CloseDoor" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool OpenDoor(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoor" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool OpenDoorAndDown(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoorAndDown" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool DoorUpAndClose(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "DoorUpAndClose" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }



        public virtual bool OpenDoorNoMap(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoorNoMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool OpenDoorAndMap(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoorAndMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Unclamp(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Unclamp" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Clamp(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Clamp" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Unload(out string reason)
        {
            reason = "";
            if (!IsEnableUnload(out reason))
                return false;
            bool ret = CheckToPostMessage((int)LoadPortMsg.Unload, "UnloadWithoutMap");
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool UnloadWithMap(out string reason)
        {
            reason = "";
            if (!IsEnableUnload(out reason))
                return false;
            bool ret = CheckToPostMessage((int)LoadPortMsg.Unload, "UnloadWithMap");
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }



        public virtual bool Stop(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Abort, null);
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool ClearError(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool Init(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Init, new object[] { "Init" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Home(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Init, new object[] { "Home" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool HomeModule(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartHome, new object[] { "Home" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool ForceHome(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Init, new object[] { "ForceHome" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool FOSBMode(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool FOUPMode(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool ReadCarrierIDByLP(object[] para, out string reason)
        {
            reason = "";
            if (para != null && para.Length == 2)
                return ReadCarrierIDByLP(Convert.ToInt32(para[0]), Convert.ToInt32(para[1]), out reason);
            return
                ReadCarrierIDByLP(0, 8, out reason);
        }

        public virtual bool ReadCarrierIDByLP(int offset, int length, out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "ReadCarrierID", offset, length });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool WriteCarrierID(string cid, int offset, int length, out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "WriteCarrierID", cid, offset, length });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool ChangeAccessMode(bool auto, out string reason)
        {
            reason = "";
            return true;
        }


        public virtual bool SetServiceCommand(bool inService, out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool GetE84HandoffActiveSignalState(E84PioPosition piopostion, E84PioSignalAtoP AtoPsignal)
        {
            if (_lpE84Callback == null) return false;
            switch (AtoPsignal)
            {
                case E84PioSignalAtoP.AM_AVBL:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.AM_AVBL);
                case E84PioSignalAtoP.BUSY:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.BUSY);
                case E84PioSignalAtoP.COMPT:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.COMPT);
                case E84PioSignalAtoP.CONT:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CONT);
                case E84PioSignalAtoP.CS_0:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CS_0);
                case E84PioSignalAtoP.CS_1:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CS_1);
                case E84PioSignalAtoP.TR_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.TR_REQ);
                case E84PioSignalAtoP.VALID:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.VALID);
                default:
                    return false;
            }
        }
        public virtual bool GetE84HandoffPassiveSignalState(E84PioPosition piopostion, E84PioSignalPtoA PtoAsignal)
        {
            if (_lpE84Callback == null) return false;
            switch (PtoAsignal)
            {
                case E84PioSignalPtoA.ES:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.ES);
                case E84PioSignalPtoA.HO_AVBL:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.HO_AVBL);
                case E84PioSignalPtoA.L_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.L_REQ);
                case E84PioSignalPtoA.READY:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.READY);
                case E84PioSignalPtoA.U_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.U_REQ);
                default:
                    return false;
            }



        }
        public virtual void SetE84HandoffSignalState(E84PioPosition piopostion, E84PioSignalPtoA PtoAsignal, bool state)
        {
            if (_lpE84Callback == null) return;
            switch (PtoAsignal)
            {
                case E84PioSignalPtoA.ES:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.ES, state);
                    break;
                case E84PioSignalPtoA.HO_AVBL:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.HOAvbl, state);
                    break;
                case E84PioSignalPtoA.L_REQ:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.LoadReq, state);
                    break;
                case E84PioSignalPtoA.READY:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.Ready, state);
                    break;
                case E84PioSignalPtoA.U_REQ:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.UnloadReq, state);
                    break;
                default:
                    break;

            }
        }

        public virtual void Monitor()
        {
            _presentTrig.CLK = _isPresent;
            _placetTrig.CLK = _isPlaced;
            _dockTrig.CLK = _isDocked;
            _clampTrig.CLK = ClampState == FoupClampState.Close;
            _doorTrig.CLK = DoorState == FoupDoorState.Close;
            _accessSwPressedTrig.CLK = _isAccessSwPressed;

            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetFoupStatus(_isPlaced);

                if (CurrentState == LoadPortStateEnum.Idle && ClampState == FoupClampState.Open &&
                    DockState == FoupDockState.Undocked)
                    _lpE84Callback.SetReadyTransferStatus(true);
                else
                    _lpE84Callback.SetReadyTransferStatus(false);
            }
        }

        public virtual void Reset()
        {
            MapError = false;
            ReadCarrierIDError = false;
            if (_carrierIDReadercallback != null)
                _carrierIDReadercallback.Reset();

        }
        public virtual bool IsEnableDualTransfer(out string reason)
        {
            reason = "";
            return false;
        }
        public virtual bool IsEnableMapWafer(out string reason)
        {
            reason = "";
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (!_isDocked)
            {
                reason = "carrier is not docked";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            if (DoorState != FoupDoorState.Open)
            {
                reason = "Door is not opened";
                return false;
            }
            if (DoorPosition != FoupDoorPostionEnum.Down)
            {
                reason = "Door is not down";
                return false;
            }
            return true;

        }

        public virtual bool IsEnableTransferWafer(out string reason)
        {
            reason = "";
            if(CurrentState == LoadPortStateEnum.Error)
            {
                reason = "LoadPort In Error";
                return false;
            }
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (!_isMapped)
            {
                reason = "Not Mapped";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
            foreach (WaferInfo wafer in wafers)
            {
                if (wafer.IsEmpty) continue;
                if (wafer.Status == WaferStatus.Crossed || wafer.Status == WaferStatus.Double)
                {
                    //EV.PostWarningLog(Name, $"At least one wafer is {wafer.Status.ToString()}.");
                    reason = $"At least one wafer is {wafer.Status.ToString()}.";
                    return false;
                }
            }
            return true;
        }

        public virtual bool IsEnableLoad(out string reason)
        {
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (_isDocked)
            {
                reason = "Carrier is docked";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            reason = "";
            return true;
        }
        public virtual bool IsEnableUnload(out string reason)
        {
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (!_isDocked)
            {
                reason = "Carrier is undocked";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            reason = "";
            return true;
        }

        public virtual bool IsForbidAccessSlotAboveWafer()
        {
            return false;
        }

        public CarrierOnLPState _CarrierOnState { get; private set; } = CarrierOnLPState.Unknow;

        public bool HasAlarm => throw new NotImplementedException();

        protected void ConfirmAddCarrier()
        {
            if (_isPresent && _isPlaced)
            {
                if (_CarrierOnState != CarrierOnLPState.On)
                {
                    CarrierManager.Instance.CreateCarrier(Name);
                    _CarrierOnState = CarrierOnLPState.On;
                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

                    dvid["PortID"] = PortID;
                    dvid["PORT_CTGRY"] = SpecPortName;
                    dvid["CarrierType"] = SpecCarrierType;
                    EV.Notify(EventCarrierArrived, dvid);
                    if (_lpcallback != null)
                        _lpcallback.CarrierArrive();
                    _isMapped = false;
                }
                IsComplete = false;
            }
        }

        protected void ConfirmRemoveCarrier()
        {
            if (!_isPlaced)
            {
                if (_CarrierOnState != CarrierOnLPState.Off)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, 25);
                    }
                    CarrierManager.Instance.DeleteCarrier(Name);
                    _CarrierOnState = CarrierOnLPState.Off;
                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

                    dvid["PortID"] = PortID;
                    dvid["PORT_CTGRY"] = SpecPortName;
                    dvid["CarrierType"] = SpecCarrierType;
                    dvid["CarrierID"] = _carrierId ?? "";
                    EV.Notify(EventCarrierRemoved, dvid);
                    if (_lpcallback != null) _lpcallback.CarrerRemove(_carrierId);
                }

                IsComplete = false;

                for (int i = 0; i < 25; i++)
                {
                    WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, 25);
                }
                _isMapped = false;
                _carrierId = "";
            }
        }

        public virtual void OnSlotMapRead(string _slotMap)
        {
            if (!IsMapWaferByLoadPort)
                OnActionDone(new object[] { });

            if (_slotMap.Length != ValidSlotsNumber)
            {
                EV.PostAlarmLog("LoadPort", "Mapping Data Error.");
            }

            for (int i = 0; i < _slotMap.Length; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                WaferInfo wafer = null;
                switch (_slotMap[i])
                {
                    case '0':
                        WaferManager.Instance.DeleteWafer(LPModuleName, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        break;
                    case '2':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapCrossedWafer);
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapDoubleWafer);
                        break;
                    case '?':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Unknown);
                        break;
                }
            }
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

            dvid["SlotMap"] = _slotMap;
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = CarrierId;

            EV.Notify(EventSlotMapAvailable, dvid);
            if (_slotMap.Contains("2"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Crossed wafer." }
                });
            }
            if (_slotMap.Contains("W"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Double wafer." }
                });
            }
            if (_slotMap.Contains("?"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Unknown wafer." }
                });
            }

            if (LPCallBack != null)
                LPCallBack.MappingComplete(_carrierId, _slotMap);

            _isMapped = true;
        }

        private string GetSlotMap()
        {
            WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
            string slot = "";
            for (int i = 0; i < 25; i++)
            {
                slot += wafers[i].IsEmpty ? "0" : "1";
            }

            return slot;
        }

        /// <summary>
        /// 获取LP中空缺Slot
        /// </summary>
        /// <returns>返回一个list, 顺序为从下到上.(0-25)</returns>       
        public virtual bool ReadCarrierIDByIndex(int offset = 0, int length = 16, int index = 0)
        {
            if (CIDReaders == null || CIDReaders.Length <= index) return false;
            return CIDReaders[index].ReadCarrierID(offset, length);
        }
        public virtual bool WriteCarrierIDByIndex(string cid, int offset = 0, int length = 16, int index = 0)
        {
            if (CIDReaders == null || CIDReaders.Length <= index) return false;

            return CIDReaders[index].WriteCarrierID(offset, length, cid);
        }
        public bool ReadCarrierIDByIndex(object[] para)
        {
            int offset = SC.ContainsItem($"LoadPort.{LPModuleName}.StartPage") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.StartPage") : 0;
            int length = SC.ContainsItem($"LoadPort.{LPModuleName}.DataReadSize") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.DataReadSize") : 16;
            int index = 0;
            if (para != null)
                index = Convert.ToInt32(para[0]);
            return ReadCarrierIDByIndex(offset, length, index);
        }
        public bool WriteCarrierIDByIndex(object[] para)
        {
            int offset = SC.ContainsItem($"LoadPort.{LPModuleName}.StartPage") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.StartPage") : 0;
            int length = SC.ContainsItem($"LoadPort.{LPModuleName}.DataReadSize") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.DataReadSize") : 16;

            if (para == null) return false;
            int index = Convert.ToInt32(para[0]);
            string cid = para[1].ToString();
            return WriteCarrierIDByIndex(cid, offset, length, index);
        }





        public virtual bool ReadCarrierID(int offset = 0, int length = 16)
        {
            if (SC.ContainsItem($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}"))
            {
                int cidindex = SC.GetValue<int>($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}");

                if (CIDReaders != null && CIDReaders.Length > cidindex)
                    _carrierIDReadercallback = CIDReaders[cidindex];
                else
                {
                    return true;
                    //EV.Notify(AlarmLoadPortCarrierIDIndexConfigError);
                }

            }
            if (_carrierIDReadercallback != null)
                return _carrierIDReadercallback.ReadCarrierID(offset, length);
            return false;
        }
        public virtual bool ReadCarrierID(object[] para)
        {
            if (para != null && para.Length == 2)
            {
                return ReadCarrierID(Convert.ToInt32(para[0]), Convert.ToInt32(para[1]));
            }
            return ReadCarrierID();
        }
        public virtual bool WriteCarrierID(string carrierID, int offset = 0, int length = 16)
        {
            if (_carrierIDReadercallback != null)
                return _carrierIDReadercallback.WriteCarrierID(offset, length, carrierID);
            return false;
        }
        public void OnCarrierIdRead(string carrierId)
        {
            if (_isPlaced && _isPresent)
            {
                _carrierId = carrierId;

                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierID"] = CarrierId;
                EV.Notify(EventCarrierIdRead, dvid);

                CarrierManager.Instance.UpdateCarrierId(Name, carrierId);
                if (_lpcallback != null) _lpcallback.CarrierIDReadSuccess(_carrierId);
                ReadCarrierIDError = false;

            }
            else
            {
                EV.PostWarningLog(Module, $"No FOUP found, carrier id {carrierId} not saved");
            }
        }
        public bool NoteTransferStart()
        {
            return CheckToPostMessage((int)LoadPortMsg.StartAccess, null);
        }
        public bool NoteTransferStop()
        {
            return CheckToPostMessage((int)LoadPortMsg.CompleteAccess, null);
        }

        public void ProceedSetCarrierID(string cid)
        {
            _carrierId = cid;
            CarrierManager.Instance.UpdateCarrierId(Name, cid);
        }
        public void OnCarrierIdWrite(string carrierId)
        {
            _carrierId = carrierId;
            if (_isPlaced && _isPresent)
            {
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierID"] = carrierId;
                EV.Notify(EventCarrierIdWrite, dvid);
            }
            else
            {
                EV.PostWarningLog(Module, $"No FOUP found, carrier id {carrierId} not saved");
            }
        }
        public void OnCarrierIdReadFailed()
        {
            if (_isPlaced && _isPresent)
            {
                _carrierId = "";

                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;


                EV.Notify(EventCarrierIdReadFailed, dvid);
                if (_lpcallback != null) _lpcallback.CarrierIDReadFail();

                EV.Notify(AlarmCarrierIDReadError, new SerializableDictionary<string, object> {
                    {"AlarmText","CarrierID read fail." }
                });
                ReadCarrierIDError = true;


            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, carrier id read is not valid");
            }
        }
        public void OnCarrierIdWriteFailed()
        {
            if (_isPlaced && _isPresent)
            {
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;

                EV.Notify(EventCarrierIdWriteFailed, dvid);

            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, carrier id not valid");
            }
        }


        public void OnCarrierIdRead(ModuleName module, string carrierId)
        {
            OnCarrierIdRead(carrierId);
        }
        public void OnCarrierIdReadFailed(ModuleName module)
        {
            OnCarrierIdReadFailed();
        }
        public void OnCarrierIdWrite(ModuleName module, string carrierId)
        {
            OnCarrierIdWrite(carrierId);
        }

        public void OnCarrierIdWriteFailed(ModuleName module)
        {
            OnCarrierIdWriteFailed();
        }

        public void OnLoaded()
        {
            var dvid = new SerializableDictionary<string, object>
            {
                ["CarrierID"] = _carrierId ?? "",
                ["CAR_ID"] = _carrierId ?? "",
                ["PORT_ID"] = PortID,
                ["PortID"] = PortID,
                ["PORT_CTGRY"] = SpecPortName,
                ["CarrierType"] = SpecCarrierType

            };
            EV.Notify(EventCarrierloaded, dvid);
            //if (_lpcallback != null) _lpcallback.LoadComplete();

            //}
        }
        public void OnUnloaded()
        {

            var dvid = new SerializableDictionary<string, object>();
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = _carrierId;
            dvid["CAR_ID"] = _carrierId ?? "";
            dvid["PORT_ID"] = PortID;
            EV.Notify(EventCarrierUnloaded, dvid);
            //}
            DockState = FoupDockState.Undocked;
            if (_lpcallback != null) _lpcallback.UnloadComplete();

            //_isMapped = false;
        }

        public void OnE84HandOffStart(bool isload)
        {
            if (_lpcallback != null) _lpcallback.OnE84HandoffStart(isload);
        }
        public void OnE84HandOffComplete(bool isload)
        {
            if (_lpcallback != null) _lpcallback.OnE84HandoffComplete(isload);
        }


        public void OnCarrierUndock()
        {
            var dvid = new SerializableDictionary<string, object>();
            dvid["PortID"] = PortID;
            dvid["PORT_ID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = _carrierId ?? "";
            dvid["CAR_ID"] = _carrierId ?? "";
            EV.Notify(EventCarrierUnloaded, dvid);
            //}
            if (_lpcallback != null) _lpcallback.UnloadComplete();
        }


        public void OnHomed()
        {
            //for (int i = 0; i < 25; i++)
            //{
            WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, 25);
            //}
            _isMapped = false;
            var dvid = new SerializableDictionary<string, object>();
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = _carrierId;


            EV.Notify(EventLPHomed);

            if (_lpcallback != null) _lpcallback.OnLPHomed();


        }

        public virtual void OnError(string error = "")
        {
            EV.Notify(AlarmLoadPortError);
            EV.PostAlarmLog("LoadPort", error);
            IsBusy = false;
            CheckToPostMessage((int)LoadPortMsg.Error, null);

            if (ActionDone != null)
                ActionDone(false);
        }

        protected void SetPresent(bool isPresent)
        {
            _isPresent = isPresent;
            if (_isPresent)
            {
                //ConfirmAddCarrier();
            }
            else
            {
                //ConfirmRemoveCarrier();
            }
        }

        protected virtual void SetPlaced(bool isPlaced)
        {
            _isPlaced = isPlaced;
            if (_isPlaced)
            {
                ConfirmAddCarrier();
            }
            else
            {
                ConfirmRemoveCarrier();
            }
        }
        public virtual bool OnActionDone(object[] param)
        {
            IsBusy = false;
            CheckToPostMessage((int)LoadPortMsg.ActionDone, new object[] { "ActionDone" });
            if (ActionDone != null)
                ActionDone(true);
            return true;
        }

        public void OnActionDone(bool result)
        {
            if (ActionDone != null)
                ActionDone(result);
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                if ((LoadPortMsg)msg != LoadPortMsg.ActionDone && (LoadPortMsg)msg != LoadPortMsg.StartExecute)
                    EV.PostWarningLog(Name, $"{Name} is in { (LoadPortStateEnum)fsm.State} state，can not do {(LoadPortMsg)msg}");
                return false;
            }
            CurrentParamter = args;
            if (msg < 10)
                IsBusy = true;
            else
                IsBusy = false;
            fsm.PostMsg(msg, args);

            return true;
        }
        public bool CheckToPostMessage(LoadPortMsg msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, (int)msg))
            {
                if ((LoadPortMsg)msg != LoadPortMsg.ActionDone && (LoadPortMsg)msg != LoadPortMsg.StartExecute)
                    EV.PostWarningLog(Name, $"{Name} is in { (LoadPortStateEnum)fsm.State} state，can not do {(LoadPortMsg)msg}");
                return false;
            }
            CurrentParamter = args;
            if ((int)msg < 10)
                IsBusy = true;
            else
                IsBusy = false;
            fsm.PostMsg((int)msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (LoadPortStateEnum)fsm.State, (LoadPortMsg)msg);
                return false;
            }
            reason = "";

            return true;
        }

        public virtual bool FALoad(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool WriteRfid(string cid, int startpage, int length, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool ReadRfId(out string reason)
        {
            reason = "";
            return true;
        }

        public object[] CurrentParamter { get; private set; }

        public virtual DeviceState State
        {
            get
            {
                if (CurrentState == LoadPortStateEnum.Idle)
                {
                    return DeviceState.Idle;
                }
                if (MapError || ReadCarrierIDError|| CurrentState == LoadPortStateEnum.Error)
                {
                    return DeviceState.Error;
                }

                if (IsBusy)
                    return DeviceState.Busy;

                return DeviceState.Unknown;
            }
        }

        public string InfoPadCarrierType { get; set; }
    }
    public enum LoadPortStateEnum
    {
        Undefined = 0,
        Init,
        Initializing,
        Homing,
        Resetting,
        Idle,
        Mapping,
        Loading,
        Unloading,
        Executing,
        Error,
        ReadingData,
        WrittingData,
        TransferBlock,
    };
    public enum LoadPortMsg
    {
        Init,
        StartReadData,
        StartWriteData,
        Load,
        Unload,
        Reset,
        StartExecute,
        StartHome,
        InitComplete = 10,
        ReadComplete,
        WriteComplete,
        LoadComplete,
        UnloadComplete,
        ResetComplete,
        HomeComplete,
        Error,
        Abort,
        Stop,
        MoveComplete,
        ActionDone,
        StartAccess,
        CompleteAccess,
    }

    public enum DispatchLPType
    {
        NA,
        Loader,
        Unloader,

    }
}
