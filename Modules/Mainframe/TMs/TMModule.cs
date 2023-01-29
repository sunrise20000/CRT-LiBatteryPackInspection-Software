using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Mainframe.TMs.Routines;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.PLC;
using SicAds = Mainframe.Devices.SicAds;

namespace Mainframe.TMs
{
    public class TMModule : TMModuleBase
    {
        public enum STATE
        {
            NotInstall,
            Init,
            Idle,
            Homing,
            Picking,
            Placing,
            Pump,
            Vent,
            Error,
            Purge,
            ServoPressure,
            Transfer,
            RobotMove,
            RobotHome,
            RobotGoto,
            NotConnect,
        }

        public enum MSG
        {
            Home,
            Reset,
            Abort,
            Error,
            Connected,
            Disconnected,
            Transfer,
            Pump,
            Vent,
            Purge,
            LeakCheck,
            OpenSlitValve,
            CloseSlitValve,
            ServoPressure,
            RobotMove,
            RobotHome,
            RobotPick,
            RobotPlace,
            RobotGoto,
            RobotExtend,
            RobotRetract,
            RobotPickAndPlace,
            ToInit,
        };



        public bool IsAlarm
        {
            get
            {
                return FsmState == (int)STATE.Error;
            }
        }

        public bool IsWarning
        {
            get
            {
                int count = 0;
                var warnings = EV.GetAlarmEvent();
                foreach (var warm in warnings)
                {
                    if (warm.Level == EventLevel.Warning && warm.Source == Name)
                        count++;
                }

                return !IsAlarm && count > 0;
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
                return 0;
            }
        }

        public int CurrentRoutineLoopTotal
        {
            get
            {

                return 0;
            }
        }

        public event Action<string> OnEnterError;

        public SicTMRobot RobotDevice { get; set; }
        public SicTM TMDevice { get; set; }

        public IoValve VentValve { get; set; }
        public IoValve PumpValve { get; set; }

        public IoPressureMeter3 ForelineGuage { get; set; }
        public IoPressureMeter3 ChamberGuage { get; set; }

        public Dictionary<ModuleName, IoSlitValve> SlitValves { get; set; } = new Dictionary<ModuleName, IoSlitValve>();
        public Dictionary<ModuleName, bool> SlitValveInstalled { get; set; } = new Dictionary<ModuleName, bool>();

        private TMFakeRoutine _fakeRoutine;

        private bool _isInit;
        private bool _isStartTMRobotHome;
        public bool IsTMRobotHomed { get; set; }

        public IAdsPlc Plc { get; set; }

        private List<IDevice> _allModuleDevice = new List<IDevice>();

        Func<MemberInfo, bool> _hasTagAttribute;
        Func<object, bool> _isTagAttribute;

        public TMModule(ModuleName module) : base(1)
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
            _fakeRoutine = new TMFakeRoutine();

        }

        public void InitDevice()
        {
            if (IsInstalled)
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
            }

            //System.Diagnostics.Debug.Assert(Plc != null, "System.TcAds not define");
            if (Plc != null)
            {
                Plc.OnDeviceAlarmStateChanged += OnModuleDeviceAlarmStateChanged;
                Plc.OnConnected += PlcConnected;
                Plc.OnDisconnected += PlcDisconnected;

                _allModuleDevice.Add(Plc);
            }


            _isTagAttribute = attribute => attribute is TagAttribute;
            _hasTagAttribute = mi => mi.GetCustomAttributes(false).Any(_isTagAttribute);
            Parallel.ForEach(this.GetType().GetProperties().Where(_hasTagAttribute),
                field =>
                {
                    TagAttribute tag = field.GetCustomAttributes(false).First(_isTagAttribute) as TagAttribute;
                    IDevice device = DEVICE.GetDevice<IDevice>($"{Module}.{tag.Tag}");
                    device.OnDeviceAlarmStateChanged += OnModuleDeviceAlarmStateChanged;

                    _allModuleDevice.Add(device);

                    PropertyInfo pi = (PropertyInfo)field;

                    var convertedValue = Convert.ChangeType(device, pi.PropertyType);

                    System.Diagnostics.Debug.Assert(convertedValue != null);

                    pi.SetValue(this, convertedValue);
                });

            RobotDevice = DEVICE.GetDevice<SicTMRobot>($"{ModuleName.TMRobot}.{ModuleName.TMRobot}");
            TMDevice = DEVICE.GetDevice<SicTM>($"{ModuleName.System}.{ModuleName.TM}");

            TMDevice.OnDeviceAlarmStateChanged += TMDevice_OnDeviceAlarmStateChanged;

