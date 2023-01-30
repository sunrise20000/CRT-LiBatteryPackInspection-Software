using System;
using System.Collections.Generic;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Fsm;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.Core.IoProviders;
using SicRT.Instances;
using SicRT.Modules;
using System.Linq;
using System.Threading.Tasks;
using Mainframe.Aligners;
using MECF.Framework.RT.Core;
using MECF.Framework.Common.PLC;
using Aitex.Core.RT.SCCore;
using Mainframe.Devices;
using MECF.Framework.Common.Event;

namespace SicRT.Equipments.Systems
{

    #region FSM State & Messages
    
    public enum RtState
    {
        Init,

        Initializing,
        Idle,

        Transfer,
        AutoRunning,
        AutoIdle,

        Cycle,

        ReturnAllWafer,
        
        PlcConnected,
        PlDisconnected,

        Error,
        ShutDown = 999

    }

    #endregion

    public class EquipmentManager : FsmDevice
    {

        #region FSM State & Messages
        
        public enum MSG
        {
            HOME,
            RESET,
            ABORT,
            ERROR,

            MoveWafer,
            ReturnAllWafer,

            Stop,

            SetAutoMode,
            SetManualMode,

            CreateJob,
            PauseJob,
            ResumeJob,
            StartJob,
            StopJob,
            AbortJob,

            SetOnline,
            SetOffline,
            
            PlcConnected,
            PlcDisconnected,
            
            ModuleError,
            ShutDown = 999,
        }

        #endregion

        #region Variables

        private ManualTransfer _manualTransfer;
        private AutoTransfer _auto = null;
        private List<string> _modules = new List<string>();

        private Mainframe.Devices.IoInterLock _ioInterlock;

        #endregion

        #region Constructors

        public EquipmentManager()
        {
            Module = ModuleName.System.ToString();
            Name = ModuleName.System.ToString();

            // 包含系统中所有模组的字典
            Modules = new Dictionary<ModuleName, ModuleFsmDevice>();

        }

        #endregion

        #region Properties

        public IAdsPlc Plc { get; private set; }

        /// <summary>
        /// 返回包含系统所有模组的字典。
        /// </summary>
        public Dictionary<ModuleName, ModuleFsmDevice> Modules { get; private set; }

        public bool IsAutoMode => FsmState == (int)RtState.AutoRunning;

        public bool IsInit => FsmState == (int)RtState.Init;

        public bool IsOnline
        {
            get;
            set;
        }

        public bool IsIdle => FsmState == (int)RtState.Idle;

        public bool IsAlarm => FsmState == (int)RtState.Error;

        public bool IsRunning =>
            FsmState == (int)RtState.Initializing
            || FsmState == (int)RtState.Transfer
            || FsmState == (int)RtState.Cycle
            || FsmState == (int)RtState.AutoRunning;
        

        #endregion

    
        public override bool Initialize()
        {
            InitModules();

            EnumLoop<RtState>.ForEach((item) =>
            {
                MapState((int)item, item.ToString());
            });

            EnumLoop<MSG>.ForEach((item) =>
            {
                MapMessage((int)item, item.ToString());
            });
            EnableFsm(100, RtState.Init);

            InitRoutine();

            InitDevice();

            InitFsm();

            InitData();

            InitOp();
            
            Singleton<EventManager>.Instance.OnAlarmEvent += Instance_OnAlarmEvent;

            _manualTransfer = new ManualTransfer();
            _auto = new AutoTransfer();

            return true;
        }

        private void InitRoutine()
        {
            
        }

        private void InitDevice()
        {
            if (SC.GetValue<bool>("System.IsSimulatorMode"))
            {
                Plc = DEVICE.GetOptionDevice($"{Module}.MainPLC", typeof(WcfPlc)) as IAdsPlc;

                (Plc as WcfPlc).Initialize();
            }
            else
            {
                Plc = DEVICE.GetOptionDevice($"{Module}.MainPLC", typeof(SicAds)) as IAdsPlc;

                (Plc as SicAds).Initialize();
            }

            //System.Diagnostics.Debug.Assert(Plc != null, "System.TcAds not define");
            if (Plc != null)
            {
                Plc.OnDeviceAlarmStateChanged += OnModuleDeviceAlarmStateChanged;
                Plc.OnConnected += PlcConnected;
                Plc.OnDisconnected += PlcDisconnected;
            }
        }

        #region PLC Events

        private void PlcDisconnected()
        {
            CheckToPostMessage(MSG.PlcDisconnected);
        }

