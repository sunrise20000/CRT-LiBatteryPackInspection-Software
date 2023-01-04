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
using Mainframe.Buffers;
using Mainframe.LLs;
using Mainframe.TMs;
using SicPM;
using SicRT.Instances;
using SicRT.Modules;
using System.Linq;
using System.Threading.Tasks;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Device.Unit;
using Mainframe.Aligners;
using Mainframe.UnLoads;
using Mainframe.EFEMs;
using Mainframe.Cassettes;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.DBCore;
using SicPM.Devices;
using MECF.Framework.RT.Core;

namespace SicRT.Equipments.Systems
{
    public enum RtState
    {
        Init,

        Initializing,
        Idle,

        Transfer,
        AutoRunning,
        AutoIdle,

        Cycle,

        PlatformCycle,

        ReturnAllWafer,

        Error,
        ShutDown = 999

    }
    public class EquipmentManager : FsmDevice
    {
        public enum MSG
        {
            HOME,
            RESET,
            ABORT,
            ERROR,

            MoveWafer,
            ReturnAllWafer,
            PlatformCycle,

            HomeUnit,

            PauseAuto,
            ResumeAuto,

            Stop,
            StopPlatformCycle,

            StartCycle,

            SetAutoMode,
            SetManualMode,

            CreateJob,
            PauseJob,
            ResumeJob,
            StartJob,
            StopJob,
            AbortJob,

            JobDone,

            FAJobCommand,

            SetOnline,
            SetOffline,

            ModuleError,
            ShutDown = 999,
        }

        public Dictionary<ModuleName, ModuleFsmDevice> Modules { get; set; }

        public bool IsAutoMode
        {
            get
            {
                return FsmState == (int)RtState.AutoRunning;
            }
        }

        public bool IsInit
        {
            get { return FsmState == (int)RtState.Init; }
        }

        public bool IsOnline
        {
            get;
            set;
        }

        public bool IsIdle
        {
            get { return FsmState == (int)RtState.Idle; }
        }
        public bool IsAlarm
        {
            get { return FsmState == (int)RtState.Error; }
        }

        public bool IsRunning
        {
            get
            {
                return FsmState == (int)RtState.Initializing
                       || FsmState == (int)RtState.Transfer
                       || FsmState == (int)RtState.Cycle
                       || FsmState == (int)RtState.AutoRunning;
            }
        }

        private ManualTransfer _manualTransfer;
        private AutoTransfer _auto = null;
        private HomeAll _homeAll;

        private ReturnAllWafer _returnAll;
        private List<string> _modules = new List<string>();
        //readonly IEnumerable<PropertyInfo> IEntityModules;

        private MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS _upsPM1A = null;
        private MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS _upsPM1B = null;

        private MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS _upsPM2A = null;
        private MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS _upsPM2B = null;

        private IoUPS _pm1UPS = null;
        private IoUPS _pm2UPS = null;
        //AETEmp
        private MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.AE.AETemp _aeTemp = null;
        private SicAETemp _pmAETemp = null;
        //
        private IoInterLock _pmInterLock = null;
        private Mainframe.Devices.IoInterLock _tmInterLock = null;
        private IoSlitValve _pm1SlitValve = null;
        private PeriodicJob _thread;
        //
        private SicPM.Devices.IoSignalTower _st = null;
        //
        private IoTC _tc = null;
        private IoPTOffsetAndK _pt1, _pt2,_pt3,_pt4,_pt9,_pt10;


        public EquipmentManager()
        {
            Module = "System";
            Name = "System";

            Modules = new Dictionary<ModuleName, ModuleFsmDevice>();

        }

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

            BuildTransitionTable();

            SubscribeDataVariable();

            SubscribeOperation();

            InitSetPSUY();
            InitPTOffsetAndK();

            Singleton<EventManager>.Instance.OnAlarmEvent += Instance_OnAlarmEvent;

            _manualTransfer = new ManualTransfer();
            _homeAll = new HomeAll();
            _auto = new AutoTransfer();
            _returnAll = new ReturnAllWafer();

            return true;
        }