            VentValve = DEVICE.GetDevice<IoValve>("TM.TMVent");
            PumpValve = DEVICE.GetDevice<IoValve>("TM.TMFastRough");
            ChamberGuage = DEVICE.GetDevice<IoPressureMeter3>("TM.TMPressure");
            ForelineGuage = DEVICE.GetDevice<IoPressureMeter3>("TM.ForelinePressure");
        }

        private void InitFsm()
        {
            //Error
            AnyStateTransition(MSG.Error, FsmOnError, STATE.Error);
            AnyStateTransition(FSM_MSG.ALARM, FsmOnError, STATE.Error);
            Transition(STATE.Error, MSG.Reset, FsmReset, STATE.Idle);
            EnterExitTransition<STATE, FSM_MSG>(STATE.Error, FsmEnterError, FSM_MSG.NONE, FsmExitError);

            //connection
            AnyStateTransition(MSG.Disconnected, FsmOnDisconnected, STATE.NotConnect);
            Transition(STATE.NotConnect, MSG.Connected, FsmOnConnected, STATE.Init);
            Transition(STATE.NotConnect, MSG.Reset, FsmResetConnect, STATE.NotConnect);

            //Home
            Transition(STATE.Init, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Error, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Idle, MSG.Home, FsmStartHome, STATE.Homing);
            Transition(STATE.Homing, FSM_MSG.TIMER, FsmMonitorHomeTask, STATE.Idle);
            Transition(STATE.Homing, MSG.Error, null, STATE.Init);
            Transition(STATE.Homing, MSG.Abort, FsmAbortTask, STATE.Init);

            EnterExitTransition((int)STATE.Homing, FsmEnterIdle, (int)FSM_MSG.NONE, FsmExitIdle);

            AnyStateTransition(MSG.ToInit, FsmToInit, STATE.Init);

            //robot home
            Transition(STATE.Idle, MSG.RobotHome, FsmStartRobotHome, STATE.RobotHome);
            Transition(STATE.RobotHome, FSM_MSG.TIMER, FsmMonitorTask, STATE.Idle);
            Transition(STATE.RobotHome, MSG.Abort, FsmAbortTask, STATE.Idle);
        }

        private void InitOp()
        {
            OP.Subscribe("Robot.MoveWafer", (string cmd, object[] args) =>
            {
                if (!Enum.TryParse((string)args[0], out ModuleName source))
                {
                    EV.PostWarningLog(Name, $"Parameter source {(string)args[0]} not valid");
                    return false;
                }

                if (!Enum.TryParse((string)args[1], out ModuleName destination))
                {
                    EV.PostWarningLog(Name, $"Parameter destination {(string)args[1]} not valid");
                    return false;
                }

                return CheckToPostMessage((int)MSG.Transfer, source, destination);
            });

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

            OP.Subscribe($"{Name}.ServoPressure", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ServoPressure);
            });

            OP.Subscribe($"{Name}.LeakCheck", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.LeakCheck, args);
            });

            OP.Subscribe($"{Name}.OpenSlitValve", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.OpenSlitValve, args[0]);
            });

            OP.Subscribe($"{Name}.CloseSlitValve", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.CloseSlitValve, args[0]);
            });


            OP.Subscribe($"{Name}.Reset", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Reset);
            });


            OP.Subscribe($"{Name}.Abort", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.Abort);
            });

            OP.Subscribe($"TMRobot.Home", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RobotHome);
            });

            OP.Subscribe($"TMRobot.Pick", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RobotPick, args[0], args[1], args[2]);
            });

            OP.Subscribe($"TMRobot.Place", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RobotPlace, args[0], args[1], args[2]);
            });

            OP.Subscribe($"TMRobot.Goto", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RobotGoto, args[0], args[1], args[2], args[3], args[4]);
            });

            OP.Subscribe($"TMRobot.Extend", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RobotExtend, args[0], args[1], args[2]);
            });

            OP.Subscribe($"TMRobot.Retract", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RobotRetract, args[0], args[1], args[2]);
            });
            
        }

        private void InitData()
        {
            DATA.Subscribe($"{Name}.Status", () => StringFsmStatus);
            DATA.Subscribe($"{Name}.IsOnline", () => IsOnline);
            DATA.Subscribe($"TMRobot.IsHomed", () => IsTMRobotHomed);
            DATA.Subscribe($"{Module}.IsError", () => IsError);
            DATA.Subscribe($"{Module}.IsAlarm", () => IsAlarm);
            DATA.Subscribe($"{Module}.IsWarning", () => IsWarning);

            DATA.Subscribe($"{Name}.CurrentRoutineLoop", () => CurrentRoutineLoop, SubscriptionAttribute.FLAG.IgnoreSaveDB);
            DATA.Subscribe($"{Name}.CurrentRoutineLoopTotal", () => CurrentRoutineLoopTotal, SubscriptionAttribute.FLAG.IgnoreSaveDB);
            

            DATA.Subscribe($"{Name}.AtATM", () =>
            {
                if (TMDevice.CheckAtm())
                {
                    return true;
                }
                return false;

            });
            DATA.Subscribe($"{Name}.UnderVAC", () =>
            {
                if (TMDevice.CheckVacuum())
                {
                    return true;
                }
                return false;

            });
        }

        private void PlcDisconnected()
        {
            CheckToPostMessage(MSG.Disconnected);
        }

        private void PlcConnected()
        {
            CheckToPostMessage(MSG.Connected);
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

        private bool FsmOnError(object[] param)
        {
            IsOnline = false;

            if (FsmState == (int)STATE.Error)
            {
                return false;
            }

            if (FsmState == (int)STATE.Picking)
            {
                _fakeRoutine.Abort();

            }
            if (FsmState == (int)STATE.Placing)
            {
                _fakeRoutine.Abort();
            }

            if (FsmState == (int)STATE.RobotGoto)
            {
                _fakeRoutine.Abort();
            }

            if (FsmState == (int)STATE.Init)
                return false;

            if (FsmState == (int)STATE.ServoPressure)
            {
                _fakeRoutine.Abort();
            }

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

        private bool FsmOnConnected(object[] param)
        {
            //SignalTowerDevice.ResetData();

            //LP1Device.ResetData();
            //LP2Device.ResetData();
            //LP3Device.ResetData();
            //LP4Device.ResetData();

            //EfemDevice.ResetData();

            return true;
        }

        private bool FsmOnDisconnected(object[] param)
        {
            return true;
        }

        private bool FsmResetConnect(object[] param)
        {
            //if (!EfemDevice.Connection.IsConnected)
            //{
            //    EfemDevice.Connect();
            //    return false;
            //}

            //if (!EfemDevice.EmoAlarm.IsAcknowledged)
            //{
            //    EfemDevice.ClearError();
            //    return false;
            //}

            return true;
        }

        private bool FsmStartHome(object[] param)
        {
            Result ret = StartRoutine(_fakeRoutine);
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
                OP.DoOperation($"{Module}.ResetTask");
                return true;
            }

            return false;
        }

        private bool FsmAbortTask(object[] param)
        {
            AbortRoutine();
            RobotDevice.RobotReset();
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

        private bool FsmStartRobotHome(object[] param)
        {
            Result ret = StartRoutine(_fakeRoutine);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;

            _isStartTMRobotHome = true;
            IsTMRobotHomed = false;

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

            if (_isStartTMRobotHome)
            {
                _isStartTMRobotHome = false;
                IsTMRobotHomed = true;
                OP.DoOperation($"{Module}.ResetTask");
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
        

        public override bool Home(out string reason)
        {
            CheckToPostMessage((int)MSG.Home);
            reason = string.Empty;
            return true;
        }
        

        private void TMDevice_OnDeviceAlarmStateChanged(string module, AlarmEventItem alarmItem)
        {
            if (IsInit)
                return;

            if (!alarmItem.IsAcknowledged)
            {
                if (alarmItem.Level == EventLevel.Warning)
                {
                    EV.PostWarningLog(Module, alarmItem.Description);
                }
                else
                {
                    EV.PostAlarmLog(Module, alarmItem.Description);
                }

                //if (alarmItem.EventEnum == TMDevice.IsMaintain.EventEnum
                //|| alarmItem.EventEnum == TMDevice.EmoAlarm.EventEnum
                //|| alarmItem.EventEnum == TMDevice.DoorOpen.EventEnum)
                //{
                //    isInit = false;
                //    PostMsg(MSG.Error);
                //}
                //else
                if (alarmItem.Level == EventLevel.Alarm)
                {
                    PostMsg(MSG.Error);
                }
            }
            else
            {
                if (IsError)
                    CheckToPostMessage((int)MSG.Reset);
            }
        }

        public int InvokeError()
        {
            if (CheckToPostMessage((int)MSG.Error))
                return (int)MSG.Error;

            return (int)FSM_MSG.NONE;
        }

        public override bool CheckAcked(int fsmTaskToken)
        {
            return FsmState == (int)STATE.Idle && CheckAllMessageProcessed();
            //return true;
        }
    }
}