        private void PlcConnected()
        {
            CheckToPostMessage(MSG.PlcConnected);
        }

        private void OnModuleDeviceAlarmStateChanged(string deviceId, AlarmEventItem alarmItem)
        {
            if (!alarmItem.IsAcknowledged)
            {
                if (alarmItem.Level == EventLevel.Alarm)
                {
                    EV.PostAlarmLog(alarmItem.Source, alarmItem.Description);
                }
                else
                {
                    EV.PostWarningLog(alarmItem.Source, alarmItem.Description);
                }
            }


            //EV.PostAlarmLog(Module, obj.Description);
        }


        #endregion

        private void InitModules()
        {
            var feederA = new FeederModule(ModuleName.FeederA);
            Modules[ModuleName.FeederA] = feederA;
            feederA.OnEnterError += OnModuleError;

            var feederB = new FeederModule(ModuleName.FeederB);
            Modules[ModuleName.FeederB] = feederB;
            feederB.OnEnterError += OnModuleError;
            
            // 把我自己添加到字典
            _modules = new List<string>() { ModuleName.System.ToString()};
            foreach (var modulesKey in Modules.Keys)
            {
                _modules.Add(modulesKey.ToString());
            }

            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.Initialize();
            }
            
            _ioInterlock = DEVICE.GetDevice<Mainframe.Devices.IoInterLock>("TM.IoInterLock");
        }

        private void InitFsm()
        {
            //ShutDown
            Transition(RtState.Init, MSG.ShutDown, FsmStartShutDown, RtState.ShutDown);
            Transition(RtState.Idle, MSG.ShutDown, FsmStartShutDown, RtState.ShutDown);
            Transition(RtState.AutoIdle, MSG.ShutDown, FsmStartShutDown, RtState.ShutDown);
            EnterExitTransition<RtState, FSM_MSG>(RtState.ShutDown, FsmShutDown, null, null);

            //connection
            AnyStateTransition(MSG.PlcDisconnected, FsmOnDisconnected, RtState.PlDisconnected);
            Transition(RtState.PlDisconnected, MSG.PlcConnected, FsmOnConnected, RtState.Init);

            //Init sequence
            Transition(RtState.Init, MSG.HOME, FsmStartHome, RtState.Initializing);
            Transition(RtState.Idle, MSG.HOME, FsmStartHome, RtState.Initializing);
            Transition(RtState.Error, MSG.HOME, FsmStartHome, RtState.Initializing);

            //  EnterExitTransition<RtState, FSM_MSG>(RtState.Initializing, fStartInit, FSM_MSG.NONE, null);

            Transition(RtState.Initializing, FSM_MSG.TIMER, FsmMonitorHome, RtState.Idle);
            Transition(RtState.Initializing, MSG.ERROR, fError, RtState.Error);
            Transition(RtState.Initializing, MSG.ABORT, FsmAbort, RtState.Init);


            //Online
            Transition(RtState.Idle, MSG.SetOnline, FsmStartSetOnline, RtState.Idle);
            Transition(RtState.Idle, MSG.SetOffline, FsmStartSetOffline, RtState.Idle);

            //Reset
            AnyStateTransition(MSG.RESET, fStartReset, RtState.Idle);

            AnyStateTransition(MSG.ERROR, fError, RtState.Error);
            AnyStateTransition((int)FSM_MSG.ALARM, fError, (int)RtState.Error);

            //Auto/manual sequence
            Transition(RtState.Idle, MSG.SetAutoMode, fStartAutoTransfer, RtState.AutoIdle);

            Transition(RtState.AutoRunning, FSM_MSG.TIMER, fAutoTransfer, RtState.AutoRunning);

            Transition(RtState.AutoRunning, MSG.ABORT, fAbortAutoTransfer, RtState.AutoIdle);
            

            EnterExitTransition<RtState, FSM_MSG>(RtState.AutoRunning, null, FSM_MSG.NONE, fExitAutoTransfer);

            //return all wafer
            Transition(RtState.Idle, MSG.ReturnAllWafer, FsmStartReturnAllWafer, RtState.ReturnAllWafer);
            Transition(RtState.ReturnAllWafer, FSM_MSG.TIMER, FsmMonitorReturnAllWafer, RtState.Idle);
            Transition(RtState.ReturnAllWafer, MSG.ABORT, FsmAbortReturnAllWafer, RtState.Idle);


            //Transfer sequence
            Transition(RtState.Idle, MSG.MoveWafer, fStartTransfer, RtState.Transfer);
            Transition(RtState.Transfer, FSM_MSG.TIMER, fTransfer, RtState.Idle);

            EnterExitTransition<RtState, FSM_MSG>(RtState.Transfer, null, FSM_MSG.NONE, fExitTransfer);

            Transition(RtState.Transfer, MSG.ABORT, FsmAbort, RtState.Idle);
        }