        private void InitModules()
        {
            var tm = new TMModule(ModuleName.TM);

            Modules[ModuleName.TM] = tm;
            tm.OnEnterError += OnModuleError;

            var ll = new LoadLockModule(ModuleName.LoadLock);
            Modules[ModuleName.LoadLock] = ll;
            ll.OnEnterError += OnModuleError;

            var unlaod = new UnLoadModule(ModuleName.UnLoad);
            Modules[ModuleName.UnLoad] = unlaod;
            unlaod.OnEnterError += OnModuleError;

            var buffer = new BufferModule(ModuleName.Buffer);
            Modules[ModuleName.Buffer] = buffer;
            buffer.OnEnterError += OnModuleError;


            var pm1 = new PMModule(ModuleName.PM1);
            Modules[ModuleName.PM1] = pm1;
            //pm1.OnEnterError += OnModuleError;

            //var pm2 = new PMModule(ModuleName.PM2);
            //Modules[ModuleName.PM2] = pm2;
            //pm2.OnEnterError += OnModuleError;

            var aligner = new AlignerModule(ModuleName.Aligner);
            Modules[ModuleName.Aligner] = aligner;
            aligner.OnEnterError += OnModuleError;

            var efem = new EFEMModule(ModuleName.EFEM);
            Modules[ModuleName.EFEM] = efem;
            efem.OnEnterError += OnModuleError;

            var waferRobot = new WaferRobotModule(ModuleName.WaferRobot);
            Modules[ModuleName.WaferRobot] = waferRobot;
            waferRobot.OnEnterError += OnModuleError;
            
            var trayRobot = new TrayRobotModule(ModuleName.TrayRobot);
            Modules[ModuleName.TrayRobot] = trayRobot;
            trayRobot.OnEnterError += OnModuleError;

            var cassal = new CassetteModule(ModuleName.CassAL, 25);
            Modules[ModuleName.CassAL] = cassal;
            cassal.OnEnterError += OnModuleError;

            var cassar = new CassetteModule(ModuleName.CassAR, 25);
            Modules[ModuleName.CassAR] = cassar;
            cassar.OnEnterError += OnModuleError;

            var cassbl = new CassetteModule(ModuleName.CassBL, 8);
            Modules[ModuleName.CassBL] = cassbl;
            cassbl.OnEnterError += OnModuleError;

            _modules = new List<string>() { "System" };
            foreach (var modulesKey in Modules.Keys)
            {
                _modules.Add(modulesKey.ToString());
            }

            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.Initialize();
            }

            _pm1SlitValve = DEVICE.GetDevice<IoSlitValve>("TM.PM1Door");
            _pmInterLock = DEVICE.GetDevice<IoInterLock>("PM1.PMInterLock");
            _tmInterLock = DEVICE.GetDevice<Mainframe.Devices.IoInterLock>("TM.IoInterLock");
            _upsPM1A = DEVICE.GetDevice<MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS>("PM1.ITAUPSA");
            _upsPM1B = DEVICE.GetDevice<MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS>("PM1.ITAUPSB");
            _pm1UPS = DEVICE.GetDevice<IoUPS>("PM1.UPS");

            _upsPM2A = DEVICE.GetDevice<MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS>("PM2.ITAUPSA");
            _upsPM2B = DEVICE.GetDevice<MECF.Framework.RT.EquipmentLibrary.HardwareUnits.UPS.ITAUPS>("PM2.ITAUPSB");
            _pm2UPS = DEVICE.GetDevice<IoUPS>("PM2.UPS");

            //AETemp
            _aeTemp = DEVICE.GetDevice<MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temps.AE.AETemp>("PM1.AETemp");
            _pmAETemp = DEVICE.GetDevice<SicAETemp>("PM1.PMAETemp");
            //
            _st = DEVICE.GetDevice<SicPM.Devices.IoSignalTower>("PM1.SignalTower");
            _tc = DEVICE.GetDevice<IoTC>("PM1.TC1");
            _pt1 = DEVICE.GetDevice<IoPTOffsetAndK>("PM1.PT1OffsetAndK");
            _pt2 = DEVICE.GetDevice<IoPTOffsetAndK>("PM1.PT2OffsetAndK");
            _pt3 = DEVICE.GetDevice<IoPTOffsetAndK>("PM1.PT3OffsetAndK");
            _pt4 = DEVICE.GetDevice<IoPTOffsetAndK>("PM1.PT4OffsetAndK");
            _pt9 = DEVICE.GetDevice<IoPTOffsetAndK>("PM1.PT9OffsetAndK");
            _pt10 = DEVICE.GetDevice<IoPTOffsetAndK>("PM1.PT10OffsetAndK");
            //
            _thread = new PeriodicJob(200, OnTimer, "PmSlitDoor", false, true);
            Task.Delay(15000).ContinueWith((a) => _thread.Start());

