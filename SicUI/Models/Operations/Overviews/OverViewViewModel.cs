using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.Common.RecipeCenter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using System;
using Caliburn.Micro.Core;
using SicUI.Controls;
using OpenSEMI.ClientBase.Utility;
using System.Dynamic;

namespace SicUI.Models.Operations.Overviews
{
    public class JobListInfo : NotifiableItem
    {
        public int Index { get; set; }
        public string WaferID { get; set; }
        public string LotName { get; set; }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                InvokePropertyChanged();
            }
        }

        private string _currentPosition;
        public string CurrentPosition
        {
            get { return _currentPosition; }
            set
            {
                _currentPosition = value;
                InvokePropertyChanged();
            }
        }
        public string CreateTime { get; set; }
        public string RecipeName { get; set; }
    }

    public class OverViewViewModel : SicUIViewModelBase, IHandle<ShowCloseMonitorWinEvent>
    {
        #region Property   
        public ObservableCollection<JobListInfo> JobListInfoData { get; set; }

        private WaferAssociationInfo _cassALWaferAssociation;
        public WaferAssociationInfo CassALWaferAssociation
        {
            get { return _cassALWaferAssociation; }
            set { _cassALWaferAssociation = value; }
        }

        private WaferAssociationInfo _cassRLWaferAssociation;
        public WaferAssociationInfo CassARWaferAssociation
        {
            get { return _cassRLWaferAssociation; }
            set { _cassRLWaferAssociation = value; }
        }

       
        public bool IsPM1Installed { get; set; }
        public bool IsPM2Installed { get; set; }
        public bool IsBufferInstalled { get; set; }
        public bool IsLoadLockInstalled { get; set; }
        public bool IsUnLoadInstalled { get; set; }
        public bool IsTMInstalled { get; set; }

        private R_TRIG monitorVCEAJobStatusTrig = new R_TRIG();
        private R_TRIG monitorVCEBJobStatusTrig = new R_TRIG();
        [Subscription("CassAL.SlotSequenceList")]
        public List<string> CassALSlotSequenceList { get; set; }

        [Subscription("CassAR.SlotSequenceList")]
        public List<string> CassARSlotSequenceList { get; set; }


        public int CassALeft1 { get; set; }

        public int CassALeft2 { get; set; }

        public int CassARight1 { get; set; }

        public int CassARight2 { get; set; }

        public int CassBL1 { get; set; }
        public int CassBL2 { get; set; }


        #region Mainframe

        [Subscription("TMRobot.RobotMoveInfo")]
        public RobotMoveInfo TmRobotMoveInfo
        {
            get;
            set;
        }
        [Subscription("WaferRobot.RobotMoveInfo")]
        public RobotMoveInfo WaferRobotMoveInfo
        {
            get;
            set;
        }

        [Subscription("TrayRobot.RobotMoveInfo")]
        public RobotMoveInfo TrayRobotMoveInfo
        {
            get;
            set;
        }

        public FoupDoorState PM1DoorState
        {
            get
            {
                if (PM1SlitValveOpenFeedback && !PM1SlitValveCloseFeedback) return FoupDoorState.Open;
                if (!PM1SlitValveOpenFeedback && PM1SlitValveCloseFeedback) return FoupDoorState.Close;

                return FoupDoorState.Unknown;
            }
        }

        public FoupDoorState PM2DoorState
        {
            get
            {
                if (PM2SlitValveOpenFeedback && !PM2SlitValveCloseFeedback) return FoupDoorState.Open;
                if (!PM2SlitValveOpenFeedback && PM2SlitValveCloseFeedback) return FoupDoorState.Close;

                return FoupDoorState.Unknown;
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


        public FoupDoorState BufferDoorState
        {
            get
            {

                {
                    if (BufferDoorOpenFeedback && !BufferDoorCloseFeedback) return FoupDoorState.Open;
                    if (!BufferDoorOpenFeedback && BufferDoorCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }


        public FoupDoorState EfemUnLoadDoorState
        {
            get
            {
                {
                    if (UnLoadEfemDoorOpenFeedback && !UnLoadEfemDoorCloseFeedback) return FoupDoorState.Open;
                    if (!UnLoadEfemDoorOpenFeedback && UnLoadEfemDoorCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }

        public FoupDoorState EfemLLLeftDoorState
        {
            get
            {

                {
                    if (LLEfemLeftDoorOpenFeedback && !LLEfemLeftDoorCloseFeedback) return FoupDoorState.Open;
                    if (!LLEfemLeftDoorOpenFeedback && LLEfemLeftDoorCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }

        public FoupDoorState EfemLLRightDoorState
        {
            get
            {

                {
                    if (LLEfemRightDoorOpenFeedback && !LLEfemRightDoorCloseFeedback) return FoupDoorState.Open;
                    if (!LLEfemRightDoorOpenFeedback && LLEfemRightDoorCloseFeedback) return FoupDoorState.Close;
                }

                return FoupDoorState.Unknown;
            }
        }
        public bool IsEfemLLLeftDoorOpen => EfemLLLeftDoorState == FoupDoorState.Open ? true : false;
        public bool IsEfemLLRightDoorOpen => EfemLLRightDoorState == FoupDoorState.Open ? true : false;
        public bool IsEfemUnLoadDoorOpen => EfemUnLoadDoorState == FoupDoorState.Open ? true : false;

        public bool IsLLDoorOpen => LLDoorState == FoupDoorState.Open ? true : false;
        public bool IsBufferDoorOpen => BufferDoorState == FoupDoorState.Open ? true : false;
        public bool IsUnLoadDoorOpen => UnLoadDoorState == FoupDoorState.Open ? true : false;
        public bool IsPM1DoorOpen => PM1DoorState == FoupDoorState.Open ? true : false;
        public bool IsPM2DoorOpen => PM2DoorState == FoupDoorState.Open ? true : false;


        public Visibility IsPM1Water
        {
            get
            {
                if (PM1Wafer != null && JobListInfoData != null)
                {
                    for (int i = 0; i < JobListInfoData.Count; i++)
                    {
                        if (JobListInfoData[i].WaferID == PM1Wafer.WaferID)
                        {
                            JobListInfoData[i].Status = GetWaferStatue(PM1Wafer.WaferStatus);
                            JobListInfoData[i].CurrentPosition = "PM1";
                        }
                    }
                }
                return PM1Wafer == null ? Visibility.Hidden : PM1Wafer.IsVisibility;
            }
        }
        public Visibility IsPM2Water
        {
            get
            {
                if (PM2Wafer != null && JobListInfoData != null)
                {
                    for (int i = 0; i < JobListInfoData.Count; i++)
                    {
                        if (JobListInfoData[i].WaferID == PM2Wafer.WaferID)
                        {
                            JobListInfoData[i].Status = GetWaferStatue(PM2Wafer.WaferStatus);
                            JobListInfoData[i].CurrentPosition = "PM2";
                        }
                    }
                }
                return PM2Wafer == null ? Visibility.Hidden : PM2Wafer.IsVisibility;
            }
        }

        public Visibility IsLLWater
        {
            get
            {
                if (LoadLockWafer != null && JobListInfoData != null)
                {
                    for (int i = 0; i < JobListInfoData.Count; i++)
                    {
                        if (JobListInfoData[i].WaferID == LoadLockWafer.WaferID)
                        {
                            JobListInfoData[i].Status = GetWaferStatue(LoadLockWafer.WaferStatus);
                            JobListInfoData[i].CurrentPosition = "LoadLock";
                        }
                    }
                }
                return LoadLockWafer == null ? Visibility.Hidden : LoadLockWafer.IsVisibility;
            }
        }
        public Visibility IsBufferWater
        {
            get
            {
                if (BufferWafer != null && JobListInfoData != null)
                {
                    for (int i = 0; i < JobListInfoData.Count; i++)
                    {
                        if (JobListInfoData[i].WaferID == BufferWafer.WaferID)
                        {
                            JobListInfoData[i].Status = GetWaferStatue(BufferWafer.WaferStatus);
                            JobListInfoData[i].CurrentPosition = "Buffer";
                        }
                    }
                }
                return BufferWafer == null ? Visibility.Hidden : BufferWafer.IsVisibility;
            }
        }
        public Visibility IsArmWater
        {
            get
            {
                if (TMRobotWafer1 != null && JobListInfoData != null)
                {
                    for (int i = 0; i < JobListInfoData.Count; i++)
                    {
                        if (JobListInfoData[i].WaferID == TMRobotWafer1.WaferID)
                        {
                            //JobListInfoData[i].Status = MECF.Framework.Common.SubstrateTrackings.WaferManager.Instance.AllLocationWafers[MECF.Framework.Common.Equipment.ModuleName.TMRobot][0].ProcessState.ToString();
                            JobListInfoData[i].CurrentPosition = "TM";
                        }
                    }
                }
                return TMRobotWafer1 == null ? Visibility.Hidden : TMRobotWafer1.IsVisibility;
            }
        }


        private string GetWaferStatue(int status)
        {
            if (status == 3)
            {
                return "InProcess";
            }

            if (status == 7 || status == 4)
            {
                return "Completed";
            }

            if (status == 3)
            {
                return "Wait";
            }

            if (status == 5)
            {
                return "Failed";
            }

            return "Idle";
        }




        [Subscription("TM.PM1Door.OpenFeedback")]
        public bool PM1SlitValveOpenFeedback { get; set; }

        [Subscription("TM.PM1Door.CloseFeedback")]
        public bool PM1SlitValveCloseFeedback { get; set; }

        [Subscription("TM.PM2Door.OpenFeedback")]
        public bool PM2SlitValveOpenFeedback { get; set; }

        [Subscription("TM.PM2Door.CloseFeedback")]
        public bool PM2SlitValveCloseFeedback { get; set; }

        [Subscription("TM.BufferDoor.OpenFeedback")]
        public bool BufferDoorOpenFeedback { get; set; }

        [Subscription("TM.BufferDoor.CloseFeedback")]
        public bool BufferDoorCloseFeedback { get; set; }

        [Subscription("TM.LoadLockDoor.OpenFeedback")]
        public bool LLDoorOpenFeedback { get; set; }

        [Subscription("TM.LoadLockDoor.CloseFeedback")]
        public bool LLDoorCloseFeedback { get; set; }

        [Subscription("TM.UnLoadDoor.OpenFeedback")]
        public bool UnLoadDoorOpenFeedback { get; set; }

        [Subscription("TM.UnLoadDoor.CloseFeedback")]
        public bool UnLoadDoorCloseFeedback { get; set; }


        [Subscription("EFEM.UnLoadSubDoor.OpenFeedback")]
        public bool UnLoadEfemDoorOpenFeedback { get; set; }

        [Subscription("EFEM.UnLoadSubDoor.CloseFeedback")]
        public bool UnLoadEfemDoorCloseFeedback { get; set; }

        [Subscription("EFEM.LoadLockLSideDoor.OpenFeedback")]
        public bool LLEfemLeftDoorOpenFeedback { get; set; }

        [Subscription("EFEM.LoadLockLSideDoor.CloseFeedback")]
        public bool LLEfemLeftDoorCloseFeedback { get; set; }

        [Subscription("EFEM.LoadLockRSideDoor.OpenFeedback")]
        public bool LLEfemRightDoorOpenFeedback { get; set; }

        [Subscription("EFEM.LoadLockRSideDoor.CloseFeedback")]
        public bool LLEfemRightDoorCloseFeedback { get; set; }


        [Subscription("TM.TMatATM.Value")]
        public bool TMatATM { get; set; }

        [Subscription("TM.LLAtATM.Value")]
        public bool LLAtATM { get; set; }


        [Subscription("TM.TMUnderVac.Value")]
        public bool TMUnderVac { get; set; }

        [Subscription("TM.LLUnderVAC.Value")]
        public bool LLUnderVAC { get; set; }

        [Subscription("TM.TMPressure.Value")]
        public float TMPressure { get; set; }

        [Subscription("TM.LLPressure.Value")]
        public double LLPressure { get; set; }

        [Subscription("Buffer.BufferTemp.FeedBack")]
        public float BufferTemp { get; set; }

        [Subscription("LL.LLTemp.FeedBack")]
        public float LLTemp { get; set; }

        [Subscription("LoadLock.LLLidClosed.Value")]
        public bool LLLidClosed { get; set; }

        [Subscription("TM.UnLoadPressure.Value")]
        public double UnLoadPressure { get; set; }

        [Subscription("LoadLock.LLLift.State")]
        public string LLLift { get; set; }

        [Subscription("UnLoad.UnLoadLift.UpSensor")]
        public bool UnLoadLiftUpSensor { get; set; }

        [Subscription("UnLoad.UnLoadLift.DownSensor")]
        public bool UnLoadLiftDownSensor { get; set; }
        public string UnLoadLiftState
        {
            get
            {
                if(UnLoadLiftUpSensor && !UnLoadLiftDownSensor)
                {
                    return "Up";
                }

                if (!UnLoadLiftUpSensor && UnLoadLiftDownSensor)
                {
                    return "Down";
                }

                return "Unknown";
            }
        }

        [Subscription("LoadLock.LLLift.UpSensor")]
        public bool LoadLiftUpSensor { get; set; }

        [Subscription("LoadLock.LLLift.DownSensor")]
        public bool LoadLiftDownSensor { get; set; }
        public string LoadLiftState
        {
            get
            {
                if (LoadLiftUpSensor && !LoadLiftDownSensor)
                {
                    return "Up";
                }

                if (!LoadLiftUpSensor && LoadLiftDownSensor)
                {
                    return "Down";
                }

                return "Unknown";
            }
        }

        [Subscription("LoadLock.LLWaferClaw.State")]
        public string LLWaferClaw { get; set; }

        [Subscription("LoadLock.LLTrayClaw.State")]
        public string LLTrayClaw { get; set; }

        [Subscription("UnLoad.UnLoadWaferClaw.State")]
        public string UnLoadWaferClaw { get; set; }
       
        //

        #endregion

        #region PM1

        [Subscription("PM1.Status")]
        public string PM1Status { get; set; }

        public string PM1StatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(PM1Status); }
        }

        public string PM1SelectedRecipe { get; set; }
        private string PM1SelectedRecipePath { get; set; }

        private int _recipeStepNumber;

        [Subscription("PM1.RecipeStepNumber")]
        public int RecipeStepNumber
        {
            get => _recipeStepNumber;
            set
            {
                if (_recipeStepNumber != value)
                {
                    _recipeStepNumber = value;
                    NotifyOfPropertyChange(nameof(FormattedRecipeStep));
                }
            }
        }

        /// <summary>
        /// 返回格式化后的Step Number。
        /// </summary>
        public string FormattedRecipeStep => RecipeStepNumber <= 0 ? "--" : RecipeStepNumber.ToString();

        [Subscription("PM1.PMServo.ActualSpeedFeedback")]
        public float ActualSpeedFeedback { get; set; }

        [Subscription("PM1.RecipeTotalElapseTime")]
        public int PM1RecipeTotalElapseTime { get; set; }

        public string ElapseTimePM1 => PM1Wafer != null && PM1Wafer.WaferStatus == 3 ? PM1RecipeTotalElapseTime.ToString() : "--";

        [Subscription("PM1.RecipeTotalTime")]
        public int PM1RecipeTotalTime { get; set; }
        public string TotalTimePM1 => PM1Wafer != null && PM1Wafer.WaferStatus == 3 ? PM1RecipeTotalTime.ToString() : "--";

        [Subscription("PM1.PT1.FeedBack")]
        public double PM1Pressure { get; set; }


        [Subscription("PM1.IsOnline")]
        public bool PM1IsOnline { get; set; }

        public bool PM1IsOffline
        {
            get { return !PM1IsOnline; }
        }

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

                if (!PM1ConfinementRingUpSensor && PM1ConfinementRingDwonSensor)
                {
                    return "Down";
                }

                return "Unknown";
            }
        }
        #endregion

        #region PM2

        [Subscription("PM2.Status")]
        public string PM2Status { get; set; }
        public string PM2StatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(PM2Status); }
        }

        public string PM2SelectedRecipe { get; set; }
        private string PM2SelectedRecipePath { get; set; }


        [Subscription("PM2.RecipeTotalElapseTime")]
        public int PM2RecipeTotalElapseTime { get; set; }

        public string ElapseTimePM2 => PM2Wafer != null && PM2Wafer.WaferStatus == 3 ? PM2RecipeTotalElapseTime.ToString() : "--";

        [Subscription("PM2.RecipeTotalTime")]
        public int PM2RecipeTotalTime { get; set; }
        public string TotalTimePM2 => PM2Wafer != null && PM2Wafer.WaferStatus == 3 ? PM2RecipeTotalTime.ToString() : "--";

        [Subscription("PM2.PT1.FeedBack")]
        public double PM2Pressure { get; set; }


        [Subscription("PM2.IsOnline")]
        public bool PM2IsOnline { get; set; }

        public bool PM2IsOffline
        {
            get { return !PM2IsOnline; }
        }
        #endregion

        #region Scheduler

        public bool IsRtInitialized
        {
            get
            {
                return RtStatus != "Init" && RtStatus != "Initializing";
            }
        }

        public bool IsRtRunning
        {
            get
            {
                return RtStatus != "Init" && RtStatus != "Idle";
            }
        }

        public bool IsAuto
        {
            get { return RtStatus == "AutoRunning" || RtStatus == "AutoIdle"; }
        }

        public string RunningMode => IsAuto ? "Auto" : "Manual";

        [Subscription("Rt.Status")]
        public string RtStatus { get; set; }

        [Subscription("Scheduler.IsCycleMode")]
        public bool IsCycleMode { get; set; }

        [Subscription("Scheduler.CycledCount")]
        public int CycledCount { get; set; }  //当前次数

        public int CurrentCycle
        {
            get
            {
                if (CycleSetPoint <= 0)
                {
                    return 0;
                }

                if (CycledCount + 1 > CycleSetPoint)
                {
                    return CycleSetPoint;
                }

                return CycledCount + 1;
            }
        }

        [Subscription("Scheduler.CycleSetPoint")]
        public int CycleSetPoint { get; set; }  //总次数



        [Subscription("CassAL.LocalJobName")]
        public string CassALJobName { get; set; }

        [Subscription("CassAL.LocalJobStatus")]
        public string CassALJobStatus { get; set; }


        [Subscription("CassAR.LocalJobName")]
        public string CassARJobName { get; set; }

        [Subscription("CassAR.LocalJobStatus")]
        public string CassARJobStatus { get; set; }



        #endregion

        #region Buffer
        [Subscription("Scheduler.TimeBuffer1")]
        public string TimeBuffer1 { get; set; }
        public double sTimeBuffer1 => TimeBuffer1 == null || TimeBuffer1.Length == 0 ? 0 : Double.Parse(TimeBuffer1);

        [Subscription("Scheduler.TimeBuffer2")]
        public string TimeBuffer2 { get; set; }
        public double sTimeBuffer2 => TimeBuffer2 == null || TimeBuffer2.Length == 0 ? 0: Double.Parse(TimeBuffer2);

        [Subscription("Scheduler.TimeBuffer3")]
        public string TimeBuffer3 { get; set; }
        public double sTimeBuffer3 => TimeBuffer3 == null || TimeBuffer3.Length == 0 ? 0 : Double.Parse(TimeBuffer3);

        public string TimeBuffer
        {
            get
            {
                return TimeBuffer1 + "/" + TimeBuffer2 + "/" + TimeBuffer3;
            }
        }

        [Subscription("TM.P116PIDTC.PVInValue")]
        public float BufferTempPV { get; set; }
        #endregion

        #region Button Logic

        public bool EnableWaferClick => RtStatus == "Idle";
        public bool LLEnableWaferClick => true;

        public bool IsEnableAbort
        {
            get
            {
                if (IsAuto)
                    return !string.IsNullOrEmpty(CassALJobStatus) || !string.IsNullOrEmpty(CassARJobStatus);

                return IsRtRunning;
            }
        }

        public bool IsEnableAuto
        {
            get { return !IsAuto && IsRtInitialized; }
        }

        public bool IsEnableManual
        {
            get { return IsAuto && IsRtInitialized && string.IsNullOrEmpty(CassALJobStatus) && string.IsNullOrEmpty(CassARJobStatus); }
        }

        public bool IsEnableInitialize
        {
            get { return !IsAuto && !IsRtRunning; }
        }


        public bool IsEnableReturnAll
        {
            get { return !IsAuto && IsRtInitialized && RtStatus == "Idle"; }
        }

        #endregion

        #endregion


        public OverViewViewModel()
        {

            DisplayName = "OverViewViewModel";
            ActiveUpdateData = true;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            InitLL();
            InitTM();
            InitPM();

            CassALWaferAssociation = new WaferAssociationInfo();
            CassALWaferAssociation.ModuleData = ModuleManager.ModuleInfos["CassAL"];

            CassARWaferAssociation = new WaferAssociationInfo();
            CassARWaferAssociation.ModuleData = ModuleManager.ModuleInfos["CassAR"];

            


            IsPM1Installed = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM1Installed");
            IsPM2Installed = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM2Installed");
            IsBufferInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsBufferInstalled");
            IsLoadLockInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsLoadLockInstalled");
            IsTMInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsTMInstalled");
            IsUnLoadInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsUnLoadInstalled");

            CassALeft1 = 1;
            CassALeft2 = 25;
            CassARight1 = 1;
            CassARight2 = 25;
            CassBL1 = 1;
            CassBL2 = 8;
        }

        public Visibility ProcessMonitorButtonVisibility { get; private set; }

        protected override void OnActivate()
        {
            var roleID = BaseApp.Instance.UserContext.RoleID;
            var allowProcessMonitorWin = RoleAccountProvider.Instance.GetMenuPermission(roleID, "PM1.Process.ProcessMonitorWindow") != (int)MenuPermissionEnum.MP_NONE;
            ProcessMonitorButtonVisibility = allowProcessMonitorWin ? Visibility.Visible : Visibility.Collapsed;

            base.OnActivate();
        }

        protected override void Poll()
        {
            base.Poll();
            PM1SelectedRecipePath = (string)QueryDataClient.Instance.Service.GetConfig("PM.PM1.LastRecipeName");

            if (!string.IsNullOrEmpty(PM1SelectedRecipePath))
            {
                var array = PM1SelectedRecipePath.Split('\\');

                if (PM1SelectedRecipe != array[array.Length - 1])
                {
                    PM1SelectedRecipe = array[array.Length - 1];
                }
            }
        }

        public void Start()
        {
            InvokeClient.Instance.Service.DoOperation("System.StartAutoRun");
        }

        public void Stop()
        {
            //InvokeClient.Instance.Service.DoOperation("System.StopAutoRun");
        }

        public void ReturnAllWafer()
        {
            var ret = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.WARNING,
                "Are you sure to perform Return All process?");

            if (ret == DialogButton.No)
                return;

            InvokeClient.Instance.Service.DoOperation("System.ReturnAllWafer");
        }

        public void PlatformCycle()
        {
            InvokeClient.Instance.Service.DoOperation("System.PlatformCycle");
        }

        public void StopPlatformCycle()
        {
            InvokeClient.Instance.Service.DoOperation("System.StopPlatformCycle");
        }


        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            if (CassALWaferAssociation == null)
            {
                CassALWaferAssociation = new WaferAssociationInfo();
                CassALWaferAssociation.ModuleData = ModuleManager.ModuleInfos["CassAL"];
            }

            if (CassARWaferAssociation == null)
            {
                CassARWaferAssociation = new WaferAssociationInfo();
                CassARWaferAssociation.ModuleData = ModuleManager.ModuleInfos["CassAR"];
            }

            CassALWaferAssociation.JobID = CassALJobName;
            CassALWaferAssociation.JobStatus = CassALJobStatus;

            CassARWaferAssociation.JobID = CassARJobName;
            CassARWaferAssociation.JobStatus = CassARJobStatus;
        }

        #region OverView Operation

        public void HomeAll()
        {
            InvokeClient.Instance.Service.DoOperation("System.HomeAll");
        }

        public void Abort()
        {
            var ret = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.WARNING,
                "Are you sure to abort process?");

            if (ret == DialogButton.No)
                return;

            InvokeClient.Instance.Service.DoOperation("System.Abort");
        }
        

        public void Auto()
        {
            var dialog = new ChooseDialogBoxViewModel
            {
                DisplayName = "Tips",
                InfoStr = "Please Check All Gas Ready Before Start Process!"
            };

            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if (!bret.HasValue || bret.Value == false)
                return;

            if (PM1Status != "ProcessIdle")
            {
                MessageBox.Show("PM must in ProcessIdle!", "Warn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InvokeClient.Instance.Service.DoOperation("System.SetAutoMode");

            JobListInfoData = new ObservableCollection<JobListInfo>();
        }

        public void Manual()
        {
            InvokeClient.Instance.Service.DoOperation("System.SetManualMode");
        }

        #endregion

        #region Wafer association

        #region Sequence operation
        public void SelectSequence(WaferAssociationInfo info)
        {
            SequenceDialogViewModel dialog = new SequenceDialogViewModel();
            dialog.DisplayName = "Select Sequence";

            dialog.Files = new ObservableCollection<FileNode>(RecipeSequenceTreeBuilder.GetFiles("",
                RecipeClient.Instance.Service.GetSequenceNameList()
            ));

            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                info.SequenceName = dialog.DialogResult;
            }
        }

        public void SetSlot(WaferAssociationInfo info)
        {
            if (InputSlotCheck(info.SlotFrom, info.SlotTo))
                AssociateSequence(info, true);
        }

        public void SkipSlot(WaferAssociationInfo info)
        {
            if (InputSlotCheck(info.SlotFrom, info.SlotTo))
                AssociateSequence(info, false);
        }

        public void SetAll(WaferAssociationInfo info)
        {
            info.SlotFrom = 1;
            info.SlotTo = 25;
            AssociateSequence(info, true);
        }

        public void DeselectAll(WaferAssociationInfo info)
        {
            info.SlotFrom = 1;
            info.SlotTo = 25;
            AssociateSequence(info, false);
        }

        public void SetSequence(WaferAssociationInfo info, int slotIndex, string seqName)
        {
            bool flag = string.IsNullOrEmpty(seqName);
            AssociateSequence(info, flag, slotIndex - 1);
        }

        private bool InputSlotCheck(int from, int to)
        {
            if (from > to)
            {
                DialogBox.ShowInfo("This index of from slot should be large than the index of to slot.");
                return false;
            }
            if (from < 1 || to > 25)
            {
                DialogBox.ShowInfo("This input value for from should be between 1 and 25.");
                return false;
            }
            return true;
        }

        private void AssociateSequence(WaferAssociationInfo info, bool flag, int slot = -1)
        {
            ObservableCollection<WaferInfo> wafers = info.ModuleData.WaferManager.Wafers;
            if (slot >= 0) //by wafer
            {
                int index = wafers.Count - slot - 1;
                if (index < wafers.Count)
                {
                    if (flag && HasWaferOnSlot(wafers.ToList(), index))
                        wafers[index].SequenceName = info.SequenceName;
                    else
                        wafers[index].SequenceName = string.Empty;
                }
            }
            else //by from-to
            {
                for (int i = info.SlotFrom - 1; i < info.SlotTo; i++)
                {
                    int index = wafers.Count - i - 1;
                    if (index < wafers.Count)
                    {
                        if (flag && HasWaferOnSlot(wafers.ToList(), index))
                            wafers[index].SequenceName = info.SequenceName;
                        else
                            wafers[index].SequenceName = string.Empty;
                    }
                }
            }
        }

        private bool HasWaferOnSlot(List<WaferInfo> wafers, int index)
        {
            if (wafers[index].WaferStatus == 0)
                return false;

            return true;
        }
        #endregion

        #region Job operation

        private bool JobCheck(string jobID)
        {
            if (jobID.Length == 0)
            {
                DialogBox.ShowWarning("Please create job first.");
                return false;
            }

            return true;
        }

        public void CreateJob(WaferAssociationInfo info)
        {
            //判断TM和Efem的的SlitValue是否都关闭了,以免后面开门报错


            List<string> slotSequence = new List<string>();
            foreach (var wafer in info.ModuleData.WaferManager.Wafers)
            {
                slotSequence.Insert(0, wafer.SequenceName);
            }

            string jobId = info.LotId.Trim();
            //if (string.IsNullOrEmpty(jobId))
            //    jobId = "CJ_Local_" + info.ModuleData.ModuleID;
            info.LotIdSaved = true;
            WaferAssociationProvider.Instance.CreateJob(jobId, info.ModuleData.ModuleID, slotSequence, true);
        }

        public void AbortJob(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.AbortJob(jobID);
        }

        public void Start(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Start(jobID);
        }

        public void Pause(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Pause(jobID);
        }

        public void Resume(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Resume(jobID);
        }

        public void Stop(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Stop(jobID);
        }


        public void PauseJobList_Click(object info)
        {
            System.Windows.Forms.MessageBox.Show(info.ToString());
        }
        public void ResumeJobList_Click(object info)
        {
            System.Windows.Forms.MessageBox.Show(info.ToString());
        }
        public void AbortJobList_Click(object info)
        {
            System.Windows.Forms.MessageBox.Show(info.ToString());
        }
        #endregion Job operation

        #region Cassette Operation
        public bool IsCassALLoadEnable => true;
        public bool IsCassARLoadEnable => true;
        public bool IsCassBLLoadEnable => true;

        public void CreateWafer(string cassName, string startSlotStr, string endSlotStr)
        {
            int startSlot = 0;
            int endSlot = 0;
            if (Int32.TryParse(startSlotStr, out startSlot) && Int32.TryParse(endSlotStr, out endSlot))
            {
                InvokeClient.Instance.Service.DoOperation($"{cassName}.CreateAll", startSlot, endSlot);
            }
        }

        public void DeleteWafer(string cassName, string startSlotStr, string endSlotStr)
        {
            int startSlot = 0;
            int endSlot = 0;
            if (Int32.TryParse(startSlotStr, out startSlot) && Int32.TryParse(endSlotStr, out endSlot))
            {
                InvokeClient.Instance.Service.DoOperation($"{cassName}.DeleteAll", startSlot, endSlot);
            }
        }

        public void MapWafer(string cassName)
        {
            InvokeClient.Instance.Service.DoOperation("WaferRobot.Map", cassName);
        }

        public void MapTray(string cassName)
        {
            InvokeClient.Instance.Service.DoOperation("TrayRobot.Map", cassName);
        }

        #endregion

        #endregion

        [Subscription("UnLoad.RemainedCoolingTime")]
        public int UnLoadCoolingTime { get; set; }

        [Subscription("UnLoad.UnLoadTemp.FeedBack")]
        public float UnLoadTemperature { get; set; }


        /// <summary>
        /// 打开 Process Monitor
        /// </summary>
        public void ShowProcessMonitorWindow()
        {
            // 如果已经打开，则激活窗口
            var wins = Application.Current.Windows.OfType<ProcessMonitorView>().ToArray();
            if (wins.Any())
            {
                foreach (var w in wins)
                {
                    w.Activate();
                }
            }
            else
            {
                var wm = new WindowManager();
                var model = new ProcessMonitorViewModel
                {
                    Token = BaseApp.Instance.UserContext.Token
                };
                if (model is ISupportMultipleSystem system)
                    system.SystemName = "PM1";

                dynamic settings = new ExpandoObject();
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settings.Title = "Process Data Monitor";

                wm.ShowWindow(model, null, settings);
            }
        }

        /// <summary>
        /// 关闭 Process Monitor
        /// </summary>
        public void CloseProcessMonitorWindow()
        {
            var wins = Application.Current.Windows.OfType<ProcessMonitorView>().ToArray();
            if (!wins.Any())
                return;

            foreach (var w in wins)
            {
                w.Close();
            }
        }

        /// <summary>
        /// Aggregated Event。
        /// </summary>
        /// <param name="message"></param>
        public void Handle(ShowCloseMonitorWinEvent message)
        {
            if(message.IsShow)
                // 显示Process监控窗口
                ShowProcessMonitorWindow();
            else
                CloseProcessMonitorWindow();
        }
    }
}
