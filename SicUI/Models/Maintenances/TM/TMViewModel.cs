using System.Collections.Generic;
using System.Windows;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using SicUI.Models;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using Caliburn.Micro;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using OpenSEMI.ClientBase;
using MECF.Framework.UI.Client.ClientBase;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DataCenter;
using MECF.Framework.Common.DBCore;
using System;

namespace SicUI.Client.Models.Platform.TM
{
    public class TMViewModel : SicUIViewModelBase
    {
        [Subscription("TM.Status")]
        public string Status { get; set; }


        [Subscription("Rt.Status")]
        public string RtStatus { get; set; }

        public bool EnableWaferClick => RtStatus == "Idle";
        public bool LLEnableWaferClick => true;

        public bool LoadValveIsEnable => !LLIsOnline;
        public bool UnLoadValveIsEnable => !UnLoadIsOnline;

        public bool TMValveIsEnable => !TMIsOnline;

        public bool PM1TMValveIsEnable => !PM1IsOnline && !TMIsOnline;

        public bool LoadTMValveIsEnable => !LLIsOnline && !TMIsOnline;
        public bool UnloadTMValveIsEnable => !UnLoadIsOnline && !TMIsOnline;


        #region Properties

        public List<string> InstallPMs { get; set; }

        private int _PickSelectPM = 0;
        public int PickSelectPM
        {
            get { return _PickSelectPM; }
            set { _PickSelectPM = value; NotifyOfPropertyChange("PickSelectPM"); }
        }

        private int _PlaceSelectPM = 0;
        public int PlaceSelectPM
        {
            get { return _PlaceSelectPM; }
            set { _PlaceSelectPM = value; NotifyOfPropertyChange("PlaceSelectPM"); }
        }

        public bool IsSelectedSlitValveOpen
        {
            get
            {
                if (SlitValveSelectedModule == "LoadLock")
                {
                    return IsLLDoorOpen;
                }
                else if (SlitValveSelectedModule == "Buffer")
                {
                    return IsBufferDoorOpen;
                }
                else if (SlitValveSelectedModule == "UnLoad")
                {
                    return IsUnLoadDoorOpen;
                }
                else if (SlitValveSelectedModule == "PM1")
                {
                    return IsPM1DoorOpen;
                }
                else if (SlitValveSelectedModule == "PM2")
                {
                    return IsPM2DoorOpen;
                }
                return false;
            }
        }
        public bool IsSelectedSlitValveClose
        {
            get
            {
                if (SlitValveSelectedModule == "LoadLock")
                {
                    return !IsLLDoorOpen;
                }
                else if (SlitValveSelectedModule == "Buffer")
                {
                    return !IsBufferDoorOpen;
                }
                else if (SlitValveSelectedModule == "UnLoad")
                {
                    return !IsUnLoadDoorOpen;
                }
                else if (SlitValveSelectedModule == "PM1")
                {
                    return !IsPM1DoorOpen;
                }
                else if (SlitValveSelectedModule == "PM2")
                {
                    return !IsPM2DoorOpen;
                }
                return false;
            }
        }