        private void InitData()
        {
            DATA.Subscribe("Rt.Status", () => StringFsmStatus);
            DATA.Subscribe("System.IsOnline", () => IsOnline);
            DATA.Subscribe("System.IsIdle", () => IsIdle || IsInit);
            DATA.Subscribe("System.IsAlarm", () => IsAlarm);
            DATA.Subscribe("System.IsBusy", () => IsRunning);
            DATA.Subscribe("System.IsAutoRunning", () => IsRunning);
            DATA.Subscribe("System.Modules", () => _modules);
            DATA.Subscribe("System.LiveAlarmEvent", EV.GetAlarmEvent);
        }

        private void InitOp()
        {
            OP.Subscribe("CreateWafer", InvokeCreateWafer);

            OP.Subscribe("DeleteWafer", InvokeDeleteWafer);

            OP.Subscribe("ReturnWafer", InvokeReturnWafer);

            OP.Subscribe("DeleteTray", InvokeDeleteTray);

            OP.Subscribe("AlterWaferInfo", InvokeAlterWaferInfo);


            OP.Subscribe("System.ReturnAllWafer", (cmd, args) =>
            {
                return CheckToPostMessage((int)MSG.ReturnAllWafer);
            });

            OP.Subscribe("System.MoveWafer", (cmd, args) =>
            {
                if (!Enum.TryParse((string)args[0], out ModuleName source))
                {
                    EV.PostWarningLog(Name, $"Parameter source {(string)args[0]} not valid");
                    return false;
                }

                if (!Enum.TryParse((string)args[2], out ModuleName destination))
                {
                    EV.PostWarningLog(Name, $"Parameter destination {(string)args[1]} not valid");
                    return false;
                }
                if (args.Length > 9 )
                {
                    if ((bool)args[9])
                    {
                        WaferManager.Instance.WaferMoved(source, (int)args[1], destination, (int)args[3]);
                        return true;
                    }
                    if ((bool)args[10])
                    {
                        WaferManager.Instance.TrayMoved(source, (int)args[1], destination, (int)args[3]);
                        return true;
                    }

                }
                if (args.Length >= 8)
                {
                    return CheckToPostMessage((int)MSG.MoveWafer, source, (int)args[1], destination, (int)args[3],
                        (bool)args[4], (int)args[5], (bool)args[6], (int)args[7]);
                }
                if (args.Length == 5)
                {
                    return CheckToPostMessage((int)MSG.MoveWafer, source, (int)args[1], destination, (int)args[3], (bool)args[4]);
                }

                return CheckToPostMessage((int)MSG.MoveWafer, source, (int)args[1], destination, (int)args[3]);
            });

            OP.Subscribe("System.HomeAll", (cmd, args) => CheckToPostMessage((int)MSG.HOME));
            OP.Subscribe("System.Abort", (cmd, args) => CheckToPostMessage((int)MSG.ABORT));

            OP.Subscribe("System.Reset", (cmd, args) => CheckToPostMessage((int)MSG.RESET));

            OP.Subscribe("System.SetAutoMode", (cmd, args) => CheckToPostMessage((int)MSG.SetAutoMode));
            OP.Subscribe("System.SetManualMode", (cmd, args) => CheckToPostMessage((int)MSG.SetManualMode));

            OP.Subscribe("System.CreateJob", (cmd, args) => CheckToPostMessage((int)MSG.CreateJob, args[0]));

            OP.Subscribe("System.StartJob", (cmd, args) => CheckToPostMessage((int)MSG.StartJob, args[0]));

            OP.Subscribe("System.PauseJob", (cmd, args) => CheckToPostMessage((int)MSG.PauseJob, args[0]));

            OP.Subscribe("System.ResumeJob", (cmd, args) => CheckToPostMessage((int)MSG.ResumeJob, args[0]));

            OP.Subscribe("System.StopJob", (cmd, args) => CheckToPostMessage((int)MSG.StopJob, args[0]));

            OP.Subscribe("System.AbortJob", (cmd, args) => CheckToPostMessage((int)MSG.AbortJob, args[0]));

            OP.Subscribe("System.SetOnline", (cmd, args) => CheckToPostMessage((int)MSG.SetOnline));

            OP.Subscribe("System.SetOffline", (cmd, args) => CheckToPostMessage((int)MSG.SetOffline));
            OP.Subscribe("System.ShutDown", (cmd, args) => CheckToPostMessage((int)MSG.ShutDown, null));
        }
        