            //IEntityModules = this.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            //   .Where(t => t.PropertyType.GetInterfaces().Contains(typeof(IEntity)) && t.PropertyType.GetInterfaces().Contains(typeof(IModuleEntity)));
        }

        private void BuildTransitionTable()
        {
            //ShutDown
            Transition(RtState.Init, MSG.ShutDown, FsmStartShutDown, RtState.ShutDown);
            Transition(RtState.Idle, MSG.ShutDown, FsmStartShutDown, RtState.ShutDown);
            Transition(RtState.AutoIdle, MSG.ShutDown, FsmStartShutDown, RtState.ShutDown);
            EnterExitTransition<RtState, FSM_MSG>(RtState.ShutDown, FsmShutDown, null, null);

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
            Transition(RtState.AutoRunning, MSG.JobDone, null, RtState.AutoIdle);

            Transition(RtState.AutoRunning, MSG.FAJobCommand, FsmFAJobCommand, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.CreateJob, FsmCreateJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.StartJob, FsmStartJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.PauseJob, FsmPauseJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.ResumeJob, FsmResumeJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.StopJob, FsmStopJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.AbortJob, FsmAbortJob, RtState.AutoRunning);

            Transition(RtState.AutoIdle, FSM_MSG.TIMER, FsmMonitorAutoIdle, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.SetManualMode, FsmStartSetManualMode, RtState.Idle);
            Transition(RtState.AutoIdle, MSG.FAJobCommand, FsmFAJobCommand, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.CreateJob, FsmCreateJob, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.StartJob, FsmStartJob, RtState.AutoRunning);
            Transition(RtState.AutoIdle, MSG.ABORT, FsmAbort, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.AbortJob, FsmAbortJob, RtState.AutoIdle);

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

        void SubscribeDataVariable()
        {
            DATA.Subscribe("Rt.Status", () => StringFsmStatus);
            DATA.Subscribe("System.IsOnline", () => IsOnline);
            DATA.Subscribe("System.IsIdle", () => IsIdle || IsInit);
            DATA.Subscribe("System.IsAlarm", () => IsAlarm);
            DATA.Subscribe("System.IsBusy", () => IsRunning);
            DATA.Subscribe("System.IsAutoRunning", () => IsRunning);
            DATA.Subscribe("System.Modules", () => _modules);
            DATA.Subscribe("System.LiveAlarmEvent", () => EV.GetAlarmEvent());
        }

        void SubscribeOperation()
        {
            OP.Subscribe("CreateWafer", InvokeCreateWafer);

            OP.Subscribe("DeleteWafer", InvokeDeleteWafer);

            OP.Subscribe("ReturnWafer", InvokeReturnWafer);

            OP.Subscribe("DeleteTray", InvokeDeleteTray);

            OP.Subscribe("AlterWaferInfo", InvokeAlterWaferInfo);


            OP.Subscribe("System.ReturnAllWafer", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ReturnAllWafer);
            });

            OP.Subscribe("System.MoveWafer", (string cmd, object[] args) =>
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

            OP.Subscribe("System.HomeAll", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.HOME);
            });
            OP.Subscribe("System.Abort", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ABORT);
            });

            OP.Subscribe("System.Reset", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RESET);
            });

            OP.Subscribe("System.SetAutoMode", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.SetAutoMode);
            });
            OP.Subscribe("System.SetManualMode", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.SetManualMode);
            });

            OP.Subscribe("System.CreateJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.CreateJob, args[0]);
            });

            OP.Subscribe("System.StartJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.StartJob, args[0]);
            });

            OP.Subscribe("System.PauseJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.PauseJob, args[0]);
            });

            OP.Subscribe("System.ResumeJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ResumeJob, args[0]);
            });

            OP.Subscribe("System.StopJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.StopJob, args[0]);
            });

            OP.Subscribe("System.AbortJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.AbortJob, args[0]);
            });

            OP.Subscribe("System.SetOnline", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.SetOnline);
            });

            OP.Subscribe("System.SetOffline", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.SetOffline);
            });
            OP.Subscribe("System.ShutDown", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ShutDown, null);
            });
            //OP.Subscribe("System.StartAutoRun", (string cmd, object[] args) =>
            //{
            //    return CheckToPostMessage((int)MSG.StartJob, args[0]);
            //}); 
        }

        void InitSetPSUY()
        {
            Task.Delay(2000).ContinueWith(x=> _tc.SetPCPSUY());
        }
        void InitPTOffsetAndK()
        {
            Task.Delay(2000).ContinueWith(x => SetPTOffsetAndK());
        }

        void SetPTOffsetAndK()
        {
            _pt1.SetPTOffset();
            _pt1.SetPTK();

            _pt2.SetPTOffset();
            _pt2.SetPTK();

            _pt3.SetPTOffset();
            _pt3.SetPTK();

            _pt4.SetPTOffset();
            _pt4.SetPTK();

            _pt9.SetPTOffset();
            _pt9.SetPTK();

            _pt10.SetPTOffset();
            _pt10.SetPTK();
        }

        //EventManager触发OnAlarmEvent事件，即EV.PostAlarmLog触发
        //具体值到 EventManager里面找
        //private void Instance_OnAlarmEvent(EventItem obj)
        //{
        //    if (obj.EventEnum == "ALARM_EVENT")
        //    {
        //        //这里应该触发IoSignalTower的警报动作
        //        if (_st != null)
        //        {
        //            MECF.Framework.Common.Device.Bases.SignalLightBase islL = _st.CreateLight(MECF.Framework.Common.Device.Bases.LightType.Red);
        //            if (islL != null)
        //            {
        //                islL.StateSetPoint = MECF.Framework.Common.Device.Bases.TowerLightStatus.On;
        //            }
        //            //
        //            MECF.Framework.Common.Device.Bases.SignalLightBase islB = _st.CreateLight(MECF.Framework.Common.Device.Bases.LightType.Buzzer);
        //            if (islB != null)
        //            {
        //                islB.StateSetPoint = MECF.Framework.Common.Device.Bases.TowerLightStatus.On;
        //            }
        //        }
        //    }
        //}
        private void Instance_OnAlarmEvent(EventItem obj)
        {
            FSM_MSG msg = FSM_MSG.NONE;
            if (obj.Level == EventLevel.Warning)
                msg = FSM_MSG.WARNING;
            else if (obj.Level == EventLevel.Alarm)
            {
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

                if (_st != null)
                {
                    _st.SwitchOffBuzzer(false);
                }
            }

        }

        #region Init
        private bool FsmStartHome(object[] objs)
        {
            return _homeAll.Start() == Result.RUN;
        }

        private bool FsmMonitorHome(object[] objs)
        {
            Result ret = _homeAll.Monitor();
            if (ret == Result.DONE)
            {
                _homeAll.Clear();
                return true;
            }
            if (ret == Result.FAIL)
            {
                _homeAll.Clear();
                PostMsg(MSG.ERROR);
            }
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

        #region AutoTransfer

        private bool FsmMonitorAutoIdle(object[] param)
        {
            //fMonitorFAJob(param);

            Result ret = _auto.Monitor();

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

            Result ret = _auto.Start(objs);

            return ret == Result.RUN;
        }

        private bool fAutoTransfer(object[] objs)
        {
            Result ret = _auto.Monitor();

            if (_auto.CheckAllJobDone())
            {
                if (!CheckToPostMessage((int)MSG.JobDone))
                    return false;
            }

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
            return _returnAll.Monitor() == Result.DONE;
        }

        private bool FsmStartReturnAllWafer(object[] param)
        {
            return _returnAll.Start() == Result.RUN;
        }
        #endregion

        #region Transfer
        private bool fStartTransfer(object[] objs)
        {
            Result ret = _manualTransfer.Start(objs);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }


        private bool fTransfer(object[] objs)
        {
            Result ret = _manualTransfer.Monitor(objs);
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

        private bool FsmFAJobCommand(object[] param)
        {
            switch ((string)param[0])
            {
                case "CreateProcessJob":
                    //_auto.CreateProcessJob((string)param[1], (string)param[2], (List<int>)param[3], (bool)param[4]);
                    break;
                case "CreateControlJob":
                    //_auto.CreateControlJob((string)param[1], (string)param[2], (List<string>)param[3], (bool)param[4]);
                    CheckToPostMessage((int)MSG.StartJob, (string)param[1]);
                    break;
            }

            return true;
        }

        private bool FsmCreateJob(object[] param)
        {
            _auto.CreateJob((Dictionary<string, object>)param[0]);

            return true;
        }



        private bool FsmAbortJob(object[] param)
        {
            _auto.AbortJob((string)param[0]);
            return true;
        }

        private bool FsmStopJob(object[] param)
        {
            _auto.StopJob((string)param[0]);
            return true;
        }

        private bool FsmResumeJob(object[] param)
        {
            _auto.ResumeJob((string)param[0]);
            return true;
        }

        private bool FsmPauseJob(object[] param)
        {
            _auto.PauseJob((string)param[0]);
            return true;
        }

        private bool FsmStartJob(object[] param)
        {
            _auto.StartJob((string)param[0]);
            return true;
        }

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

            var tm = Modules[ModuleName.TM] as TMModule;
            tm.InvokeOffline();

            var ll = Modules[ModuleName.LoadLock] as LoadLockModule;
            ll.InvokeOffline();

            var buffer = Modules[ModuleName.Buffer] as BufferModule;
            buffer.InvokeOffline();

            var unLoad = Modules[ModuleName.UnLoad] as UnLoadModule;
            unLoad.InvokeOffline();

            var efem = Modules[ModuleName.EFEM] as EFEMModule;
            efem.InvokeOffline();

            var waferRobot = Modules[ModuleName.WaferRobot] as WaferRobotModule;
            waferRobot.InvokeOffline();

            var trayRobot = Modules[ModuleName.TrayRobot] as TrayRobotModule;
            trayRobot.InvokeOffline();

            var aligner = Modules[ModuleName.Aligner] as AlignerModule;
            aligner.InvokeOffline();

            var pm1 = Modules[ModuleName.PM1] as PMModule;
            pm1.InvokeOffline();

            var cassAL = Modules[ModuleName.CassAL] as CassetteModule;
            cassAL.InvokeOffline();

            var cassAR = Modules[ModuleName.CassAR] as CassetteModule;
            cassAR.InvokeOffline();

            var cassBL = Modules[ModuleName.CassBL] as CassetteModule;
            cassBL.InvokeOffline();

            //var pm2 = Modules[ModuleName.PM2] as PMModule;
            //pm2.InvokeOffline();


            return true;
        }

        private bool FsmStartSetOnline(object[] param)
        {
            IsOnline = true;

            var tm = Modules[ModuleName.TM] as TMModule;
            tm.InvokeOnline();

            var ll = Modules[ModuleName.LoadLock] as LoadLockModule;
            ll.InvokeOnline();

            var buffer = Modules[ModuleName.Buffer] as BufferModule;
            buffer.InvokeOnline();

            var unLoad = Modules[ModuleName.UnLoad] as UnLoadModule;
            unLoad.InvokeOnline();

            var efem = Modules[ModuleName.EFEM] as EFEMModule;
            efem.InvokeOnline();

            var waferRobot = Modules[ModuleName.WaferRobot] as WaferRobotModule;
            waferRobot.InvokeOnline();

            var trayRobot = Modules[ModuleName.TrayRobot] as TrayRobotModule;
            trayRobot.InvokeOnline();

            var aligner = Modules[ModuleName.Aligner] as AlignerModule;
            aligner.InvokeOnline();

            var pm1 = Modules[ModuleName.PM1] as PMModule;
            pm1.InvokeOnline();

            var cassAL = Modules[ModuleName.CassAL] as CassetteModule;
            cassAL.InvokeOnline();

            var cassAR = Modules[ModuleName.CassAR] as CassetteModule;
            cassAR.InvokeOnline();

            var cassBL = Modules[ModuleName.CassBL] as CassetteModule;
            cassBL.InvokeOnline();

            //var pm2 = Modules[ModuleName.PM2] as PMModule;
            //pm2.InvokeOnline();


            return true;
        }
        #region cassette popup menu
        private bool InvokeReturnWafer(string arg1, object[] args)
        {
            ModuleName target = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];

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

            WaferInfo wafer = WaferManager.Instance.GetWafer(target, slot);
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
            ModuleName chamber = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];

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
            ModuleName chamber = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];
            WaferStatus state = WaferStatus.Normal;

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
            ModuleName chamber = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];

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
            
            ModuleName chamber = ModuleHelper.Converter(args[0].ToString());
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
                    for (int i = 0; i < waferInfo.ProcessJob.Sequence.Steps.Count; i++)
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

        private void OnModuleError(string module)
        {
            if (FsmState == (int)RtState.AutoRunning)
            {
                ModuleName mod = ModuleHelper.Converter(module);

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


        public bool OnTimer()
        {
            //MonitorModuleAlarm();
            MonitorPmTmInfo();
            MonitorUPSAlarm();
            MonitorAETemp(); //AE通断，DO220
            return true;
        }

        //public void MonitorModuleAlarm()
        //{
        //    var alarms = EV.GetAlarmEvent();
        //    if (alarms != null && StringFsmStatus != "Initializing")
        //    {
        //        foreach (var modulesNa in Modules.Keys)
        //        {
        //            if (alarms.FindAll(a => a.Level == EventLevel.Alarm && a.Source == modulesNa.ToString()).Count > 0 && Modules[modulesNa].StringFsmStatus.ToLower() != "error")
        //            {
        //                Modules[modulesNa].PostMsg(MSG.ERROR);
        //            }
        //        }
        //    }
        //}


        private void MonitorPmTmInfo()
        {
            if (_pm1SlitValve != null)
            {
                if (_pmInterLock != null && _pmInterLock.DoPMASlitDoorClosed != _pm1SlitValve.IsClose)
                {
                    _pmInterLock.DoPMASlitDoorClosed = _pm1SlitValve.IsClose;
                }

                if (_tmInterLock != null && _pmInterLock != null)
                {
                    if (_tmInterLock.DoPm1LidClosed != _pmInterLock.DiChamLidClosed)
                    {
                        _tmInterLock.DoPm1LidClosed = _pmInterLock.DiChamLidClosed;
                    }

                    if (_tmInterLock.DoRectorAATMTransferReady != _pmInterLock.DoReactorATMTransferReady)
                    {
                        _tmInterLock.DoRectorAATMTransferReady = _pmInterLock.DoReactorATMTransferReady;
                    }
                    if (_tmInterLock.DoRectorAProcessTransferReady != _pmInterLock.DoReactorVACTransferReady)
                    {
                        _tmInterLock.DoRectorAProcessTransferReady = _pmInterLock.DoReactorVACTransferReady;
                    }
                }
            }
        }

        private void MonitorUPSAlarm()
        {
            string sReason;
            if (_upsPM1A != null && _upsPM1B != null && _pm1UPS != null)
            {
                bool _power = !(_upsPM1A.UPSPowerAlarm || _upsPM1B.UPSPowerAlarm);
                bool _lowerBattery = !(_upsPM1A.UPSLowerBatteryAlarm || _upsPM1B.UPSLowerBatteryAlarm);


                if (_pm1UPS.UPSEnable == _power)
                {
                    //_pm1UPS.UPSEnable = _power; 
                    _pm1UPS.SetUPSEnable(_power, out sReason);
                }
                if (_pm1UPS.UPSLowBattery == _lowerBattery)
                {
                    //_pm1UPS.UPSLowBattery = _lowerBattery; 
                    _pm1UPS.SetUPSLowBattery(_lowerBattery, out sReason);
                }
            }

        }

        private void MonitorAETemp()
        {
            if (_aeTemp != null && _pmAETemp != null)
            {
                string sReason;
                if (!_aeTemp.IsConnected)
                {
                    _pmAETemp.SetPyroCommunicationError(true, out sReason);
                }
                else
                {
                    _pmAETemp.SetPyroCommunicationError(false, out sReason);
                }
            }
            
        }
        #endregion
    }
}