        public FoupDoorState LLDoorState
        {
            get
            {
                {
                    if (LLDoorOpenFeedback && !LLDoorCloseFeedback) return FoupDoorState.Open;
                    if (!LLDoorOpenFeedback && LLDoorCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }

        public FoupDoorState UnLoadDoorState
        {
            get
            {
                {
                    if (UnLoadDoorOpenFeedback && !UnLoadDoorCloseFeedback) return FoupDoorState.Open;
                    if (!UnLoadDoorOpenFeedback && UnLoadDoorCloseFeedback) return FoupDoorState.Close; 
                }

                return FoupDoorState.Unknown;
            }
        }




        public FoupDoorState PM1DoorState
        {
            get
            {
                {
                    if (PM1SlitValveOpenFeedback && !PM1SlitValveCloseFeedback) return FoupDoorState.Open;
                    if (!PM1SlitValveOpenFeedback && PM1SlitValveCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }

        public FoupDoorState BufferDoorState
        {
            get
            {
                {
                    if (TMBufferDoorOpenFeedback && !TMBufferDoorCloseFeedback) return FoupDoorState.Open;
                    if (!TMBufferDoorOpenFeedback && TMBufferDoorCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }

        public FoupDoorState PM2DoorState
        {
            get
            {
                {
                    if (PM2SlitValveOpenFeedback && !PM2SlitValveCloseFeedback) return FoupDoorState.Open;
                    if (!PM2SlitValveOpenFeedback && PM2SlitValveCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }


        [Subscription("TMRobot.CmdBladeTarget")]
        public string TMRobotBladeTarget
        {
            get;
            set;
        }

        [Subscription("TMRobot.CmdBlade1Extend")]
        public string TMArmAExtended
        {
            get;
            set;
        }

        [Subscription("TMRobot.CmdBlade2Extend")]
        public string TMArmBExtended
        {
            get;
            set;
        }

        [Subscription("TMRobot.RobotMoveInfo")]
        public RobotMoveInfo TmRobotMoveInfo
        {
            get;
            set;
        }

        [Subscription("TMRobot.ModuleWaferList")]
        public Aitex.Core.Common.WaferInfo[] TMRobotWafers { get; set; }

        [Subscription("TM.Status")]
        public string TMStatus { get; set; }


        [Subscription("TM.ChamberPressure")]
        public double TMChamberPressure { get; set; }


        [Subscription("TM.IsAtm")]
        public bool IsTMAtmFeedback { get; set; }

        [Subscription("TM.IsVacuum")]
        public bool IsTMVacuumFeedback { get; set; }

        [Subscription("TM.CurrentRoutineLoop")]
        public int TMCurrentLoop { get; set; }

        [Subscription("TM.CurrentRoutineLoopTotal")]
        public int TMTotalLoop { get; set; }

        public string TMPurgeLoop => $"Purge Loop: {TMCurrentLoop}/{TMTotalLoop}";

        [Subscription("TM.PM1Door.OpenFeedback")]
        public bool PM1SlitValveOpenFeedback { get; set; }

        [Subscription("TM.PM1Door.CloseFeedback")]
        public bool PM1SlitValveCloseFeedback { get; set; }

        [Subscription("TM.PM2Door.OpenFeedback")]
        public bool PM2SlitValveOpenFeedback { get; set; }

        [Subscription("TM.PM2Door.CloseFeedback")]
        public bool PM2SlitValveCloseFeedback { get; set; }

        [Subscription("TM.BufferDoor.OpenFeedback")]
        public bool TMBufferDoorOpenFeedback { get; set; }

        [Subscription("TM.BufferDoor.CloseFeedback")]
        public bool TMBufferDoorCloseFeedback { get; set; }



        [Subscription("TM.LoadLockDoor.OpenFeedback")]
        public bool LLDoorOpenFeedback { get; set; }

        [Subscription("TM.LoadLockDoor.CloseFeedback")]
        public bool LLDoorCloseFeedback { get; set; }

        [Subscription("TM.UnLoadDoor.OpenFeedback")]
        public bool UnLoadDoorOpenFeedback { get; set; }

        [Subscription("TM.UnLoadDoor.CloseFeedback")]
        public bool UnLoadDoorCloseFeedback { get; set; }



        public string TMStatusBackground
        {
            get { return GetUnitStatusBackground(TMStatus); }
        }


        private List<string> _modules = new List<string>() { "PM1", "PM2", "LoadLock", "UnLoad", "Buffer.01", "Buffer.02", "Buffer.03" };
        public List<string> Modules
        {
            get { return _modules; }
            set { _modules = value; NotifyOfPropertyChange("Modules"); }
        }

        private List<string> _modulesSlitValve = new List<string>() { "PM1", "PM2", "LoadLock", "UnLoad", "Buffer"};
        public List<string> ModulesSlitValve
        {
            get { return _modulesSlitValve; }
            set { _modulesSlitValve = value; NotifyOfPropertyChange("ModulesSlitValve"); }
        }


        private string _slitValveSelectedModule;
        public string SlitValveSelectedModule
        {
            get { return _slitValveSelectedModule; }
            set
            {
                _slitValveSelectedModule = value;
                NotifyOfPropertyChange("SlitValveSelectedModule");
            }
        }

        private string _pickSelectedModule;
        public string PickSelectedModule
        {
            get { return _pickSelectedModule; }
            set
            {
                _pickSelectedModule = value;
                NotifyOfPropertyChange("PickSelectedModule");
            }
        }

        public bool IsPM1Installed { get; set; }
        public bool IsPM2Installed { get; set; }
        public bool IsBufferInstalled { get; set; }
        public bool IsLLInstalled { get; set; }
        public bool IsUnLoadInstalled { get; set; }
        public bool IsTMInstalled { get; set; }


        #endregion

        #region TM

        [Subscription("TM.LoadSlowPump.DeviceData")]
        public AITValveData LLSlowRough { get; set; }

        [Subscription("TM.LoadFastPump.DeviceData")]
        public AITValveData LLFastRough { get; set; }

        [Subscription("TM.TMSlowPump.DeviceData")]
        public AITValveData TMSlowRough { get; set; }

        [Subscription("TM.TMFastPump.DeviceData")]
        public AITValveData TMFastRough { get; set; }

        [Subscription("TM.UnLoadSlowPump.DeviceData")]
        public AITValveData UnLoadSlowRough { get; set; }

        [Subscription("TM.UnLoadFastPump.DeviceData")]
        public AITValveData UnLoadFastRough { get; set; }

        [Subscription("TM.BufferVent.DeviceData")]
        public AITValveData BufferVent { get; set; }

        [Subscription("TM.LoadVent.DeviceData")]
        public AITValveData LoadLockVent { get; set; }

        [Subscription("TM.TMVent.DeviceData")]
        public AITValveData TMVent { get; set; }

        [Subscription("TM.UnLoadVent.DeviceData")]
        public AITValveData UnLoadVent { get; set; }

        [Subscription("TM.TMLoadBanlance.DeviceData")]
        public AITValveData TMLoadBanlance { get; set; }

        [Subscription("TM.TMUnLoadBanlance.DeviceData")]
        public AITValveData TMUnLoadBanlance { get; set; }

        [Subscription("PM1.V70.DeviceData")]
        public AITValveData TMPMABanlance { get; set; }

        //[Subscription("PM2.TMPMBBanlance.DeviceData")]
        //public AITValveData TMPMBBanlance { get; set; }

        [Subscription("TM.TMPump1.DeviceData")]
        public AITPumpData PumpData { get; set; }

        [Subscription("TM.TMPump2.DeviceData")]
        public AITPumpData PumpData2 { get; set; }

        [Subscription("TM.LLTrayPresence.DeviceData")]
        public AITSensorData LLTrayPresence { get; set; }

        [Subscription("TM.UnLoadWaferPlaced.DeviceData")]
        public AITSensorData UnLoadTrayPresence { get; set; }

        [Subscription("PM1.V70.DeviceData")]
        public AITValveData TMPressBalance1 { get; set; }

        [Subscription("UnLoad.RemainedCoolingTime")]
        public int UnLoadCoolingTime { get; set; }

        [Subscription("Scheduler.TimeBuffer1")]
        public string TimeBuffer1 { get; set; }
        public double sTimeBuffer1 => TimeBuffer1 == null || TimeBuffer1.Length == 0 ? 0 : Double.Parse(TimeBuffer1);

        [Subscription("Scheduler.TimeBuffer2")]
        public string TimeBuffer2 { get; set; }
        public double sTimeBuffer2 => TimeBuffer2 == null || TimeBuffer2.Length == 0 ? 0 : Double.Parse(TimeBuffer2);

        [Subscription("Scheduler.TimeBuffer3")]
        public string TimeBuffer3 { get; set; }
        public double sTimeBuffer3 => TimeBuffer3 == null || TimeBuffer3.Length == 0 ? 0 : Double.Parse(TimeBuffer3);


        public bool IsLLDoorOpen => LLDoorState == FoupDoorState.Open ? true : false;
        public bool IsBufferDoorOpen => BufferDoorState == FoupDoorState.Open ? true : false;
        public bool IsUnLoadDoorOpen => UnLoadDoorState == FoupDoorState.Open ? true : false;
        public bool IsPM1DoorOpen => PM1DoorState == FoupDoorState.Open ? true : false;
        public bool IsPM2DoorOpen => PM2DoorState == FoupDoorState.Open ? true : false;

        public Visibility IsPM1Water => PM1Wafer == null ? Visibility.Hidden : PM1Wafer.IsVisibility;
        public Visibility IsPM2Water => PM2Wafer == null ? Visibility.Hidden : PM2Wafer.IsVisibility;
        public Visibility IsLLWater => LoadLockWafer == null ? Visibility.Hidden : LoadLockWafer.IsVisibility;
        public Visibility IsBufferWater => BufferWafer == null ? Visibility.Hidden : BufferWafer.IsVisibility;



        [Subscription("TM.TMLid.Status")]
        public string TMLidStatus { get; set; }

        [Subscription("TM.TMPressure.DeviceData")]
        public AITPressureMeterData TMPressure { get; set; }

        [Subscription("TM.LLPressure.DeviceData")]
        public AITPressureMeterData LLPressure { get; set; }

        [Subscription("TM.UnLoadPressure.DeviceData")]
        public AITPressureMeterData UnLoadPressure { get; set; }

        [Subscription("TM.ForelinePressure.DeviceData")]
        public AITPressureMeterData ForelinePressure { get; set; }


        public string TMPressureDisplay => TMPressure.Display;
        public string LLPressureDisplay => LLPressure.Display;
        public string UnLoadPressureDisplay => UnLoadPressure.Display;
        public string ForelinePressureDisplay => ForelinePressure.Display;

        [Subscription("LL.LLTemp.FeedBack")]
        public float LLTemperature { get; set; }

        [Subscription("UnLoad.UnLoadTemp.FeedBack")]
        public float UnLoadTemperature { get; set; }

        [Subscription("TMRobot.IsHomed")]
        public bool IsTMRobotHomed { get; set; }

        [Subscription("TM.IsOnline")]
        public bool TMIsOnline { get; set; }

        public string TMOnlineMode => TMIsOnline ? "Online" : "Offline";
        public bool IsTMEnableManualOperation => !TMIsOnline && TMStatus == "Idle";
        public bool IsTMOnlineButtonEnable => !TMIsOnline && TMStatus == "Idle";
        public bool IsTMOfflineButtonEnable => TMIsOnline && TMStatus == "Idle";

        [Subscription("TMRobot.RobotState")]
        public string RobotState { get; set; }

        [Subscription("TM.Mfc60.DeviceData")]
        public AITMfcData Mfc60Data { get; set; }


        [Subscription("TM.Mfc61.DeviceData")]
        public AITMfcData Mfc61Data { get; set; }

        public ICommand CmdSetMfcFlow { get; set; }
        #endregion

        #region MoveSpeed
        private const double movespeed = 0.5;
        private const double movespeed2 = -0.5;
        public double V77MoveSpeed => TMVent.Feedback ? movespeed : 0;
        public double V77MoveSpeed2 => TMVent.Feedback ? movespeed2 : 0;
        public double V78MoveSpeed => UnLoadVent.Feedback ? movespeed : 0;
        public double V78MoveSpeed2 => UnLoadVent.Feedback ? movespeed2 : 0;
        public double V79MoveSpeed => LoadLockVent.Feedback ? movespeed : 0;
        public double V79MoveSpeed2 => LoadLockVent.Feedback ? movespeed2 : 0;
        public double V80MoveSpeed => BufferVent.Feedback ? movespeed : 0;
        public double V80MoveSpeed2 => BufferVent.Feedback ? movespeed2 : 0;

        public double V122MoveSpeed => UnLoadFastRough.Feedback ? movespeed : 0;
        public double V122MoveSpeed2 => UnLoadFastRough.Feedback ? movespeed2 : 0;
        public double V123MoveSpeed => UnLoadSlowRough.Feedback ? movespeed : 0;
        public double V123MoveSpeed2 => UnLoadSlowRough.Feedback ? movespeed2 : 0;
        public double V81MoveSpeed => TMFastRough.Feedback ? movespeed : 0;
        public double V81MoveSpeed2 => TMFastRough.Feedback ? movespeed2 : 0;
        public double V82MoveSpeed => TMSlowRough.Feedback ? movespeed : 0;
        public double V82MoveSpeed2 => TMSlowRough.Feedback ? movespeed2 : 0;
        public double V83MoveSpeed => LLFastRough.Feedback ? movespeed : 0;
        public double V83MoveSpeed2 => LLFastRough.Feedback ? movespeed2 : 0;
        public double V84MoveSpeed => LLSlowRough.Feedback ? movespeed : 0;
        public double V84MoveSpeed2 => LLSlowRough.Feedback ? movespeed2 : 0;
        public double V701MoveSpeed => TMPMABanlance.Feedback ? movespeed : 0;
        public double V701MoveSpeed2 => TMPMABanlance.Feedback ? movespeed2 : 0;
        public double V702MoveSpeed => 0;// TMPMBBanlance.Feedback ? movespeed : 0;
        public double V702MoveSpeed2 => 0;// TMPMBBanlance.Feedback ? movespeed2 : 0;
        public double V85MoveSpeed => TMLoadBanlance.Feedback ? movespeed : 0;
        public double V85MoveSpeed2 => TMLoadBanlance.Feedback ? movespeed2 : 0;
        public double V124MoveSpeed => TMUnLoadBanlance.Feedback ? movespeed : 0;
        public double V124MoveSpeed2 => TMUnLoadBanlance.Feedback ? movespeed2 : 0;


        public double V77V78MoveSpeed => TMVent.Feedback || UnLoadVent.Feedback ? movespeed : 0;
        public double V77V78MoveSpeed2 => TMVent.Feedback || UnLoadVent.Feedback ? movespeed2 : 0;
        public double V79V80MoveSpeed => BufferVent.Feedback || LoadLockVent.Feedback ? movespeed : 0;
        public double V79V80MoveSpeed2 => BufferVent.Feedback || LoadLockVent.Feedback ? movespeed2 : 0;
        public double V77V78V79V80MoveSpeed => TMVent.Feedback || UnLoadVent.Feedback || BufferVent.Feedback || LoadLockVent.Feedback ? movespeed : 0;
        public double V77V78V79V80MoveSpeed2 => TMVent.Feedback || UnLoadVent.Feedback || BufferVent.Feedback || LoadLockVent.Feedback ? movespeed2 : 0;


        public double V122V123MoveSpeed => UnLoadFastRough.Feedback || UnLoadSlowRough.Feedback ? movespeed : 0;
        public double V122V123MoveSpeed2 => UnLoadFastRough.Feedback || UnLoadSlowRough.Feedback ? movespeed2 : 0;
        public double V81V82MoveSpeed => TMFastRough.Feedback || TMSlowRough.Feedback ? movespeed : 0;
        public double V81V82MoveSpeed2 => TMFastRough.Feedback || TMSlowRough.Feedback ? movespeed2 : 0;
        public double V83V84MoveSpeed => LLFastRough.Feedback || LLSlowRough.Feedback ? movespeed : 0;
        public double V83V84MoveSpeed2 => LLFastRough.Feedback || LLSlowRough.Feedback ? movespeed2 : 0;

        public double V83V84V122V123MoveSpeed => LLFastRough.Feedback || LLSlowRough.Feedback || UnLoadFastRough.Feedback || UnLoadSlowRough.Feedback ? movespeed : 0;
        public double V83V84V122V123MoveSpeed2 => LLFastRough.Feedback || LLSlowRough.Feedback || UnLoadFastRough.Feedback || UnLoadSlowRough.Feedback ? movespeed2 : 0;


        #endregion

        #region Functions
        public TMViewModel()
        {
            this.DisplayName = "TM";

            IsPM1Installed = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM1Installed");
            IsPM2Installed = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM2Installed");
            IsBufferInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsBufferInstalled");
            IsLLInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsLoadLockInstalled");
            IsTMInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsTMInstalled");
            IsUnLoadInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsUnLoadInstalled");

            CmdSetMfcFlow = new DelegateCommand<object>(PerformCmdSetMfcFlow);
        }

        private void PerformCmdSetMfcFlow(object param)
        {
            object[] args = (object[])param; //0:devicename, 1:operation, 2:args
            if (args.Length == 3)
            {
                InvokeClient.Instance.Service.DoOperation($"{args[0]}.Ramp", args[2]);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            base.InitLL();
            base.InitTM();
            base.InitEFEM();
            base.InitPM();
            base.InitAligner();


            PickSelectedModule = "PM1";
            SlitValveSelectedModule = "PM1";


            ActiveUpdateData = true;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
        }

        #endregion

        #region TM Robot
        public void TMRobot_Home()
        {
            TMProvider.Instance.TMRobot_Home();
        }

        public void TMRobot_Pick()
        {
            if (PickSelectedModule != null )
            {
                if (PickSelectedModule == "Buffer.01")
                {
                    TMProvider.Instance.TMRobot_Pick("Buffer", 0, 0);
                }
                else if (PickSelectedModule == "Buffer.02")
                {
                    TMProvider.Instance.TMRobot_Pick("Buffer", 1, 0);
                }
                else if (PickSelectedModule == "Buffer.03")
                {
                    TMProvider.Instance.TMRobot_Pick("Buffer", 2, 0);
                }
                else
                {
                    TMProvider.Instance.TMRobot_Pick(PickSelectedModule, 0, 0);
                }
            }
        }

        public void TMRobot_Place()
        {
            if (PickSelectedModule != null )
            {
                if (PickSelectedModule == "Buffer.01")
                {
                    TMProvider.Instance.TMRobot_Place("Buffer", 0, 0);
                }
                else if (PickSelectedModule == "Buffer.02")
                {
                    TMProvider.Instance.TMRobot_Place("Buffer", 1, 0);
                }
                else if (PickSelectedModule == "Buffer.03")
                {
                    TMProvider.Instance.TMRobot_Place("Buffer", 2, 0);
                }
                else 
                {
                    TMProvider.Instance.TMRobot_Place(PickSelectedModule, 0, 0);
                }
            }
        }
        #endregion

        #region LoadLock
        [Subscription("LoadLock.IsOnline")]
        public bool LLIsOnline { get; set; }

        [Subscription("LoadLock.Status")]
        public string LLStatus { get; set; }

        [Subscription("LoadLock.LidState")]
        public string LLLidState { get; set; }

        [Subscription("LoadLock.LidClosed")]
        public string LidClosed { get; set; }



        public string LLOnlineMode => LLIsOnline ? "Online" : "Offline";
        public bool IsLLEnableManualOperation => !LLIsOnline && LLStatus == "Idle";
        public bool IsLLOnlineButtonEnable => !LLIsOnline && LLStatus == "Idle";
        public bool IsLLOfflineButtonEnable => LLIsOnline && LLStatus == "Idle";

        [Subscription("LoadLock.CurrentRoutineLoop")]
        public int LLBCurrentLoop { get; set; }

        [Subscription("LoadLock.CurrentRoutineLoopTotal")]
        public int LLBTotalLoop { get; set; }

        public string LLBPurgeLoop => $"Purge Loop: {LLBCurrentLoop}/{LLBTotalLoop}";


        #endregion

        #region UnLoad
        [Subscription("UnLoad.IsOnline")]
        public bool UnLoadIsOnline { get; set; }

        [Subscription("UnLoad.Status")]
        public string UnLoadStatus { get; set; }

        [Subscription("UnLoad.LidState")]
        public string UnLoadLidState { get; set; }



        public string UnLoadOnlineMode => UnLoadIsOnline ? "Online" : "Offline";
        public bool IsUnLoadEnableManualOperation => !UnLoadIsOnline && UnLoadStatus == "Idle";
        public bool IsUnLoadOnlineButtonEnable => !UnLoadIsOnline && UnLoadStatus == "Idle";
        public bool IsUnLoadOfflineButtonEnable => UnLoadIsOnline && UnLoadStatus == "Idle";

        [Subscription("UnLoad.CurrentRoutineLoop")]
        public int UnLoadBCurrentLoop { get; set; }

        [Subscription("UnLoad.CurrentRoutineLoopTotal")]
        public int UnLoadBTotalLoop { get; set; }

        public string UnLoadPurgeLoop => $"Purge Loop: {UnLoadBCurrentLoop}/{UnLoadBTotalLoop}";


        #endregion

        #region Buffer

        [Subscription("Buffer.IsHomed")]
        public bool IsBufferRobotHomed { get; set; }

        [Subscription("Buffer.IsOnline")]
        public bool BufferIsOnline { get; set; }

        [Subscription("Buffer.Status")]
        public string BufferStatus { get; set; }

        [Subscription("TM.BufferWaferHigh.DeviceData")]
        public AITSensorData BufferTrayPresenceHigh { get; set; }

        [Subscription("TM.BufferWaferMiddle.DeviceData")]
        public AITSensorData BufferTrayPresenceMiddle { get; set; }

        [Subscription("TM.BufferWaferLow.DeviceData")]
        public AITSensorData BufferTrayPresenceLow { get; set; }

        [Subscription("TM.P116PIDTC.PVInValue")]
        public float BufferTempPV { get; set; }

        [Subscription("TM.P116PIDTC.TargetSP")]
        public float BufferTempTargetSP { get; set; }

        [Subscription("TM.P116PIDTC.AM")]
        public int BufferTempAM { get; set; }
        public bool BufferTempAMAuto => BufferTempAM == 0;


        public string BufferOnlineMode => BufferIsOnline ? "Online" : "Offline";
        public bool IsBufferEnableManualOperation => !BufferIsOnline && BufferStatus == "Idle";
        public bool IsBufferOnlineButtonEnable => !BufferIsOnline && BufferStatus == "Idle";
        public bool IsBufferOfflineButtonEnable => BufferIsOnline && BufferStatus == "Idle";


        #endregion

        #region PM1
        [Subscription("PM1.Status")]
        public string StatusPM1 { get; set; }
        public bool PM1IsIdel => StatusPM1 == "Idle" || StatusPM1 == "Safety";

        [Subscription("PM1.IsOnline")]
        public bool PM1IsOnline { get; set; }
        public string PM1OnlineMode => PM1IsOnline ? "Online" : "Offline";

        [Subscription("PM1.PT1.FeedBack")]
        public double PM1Pressure { get; set; }

        [Subscription("PM1.TC1.L2PVFeedBack")]
        public float PM1Temprature { get; set; }

        [Subscription("PM1.PMServo.ActualSpeedFeedback")]
        public float PM1Rotation { get; set; }

        [Subscription("PM1.TC1.L1InputTempSetPoint")]
        public float PM1L1InputTemp { get; set; }

        [Subscription("PM1.TC1.L2InputTempSetPoint")]
        public float PM1L2InputTemp { get; set; }

        [Subscription("PM1.TC1.L3InputTempSetPoint")]
        public float PM1L3InputTemp { get; set; }

        [Subscription("PM1.ServoStates")]
        public string ServoStatePM1 { get; set; }

        [Subscription("PM1.CurrentRoutineLoop")]
        public int PM1CurrentLoop { get; set; }

        [Subscription("PM1.CurrentRoutineLoopTotal")]
        public int PM1TotalLoop { get; set; }

        public string PM1PurgeLoop => $"Purge Loop: {PM1CurrentLoop}/{PM1TotalLoop}";

        [Subscription("PM1.ConfinementRing.RingUpSensor")]
        public bool PM1ConfinementRingUpSensor { get; set; }

        [Subscription("PM1.ConfinementRing.RingDownSensor")]
        public bool PM1ConfinementRingDwonSensor { get; set; }

        public string ConfinementState
        {
            get
            {
                if (PM1ConfinementRingUpSensor && !PM1ConfinementRingDwonSensor)
                {
                    return "Up";
                }
                else if (!PM1ConfinementRingUpSensor && PM1ConfinementRingDwonSensor)
                {
                    return "Down";
                }
                else
                {
                    return "UnKonw";
                }
            }
        }

        public bool PM1OnlineButtonEnable => !PM1IsOnline;
        public bool PM1OfflineButtonEnable => PM1IsOnline;


        #endregion

        //#region PM2
        //[Subscription("PM1.Status")]
        //public string StatusPM2 { get; set; }
        //public bool PM2IsIdel => StatusPM2 == "Idle" || StatusPM2 == "Safety";

        //[Subscription("PM2.IsOnline")]
        //public bool PM2IsOnline { get; set; }
        //public string PM2OnlineMode => PM2IsOnline ? "Online" : "Offline";

        //[Subscription("PM2.PT1.FeedBack")]
        //public double PM2Pressure { get; set; }

        //[Subscription("PM2.TC1.L2PVFeedBack")]
        //public float PM2Temprature { get; set; }

        //[Subscription("PM2.PMServo.ActualSpeedFeedback")]
        //public float PM2Rotation { get; set; }

        //[Subscription("PM2.TC1.L1InputTempSetPoint")]
        //public float PM2L1InputTemp { get; set; }

        //[Subscription("PM2.TC1.L2InputTempSetPoint")]
        //public float PM2L2InputTemp { get; set; }

        //[Subscription("PM2.TC1.L3InputTempSetPoint")]
        //public float PM2L3InputTemp { get; set; }

        //[Subscription("PM2.ServoStates")]
        //public string ServoStatePM2 { get; set; }

        //[Subscription("PM2.CurrentRoutineLoop")]
        //public int PM2CurrentLoop { get; set; }

        //[Subscription("PM1.CurrentRoutineLoopTotal")]
        //public int PM2TotalLoop { get; set; }

        //public string PM2PurgeLoop => $"Purge Loop: {PM2CurrentLoop}/{PM2TotalLoop}";

        //[Subscription("PM2.ConfinementRing.RingUpFaceback")]
        //public bool IsConfinementRingUpPM2 { get; set; }
        //[Subscription("PM2.ConfinementRing.RingDownFaceback")]
        //public bool IsConfinementRingDownPM2 { get; set; }

        //[Subscription("PM2.ConfinementRing.RingDownSetpoint")]
        //public bool RingDownSetpointPM2 { get; set; }
        //[Subscription("PM2.ConfinementRing.RingUpSetpoint")]
        //public bool RingUpSetpointPM2 { get; set; }

        //[Subscription("PM2.NAISServo.AlarmStatus")]
        //public bool IsAlarmStatusPM2 { get; set; }

        //public bool PM2OnlineButtonEnable => !PM2IsOnline;
        //public bool PM2OfflineButtonEnable => PM2IsOnline;

        //public bool EnableRingUpPM2 => IsConfinementRingDownPM2 && !RingUpSetpointPM2 && PM2IsIdel && !IsAlarmStatusPM2 && StatusPM2 == "Error";
        //public bool EnableRingDownPM2 => IsConfinementRingUpPM2 && !RingDownSetpointPM2 && PM2IsIdel && !IsAlarmStatusPM2 && StatusPM2 == "Error";
        //public string RingStatusPM2
        //{
        //    get
        //    {
        //        return ServoStatePM2;
        //    }
        //}
        //#endregion

        #region OP
        public void Reset(string module)
        {
            if(module =="EFEM")
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.Reset");
            }
            else
            {
                TMProvider.Instance.Reset(module);
            }
        }
        public void Home(string module)
        {
            if(module == "EFEM")
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.Home");
            }
            else
            {
                TMProvider.Instance.Home(module);
            }
        }
        public void Pump(string module)
        {
            TMProvider.Instance.Pump(module);
        }
        public void Vent(string module)
        {
            //TM Vent 时需弹窗提醒Buffer腔温度,提示用户小心Buffer腔开盖烫伤

            if(module == "TM")
            {
                if (BufferTempPV >= (double)QueryDataClient.Instance.Service.GetConfig("Buffer.TMVentBufferTempMax"))
                {
                    EV.PostWarningLog("TM", "Buffer Temp high,TM can not Vent");
                    return;
                }

                var selection = DialogBox.ShowDialog(DialogButton.OK | DialogButton.Cancel, DialogType.WARNING,
                $"Buffer temperature is {BufferTempPV}℃. If want open Buffer Lid, there is a risk of scalding. Please pay special attention!");
                if (selection == DialogButton.OK)
                {
                    TMProvider.Instance.Vent(module);
                }
            }
            else
            {
                TMProvider.Instance.Vent(module);
            }
        }
        public void ServoToLL(string module)
        {
            TMProvider.Instance.ServoToLL(module);
        }

        public void RobotExtend()
        {
            InvokeClient.Instance.Service.DoOperation($"TMRobot.SetPosition", PickSelectedModule.Split('.')[0]);
        }

        public void Purge(string module)
        {
            PurgeDialogViewModel dialog = new PurgeDialogViewModel();
            dialog.DisplayName = $"Confirm {module} Purge Setting";


            object config = QueryDataClient.Instance.Service.GetConfig($"{module}.Purge.CyclePurgeCount");
            if (config != null)
            {
                dialog.CycleCount = config.ToString();
            }
            config = QueryDataClient.Instance.Service.GetConfig($"{module}.Purge.PumpBasePressure");
            if (config != null)
            {
                dialog.PumpPressure = config.ToString();
            }
            config = QueryDataClient.Instance.Service.GetConfig($"{module}.Purge.VentBasePressure");
            if (config != null)
            {
                dialog.VentPressure = config.ToString();
            }


            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if (!bret.HasValue || !bret.Value)
                return;

            InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"{module}.Purge.CyclePurgeCount", dialog.CycleCountValue);
            InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"{module}.Purge.PumpBasePressure", dialog.PumpPressureValue);
            InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"{module}.Purge.VentBasePressure", dialog.VentPressureValue);

            TMProvider.Instance.Purge(module);
        }
        public void LiftUp(string module)
        {
            TMProvider.Instance.LiftUp(module);
        }
        public void LiftDown(string module)
        {
            TMProvider.Instance.LiftDown(module);
        }
        public void OpenDoor(string module)
        {
            TMProvider.Instance.OpenDoor(module);
        }
        public void CloseDoor(string module)
        {
            TMProvider.Instance.CloseDoor(module);
        }
        public void OpenSlitValve(string module)
        {
            TMProvider.Instance.OpenSlitValve(module);
        }
        public void CloseSlitValve(string module)
        {
            TMProvider.Instance.CloseSlitValve(module);
        }
        public void Abort(string module)
        {
            if(module == "EFEM")
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.Abort");
            }
            else
            {
                TMProvider.Instance.Abort(module);
            }
           
        }
        public void PrepareTransfer(string module, string type)
        {
            TMProvider.Instance.PrepareTransfer(module, type);
        }
        public void TransferHandoff(string module, string type)
        {
            TMProvider.Instance.TransferHandoff(module, type);
        }
        public void SetOnline(string module)
        {
            if (MessageBoxResult.Yes == MessageBox.Show($"Set {module} Online ?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.SetOnline");
            }
        }
        public void SetOffline(string module)
        {
            if (MessageBoxResult.Yes == MessageBox.Show($"Set {module} Offline ?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.SetOffline");
            }
        }
        public void LeakCheck(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.LeakCheck");
        }

        public void SetBufferTempTargetSP(int tempSP)
        {
            InvokeClient.Instance.Service.DoOperation("TM.P116PIDTC.WriteTargetSP",tempSP);
        }
        public void SetBufferTempAM(int iAMValue)
        {
            InvokeClient.Instance.Service.DoOperation($"TM.P116PIDTC.WriteAM",iAMValue);
        }

        public void Group(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Group");
        }

        public void LoadSeparate()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.Separate");
        }

        public void Separate(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Separate");
        }

       
        #endregion

        #region ConfinementRing

        public void RingMoveUpPos(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.ConfinementRing.MoveUpPos");
        }

        public void RingMoveDownPos(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.ConfinementRing.MoveDownPos");
        }

        public void RingHome(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.ConfinementRing.Home");
        }
        public void RingReset(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.ConfinementRing.Reset");
        }


        #endregion


        #region EFEM
        [Subscription("EFEM.Status")]
        public string EFEMStatus { get; set; }


        [Subscription("EFEM.IsOnline")]
        public bool EFEMIsOnline { get; set; }
        public bool EFEMIsOffline => !EFEMIsOnline;

        public bool IsEFEMEnableManualOperation => !EFEMIsOnline && EFEMStatus == "Idle";

        public void EFEMHomeRoutine(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Home");
        }
        public void EFEMAbortRoutine(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Abort");
        }
        public void SetModuleOnline(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.SetOnline");
        }
        public void SetModuleOffline(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.SetOffline");
        }
        #endregion






    }
}