        private void Instance_OnAlarmEvent(EventItem obj)
        {
            var msg = FSM_MSG.NONE;
            switch (obj.Level)
            {
                case EventLevel.Warning:
                    msg = FSM_MSG.WARNING;
                    break;
                case EventLevel.Alarm:
                    msg = FSM_MSG.ALARM;

                    switch (obj.Source)
                    {
                        case "PM1":
                        case "PM2":
                        case "EFEM":
                        case "TM":
                        default:
                            if (Modules.ContainsKey(ModuleHelper.Converter(obj.Source)))
                            {
                                Modules[ModuleHelper.Converter(obj.Source)]?.PostMsg(msg, obj.Id, obj.Description);
                            }
                            break;
                    }

                    break;
            }
        }
        
        #region Init
        private bool FsmStartHome(object[] objs)
        {
            return true;
        }

        private bool FsmMonitorHome(object[] objs)
        {
            return false;
        }

        private bool fError(object[] objs)
        {
            IsOnline = false;

            if (FsmState == (int)RtState.Transfer)
            {
                _manualTransfer.Clear();
            }

            return true;
        }


        #endregion

        #region Plc

        private bool FsmOnConnected(object[] param)
        {
            return true;
        }

        private bool FsmOnDisconnected(object[] param)
        {
            return true;
        }

        #endregion

        #region AutoTransfer

        private bool FsmMonitorAutoIdle(object[] param)
        {
            //fMonitorFAJob(param);

            var ret = _auto.Monitor();

            //if (!_auto.CheckAllJobDone())
            //{
            //    return false;
            //}

            return ret == Result.DONE;
        }

        private bool FsmStartSetManualMode(object[] objs)
        {
            if (_auto.HasJobRunning)
            {
                EV.PostWarningLog("System", "Can not change to manual mode, abort running job first");
                return false;
            }

            return true;
        }

        private bool fStartAutoTransfer(object[] objs)
        {

            var ret = _auto.Start(objs);

            return ret == Result.RUN;
        }

        private bool fAutoTransfer(object[] objs)
        {
            var ret = _auto.Monitor();
            
            return ret == Result.DONE;
        }

        private bool fExitAutoTransfer(object[] objs)
        {
            _auto.Clear();
            return true;
        }

        private bool fAbortAutoTransfer(object[] objs)
        {
            if (FsmState == (int)RtState.Transfer)
            {
                _manualTransfer.Clear();
            }

            if (FsmState == (int)RtState.AutoRunning)
            {
                _auto.Clear();
            }
            foreach (var modulesValue in Modules.Values)
            {
                if (!modulesValue.Module.Contains("PM"))
                {
                    modulesValue.PostMsg(MSG.ABORT);
                }
            }
            return true;
        }

        #endregion

        #region  cycle

        private bool FsmAbortCycle(object[] param)
        {
            return true;
        }

        private bool FsmMonitorCycle(object[] param)
        {
            return _auto.Monitor() == Result.DONE;
        }

        private bool FsmStartCycle(object[] param)
        {
            return _auto.Start() == Result.RUN;
        }
        #endregion

        #region  return all wafer

        private bool FsmAbortReturnAllWafer(object[] param)
        {
            //_returnAll.Clear();
            return true;
        }

        private bool FsmMonitorReturnAllWafer(object[] param)
        {
            return true;
        }

        private bool FsmStartReturnAllWafer(object[] param)
        {
            return true;
        }
        #endregion

        #region Transfer
        private bool fStartTransfer(object[] objs)
        {
            var ret = _manualTransfer.Start(objs);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }


        private bool fTransfer(object[] objs)
        {
            var ret = _manualTransfer.Monitor(objs);
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.ERROR);
                return false;
            }
            return ret == Result.DONE;
        }

        private bool fExitTransfer(object[] objs)
        {
            _manualTransfer.Clear();

            return true;
        }

        private bool fAbortTransfer(object[] objs)
        {
            return true;
        }
        #endregion

        #region reset

        private bool fStartReset(object[] objs)
        {
            EV.ClearAlarmEvent();

            if (FsmState == (int)RtState.AutoRunning || FsmState == (int)RtState.AutoIdle)
            {
                _auto.ResetTask();
            }

            Singleton<DeviceEntity>.Instance.PostMsg(DeviceEntity.MSG.RESET);

            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.PostMsg(MSG.RESET);
            }


            IoProviderManager.Instance.Reset();

            if (FsmState == (int)RtState.Error)
                return true;


            return false;
        }

        #endregion
        

        private bool FsmAbort(object[] param)
        {
            if (FsmState == (int)RtState.Transfer)
            {
                _manualTransfer.Clear();
            }

            if (FsmState == (int)RtState.AutoRunning)
            {
                _auto.Clear();
            }
            foreach (var modulesValue in Modules.Values)
            {
                if (!modulesValue.Module.Contains("PM"))
                {
                    modulesValue.PostMsg(MSG.ABORT);
                }
            }
            return true;
        }

        private bool FsmStartSetOffline(object[] param)
        {
            IsOnline = false;

            foreach (var module in Modules.Values)
            {
                if(module is OfflineTimeoutNotifiableModuleBase m)
                    m.InvokeOffline();
            }

            return true;
        }

        private bool FsmStartSetOnline(object[] param)
        {
            IsOnline = true;

            foreach (var module in Modules.Values)
            {
                if (module is OfflineTimeoutNotifiableModuleBase m)
                    m.InvokeOnline();
            }

            return true;
        }
        
        #region cassette popup menu
        
        private bool InvokeReturnWafer(string arg1, object[] args)
        {
            var target = ModuleHelper.Converter(args[0].ToString());
            var slot = (int)args[1];

            if (ModuleHelper.IsLoadPort(target))
            {
                EV.PostInfoLog("System", string.Format("Wafer already at LoadPort {0} {1}, return operation is not valid", target.ToString(), slot + 1));
                return false;
            }

            if (!WaferManager.Instance.IsWaferSlotLocationValid(target, slot))
            {
                EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", target.ToString(), slot.ToString()));
                return false;
            }

            var wafer = WaferManager.Instance.GetWafer(target, slot);
            if (wafer.IsEmpty)
            {
                EV.PostInfoLog("System", string.Format("No wafer at {0} {1}, return operation is not valid", target.ToString(), slot + 1));
                return false;
            }

            return CheckToPostMessage((int)MSG.MoveWafer,
                target, slot,
                (ModuleName)wafer.OriginStation, wafer.OriginSlot,
                false, 0, false, 0);
        }


        private bool InvokeDeleteWafer(string arg1, object[] args)
        {
            var chamber = ModuleHelper.Converter(args[0].ToString());
            var slot = (int)args[1];

            if (chamber == ModuleName.TrayRobot || chamber == ModuleName.CassBL)
            {
                if (WaferManager.Instance.CheckHasTray(chamber, slot))
                {
                    WaferManager.Instance.DeleteWafer(chamber, slot);
                    EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferDelete, chamber.ToString(), slot + 1);
                }
                else
                {
                    EV.PostInfoLog("System", string.Format("No wafer at {0} {1}, delete not valid", chamber.ToString(), slot + 1));
                }
            }
            else if (WaferManager.Instance.CheckHasWafer(chamber, slot))
            {
                if (chamber == ModuleName.TMRobot || chamber == ModuleName.LoadLock || chamber == ModuleName.UnLoad || chamber == ModuleName.Buffer)
                {
                    if (WaferManager.Instance.CheckHasTray(chamber, slot))
                    {
                        WaferManager.Instance.DeleteWaferOnly(chamber, slot);
                    }
                    else
                    {
                        WaferManager.Instance.DeleteWafer(chamber, slot);
                    }
                }
                else
                {
                    WaferManager.Instance.DeleteWafer(chamber, slot);
                }
            }
            else
            {

                EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", chamber.ToString(), slot.ToString()));
                return false;
            }

            return true;
        }

        private bool InvokeCreateWafer(string arg1, object[] args)
        {
            var chamber = ModuleHelper.Converter(args[0].ToString());
            var slot = (int)args[1];
            var state = WaferStatus.Normal;

            if (chamber == ModuleName.TrayRobot || chamber == ModuleName.CassBL)
            {
                if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, slot))
                {
                    if (WaferManager.Instance.CheckHasTray(chamber, slot))
                    {
                        EV.PostInfoLog("System", string.Format("{0} slot {1} already has tray.create wafer is not valid", chamber, slot));
                    }
                    else if (WaferManager.Instance.CreateWafer(chamber, slot, state) != null)
                    {
                        EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferCreate, chamber.ToString(), slot + 1, state.ToString());
                    }
                }
                else
                {
                    EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", chamber.ToString(), slot.ToString()));
                    return false;
                }
            }
            else
            {

                if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, slot))
                {
                    if (WaferManager.Instance.CheckHasWafer(chamber, slot))
                    {
                        EV.PostInfoLog("System", string.Format("{0} slot {1} already has wafer.create wafer is not valid", chamber, slot));
                    }
                    else if (WaferManager.Instance.CreateWafer(chamber, slot, state) != null)
                    {
                        EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferCreate, chamber.ToString(), slot + 1, state.ToString());
                    }
                }
                else
                {
                    EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", chamber.ToString(), slot.ToString()));
                    return false;
                }
            }
            return true;
        }

        private bool InvokeDeleteTray(string arg1, object[] args)
        {
            var chamber = ModuleHelper.Converter(args[0].ToString());
            var slot = (int)args[1];

            if (WaferManager.Instance.CheckHasTray(chamber, slot))
            {
                if (chamber == ModuleName.Buffer)
                {
                    WaferManager.Instance.DeleteWafer(chamber, slot);
                }
                else if (WaferManager.Instance.CheckHasWafer(chamber, slot))
                {
                    WaferManager.Instance.GetWafer(chamber, slot).TrayState = WaferTrayStatus.Empty;
                }
                else
                {
                    WaferManager.Instance.DeleteWafer(chamber, slot);
                }
            }

            return true;
        }

        private bool InvokeAlterWaferInfo(string arg1, object[] args)
        {
            // See SlotEditorDialogBox.xaml.cs
            // args[0]: ModuleID
            // args[1]: SlotID
            // args[2]: WaferID
            // args[3]: RecipeName
            // args[4]: processCount
            
            var chamber = ModuleHelper.Converter(args[0].ToString());
            var slot = (int)args[1];
            var waferId = args[2].ToString();
            var recipeName = args[3].ToString();
            var trayProcessCount = (int)args[4];
            var waferInfo = WaferManager.Instance.GetWafer(chamber, slot);
            if (WaferManager.Instance.CheckHasWafer(chamber, slot))
            {
                // 更新Wafer ID
                WaferManager.Instance.UpdateWaferId(chamber, slot, waferId);
                WaferDataRecorderEx.ChangeWaferId(waferInfo.InnerId.ToString(), waferId);

                if (!string.IsNullOrEmpty(recipeName) && waferInfo.ProcessJob?.Sequence != null && waferInfo.ProcessState == EnumWaferProcessStatus.Idle)
                {
                    for (var i = 0; i < waferInfo.ProcessJob.Sequence.Steps.Count; i++)
                    {
                        if (!String.IsNullOrEmpty(waferInfo.ProcessJob.Sequence.Steps[i].RecipeName))
                        {
                            waferInfo.ProcessJob.Sequence.Steps[i].RecipeName = recipeName;
                        }
                    }
                }                
            }
            
            if (WaferManager.Instance.CheckHasTray(chamber, slot))
            {
                if (waferInfo != null)
                {
                    waferInfo.TrayProcessCount = trayProcessCount;
                }
            }
            return true;
        }

        #endregion

        private void OnModuleError(object sender, string module)
        {
            if (FsmState == (int)RtState.AutoRunning)
            {
                var mod = ModuleHelper.Converter(module);

                PostMsg(MSG.ModuleError, module);
            }
        }

        #region ShutDown
        private bool FsmStartShutDown(object[] objs)
        {
            var InBusyModuleNames = this.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(t => t.PropertyType.GetInterfaces().Contains(typeof(IModuleEntity)) && ((IModuleEntity)t.GetValue(this, null)).IsBusy)
                .Select(n => n.Name).ToArray();

            if (InBusyModuleNames.Length > 0)
            {
                EV.PostWarningLog("System", $"Shut down fail , {string.Join(" , ", InBusyModuleNames)} is in busy");
                return false;
            }

            Term();
            EV.PostKickoutMessage("ShutDown");
            return true;
        }

        private bool FsmShutDown(object[] objs)
        {
            Task.Delay(3000).ContinueWith(a =>
            {
                Environment.Exit(0);
            });
            return true;
        }

        protected void Term()
        {
            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.Terminate();
            }
        }
        
        #endregion
    }
}
