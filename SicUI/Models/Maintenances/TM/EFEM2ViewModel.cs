using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Collections.Generic;
using OpenSEMI.ClientBase;

namespace SicUI.Models.Maintenances.TM
{
    public class EFEM2ViewModel : SicUIViewModelBase
    {
        #region Properties Binding
        private List<string> _waferModules = new List<string>() { "LoadLock", "UnLoad", "Aligner", "CassAR", "CassAL" };
        public List<string> WaferModules
        {
            get { return _waferModules; }
            set { _waferModules = value; NotifyOfPropertyChange("WaferModules"); }
        }

        private List<string> _trayModules = new List<string>() { "LoadLock", "CassBL" };
        public List<string> TrayModules
        {
            get { return _trayModules; }
            set { _trayModules = value; NotifyOfPropertyChange("TrayModules"); }
        }
        private string _waferSelectedModule;
        public string WaferSelectedModule
        {
            get { return _waferSelectedModule; }
            set { _waferSelectedModule = value; NotifyOfPropertyChange("WaferSelectedModule"); }
        }

        private string _traySelectedModule;
        public string TraySelectedModule
        {
            get { return _traySelectedModule; }
            set { _traySelectedModule = value; NotifyOfPropertyChange("TraySelectedModule"); }
        }

        private List<int> _waferSlots = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
        public List<int> WaferSlots
        {
            get 
            {
                if (WaferSelectedModule !=null && WaferSelectedModule.Contains("Cass"))
                {
                    return _waferSlots;
                }
                else
                {
                    return new List<int> { 1 };
                }
            }
            set { _waferSlots = value; NotifyOfPropertyChange("WaferSlots"); }
        }
        private int _waferSelectedSlot;
        public int WaferSelectedSlot
        {
            get { return _waferSelectedSlot; }
            set { _waferSelectedSlot = value; NotifyOfPropertyChange("WaferSelectedSlot"); }
        }

        private List<int> _traySlots = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
        public List<int> TraySlots
        {
            get {
                if (TraySelectedModule != null &&  TraySelectedModule.Contains("Cass"))
                {
                    return _traySlots;
                }
                else
                {
                    return new List<int> { 1 };
                }
            }
        }
        private int _traySelectedSlot;
        public int TraySelectedSlot
        {
            get { return _traySelectedSlot; }
            set { _traySelectedSlot = value; NotifyOfPropertyChange("TraySelectedSlot"); }
        }



        public List<string> UnLoadDoors=> new List<string>() { "AtmDoor", "VaccumDoor" };
        public List<string> LoadDoors => new List<string>() { "LeftDoor", "RightDoor", "VaccumDoor" };

        private string _selectedUnLoadDoor;
        public string SelectedUnLoadDoor
        {
            get { return _selectedUnLoadDoor; }
            set { _selectedUnLoadDoor = value; NotifyOfPropertyChange("SelectedUnLoadDoor"); }
        }
        private string _selectedLLDoor;
        public string SelectedLLDoor
        {
            get { return _selectedLLDoor; }
            set { _selectedLLDoor = value; NotifyOfPropertyChange("SelectedLLDoor"); }
        }
        #endregion

        [Subscription("Rt.Status")]
        public string RtStatus { get; set; }
        public bool EnableWaferClick => RtStatus == "Idle";

        #region EFEM,Aligner

        [Subscription("EFEM.Status")]
        public string EFEMStatus { get; set; }


        [Subscription("EFEM.IsOnline")]
        public bool EFEMIsOnline { get; set; }

        [Subscription("TM.HiWinAligner.HaveWafer")]
        public string AlignerHaveWafer { get; set; }


        public bool IsEFEMEnableManualOperation => !EFEMIsOnline && EFEMStatus == "Idle";

        public bool IsWaferRobotEnableManualOperation => !WaferRobotIsOnline && (WaferRobotStatus == "Idle" || WaferRobotStatus == "Init");

        public bool IsTrayRobotEnableManualOperation => !TrayRobotIsOnline && (TrayRobotStatus == "Idle" || TrayRobotStatus == "Init");
        
        //设定马达激磁状态
        public void Aligner_SME()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwSME");
        }
        //原点复归
        public void Aligner_HOM()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwHOM");
        }

        //移至测量中心点
        public void Aligner_MTM()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwMTM");
        }

        //读取镭射侦测物件
        public void Aligner_DOC()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwDOC");
        }
        //开启真空
        public void Aligner_CVN()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwCVN");
        }
        //晶圆寻边与辅正
        public void Aligner_BAL()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwBAL");
        }
        //关闭真空
        public void Aligner_CVF()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwCVF");
        }
        //清除报警
        public void Aligner_ERS()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwERS");
        }

        public void Aligner_Abort()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwAbort");
        }

        public void AlignerAbortRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"Aligner.Abort");
        }

        public void AlignerAlignerRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"Aligner.Aligner");
        }

        [Subscription("Aligner.IsOnline")]
        public bool AlignerIsOnline { get; set; }


        public bool IsAlignerEnableManualOperation =>  AlignerStatus == "Idle";

       


        #endregion

        #region  Robot 
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

        [Subscription("WaferRobot.Status")]
        public string WaferRobotStatus
        {
            get;
            set;
        }


        [Subscription("TrayRobot.Status")]
        public string TrayRobotStatus
        {
            get;
            set;
        }

        [Subscription("WaferRobot.IsOnline")]
        public bool WaferRobotIsOnline { get; set; }


        [Subscription("TrayRobot.IsOnline")]
        public bool TrayRobotIsOnline { get; set; }

        public void RobotGetError(string robotName)
        {
            InvokeClient.Instance.Service.DoOperation($"{robotName}.ERR");
        }

        public void WaferRobotHome()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.RobotConnectHomeAll");
        }

        public void WaferRobotPickRoutine()
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                DialogBox.ShowWarning("No target module selected");
                return;
            }
            int cSlot = WaferSelectedSlot - 1;
            if (cSlot < 0)
            {
                DialogBox.ShowWarning("Target slot incorrect");
                return;
            }
            
            if (!WaferSelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.Pick", WaferSelectedModule, cSlot);
        }
        public void WaferRobotPlaceRoutine()
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                DialogBox.ShowWarning("No target module selected");
                return;
            }
            int cSlot = WaferSelectedSlot - 1;
            if (cSlot < 0)
            {
                DialogBox.ShowWarning("Target slot incorrect");
                return;
            }
            
            if (!WaferSelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.Place", WaferSelectedModule, cSlot);
        }
        public void WaferRobotMapRoutine()
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                DialogBox.ShowWarning("No cassette selected");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"WaferRobot.Map", WaferSelectedModule);
        }

        public void TrayRobotHome()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.RobotConnectHomeAll");
        }
        public void TrayRobotPickRoutine()
        {
            if (string.IsNullOrEmpty(TraySelectedModule))
            {
                DialogBox.ShowWarning("No target module selected");
                return;
            }
            int cSlot = TraySelectedSlot - 1;
            if (cSlot < 0)
            {
                DialogBox.ShowWarning("Target slot incorrect");
                return;
            }
            
            if (!TraySelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.Pick", TraySelectedModule, cSlot);
        }
        public void TrayRobotPlaceRoutine()
        {
            if (string.IsNullOrEmpty(TraySelectedModule))
            {
                DialogBox.ShowWarning("No target module selected");
                return;
            }
            
            var cSlot = TraySelectedSlot - 1;
            if (cSlot < 0)
            {
                DialogBox.ShowWarning("Target slot incorrect");
                return;
            }
            
            if (!TraySelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.Place", TraySelectedModule, cSlot);
        }
        public void TrayRobotMapRoutine()
        {
            if (string.IsNullOrEmpty(TraySelectedModule))
            {
                DialogBox.ShowWarning("No cassette selected");
               return;
            }
            
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.Map", TraySelectedModule);
        }
        #endregion OP

        #region LoadLock
        [Subscription("LoadLock.IsOnline")]
        public bool LLIsOnline { get; set; }

        [Subscription("LoadLock.Status")]
        public string LLStatus { get; set; }

        [Subscription("TM.LLPressure.Value")]
        public double LoadPressure { get; set; }

        public bool IsLLEnableManualOperation => !LLIsOnline;

        [Subscription("LoadLock.LLLift.UpSensor")]
        public bool LoadLiftUpSensor { get; set; }

        [Subscription("LoadLock.LLLift.DownSensor")]
        public bool LoadLiftDownSensor { get; set; }

        [Subscription("PM1.IsService")]
        public bool IsPMService { get; set; }

        public void LoadGroup()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.Group");
        }

        public void LoadSeparate()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.Separate");
        }

        public void LLLiftUp()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.LLLift.MoveUp");
        }

        public void LLLiftDown()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.LLLift.MoveDown");
        }

        public void LLWaferClamped()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.LLWaferClaw.Clamping");
        }

        public void LLWaferOpen()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.LLWaferClaw.UnClamping");
        }

        public void LLTrayClamped()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.LLTrayClaw.Clamping");
        }

        public void LLTrayOpen()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.LLTrayClaw.UnClamping");
        }
        public void OpenSlitValve()
        {
            if (SelectedLLDoor == "LeftDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"EFEM.OpenSlitValve", "LoadLock", "WaferRobot");
            }
            else if (SelectedLLDoor == "RightDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"EFEM.OpenSlitValve", "LoadLock", "TrayRobot");
            }
            else if (SelectedLLDoor == "VaccumDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"TM.OpenSlitValve", "LoadLock");
            }
        }
        public void CloseSlitValve()
        {
            if (SelectedLLDoor == "LeftDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"EFEM.CloseSlitValve", "LoadLock", "WaferRobot");
            }
            else if (SelectedLLDoor == "RightDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"EFEM.CloseSlitValve", "LoadLock", "TrayRobot");
            }
            else if (SelectedLLDoor == "VaccumDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"TM.CloseSlitValve", "LoadLock");
            }
        }
        #endregion

        #region UnLoad

        [Subscription("UnLoad.IsOnline")]
        public bool UnLoadIsOnline { get; set; }

        [Subscription("UnLoad.Status")]
        public string UnLoadStatus { get; set; }


        [Subscription("TM.UnLoadPressure.Value")]
        public double UnLoadPressure { get; set; }

        [Subscription("LoadLock.LLLift.State")]
        public string LLPinState
        {
            get;
            set;
        }
        [Subscription("LoadLock.LLWaferClaw.State")]
        public string LLWaferClawState
        {
            get;
            set;
        }
        [Subscription("LoadLock.LLTrayClaw.State")]
        public string LLTrayClawState
        {
            get;
            set;
        }

        [Subscription("UnLoad.UnLoadLift.State")]
        public string UnLoadPinState
        {
            get;
            set;
        }

        [Subscription("UnLoad.UnLoadWaferClaw.State")]
        public string UnLoadWaferClawState
        {
            get;
            set;
        }

        [Subscription("UnLoad.UnLoadLift.UpSensor")]
        public bool UnLoadLiftUpSensor { get; set; }

        [Subscription("UnLoad.UnLoadLift.DownSensor")]
        public bool UnLoadLiftDownSensor { get; set; }


        public bool IsUnLoadEnableManualOperation => !UnLoadIsOnline;

        public void UnLoadSeparate()
        {
            InvokeClient.Instance.Service.DoOperation($"UnLoad.Separate");
        }
        public void UnLoadLiftUp()
        {
            InvokeClient.Instance.Service.DoOperation($"UnLoad.UnLoadLift.MoveUp");
        }

        public void UnLoadLiftDown()
        {
            InvokeClient.Instance.Service.DoOperation($"UnLoad.UnLoadLift.MoveDown");
        }

        public void UnLoadWaferClamped()
        {
            InvokeClient.Instance.Service.DoOperation($"UnLoad.UnLoadWaferClaw.Clamping");
        }

        public void UnLoadWaferOpen()
        {
            InvokeClient.Instance.Service.DoOperation($"UnLoad.UnLoadWaferClaw.UnClamping");
        }


        public void OpenSlitValveUnLoad()
        {
            if (SelectedUnLoadDoor == "AtmDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"EFEM.OpenSlitValve", "UnLoad", "WaferRobot");
            }
            else if(SelectedUnLoadDoor == "VaccumDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"TM.OpenSlitValve", "UnLoad");
            }
        }
        public void CloseSlitValveUnLoad()
        {
            if (SelectedUnLoadDoor == "AtmDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"EFEM.CloseSlitValve", "UnLoad", "WaferRobot");
            }
            else if (SelectedUnLoadDoor == "VaccumDoor")
            {
                InvokeClient.Instance.Service.DoOperation($"TM.CloseSlitValve", "UnLoad");
            }
        }

        #endregion

        #region Cassette
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
        #endregion Cassette

        #region SlitValve

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

        [Subscription("TM.LoadLockDoor.OpenFeedback")]
        public bool LLDoorOpenFeedback { get; set; }

        [Subscription("TM.LoadLockDoor.CloseFeedback")]
        public bool LLDoorCloseFeedback { get; set; }

        [Subscription("TM.UnLoadDoor.OpenFeedback")]
        public bool UnLoadDoorOpenFeedback { get; set; }

        [Subscription("TM.UnLoadDoor.CloseFeedback")]
        public bool UnLoadDoorCloseFeedback { get; set; }



        public bool IsEfemLLLeftDoorOpen => EfemLLLeftDoorState == FoupDoorState.Open ? true : false;
        public bool IsEfemLLRightDoorOpen => EfemLLRightDoorState == FoupDoorState.Open ? true : false;
        public bool IsEfemUnLoadDoorOpen => EfemUnLoadDoorState == FoupDoorState.Open ? true : false;
        public bool IsLLDoorOpen => LLDoorState == FoupDoorState.Open ? true : false;

        public bool IsUnLoadDoorOpen => UnLoadDoorState == FoupDoorState.Open ? true : false;

        public bool IsEfemLLLeftDoorClose => EfemLLLeftDoorState == FoupDoorState.Close ? true : false;
        public bool IsEfemLLRightDoorClose => EfemLLRightDoorState == FoupDoorState.Close ? true : false;
        public bool IsEfemUnLoadDoorClose => EfemUnLoadDoorState == FoupDoorState.Close ? true : false;

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
        #endregion


        public void Pump(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Pump");
        }

        public void Vent(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Vent");
        }

        public void Home(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Home");
        }

        public void Reset(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Reset");
        }

        public void Abort(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.Abort");
        }

        public void Online(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.SetOnline");
        }

        public void Offline(string module)
        {
            InvokeClient.Instance.Service.DoOperation($"{module}.SetOffline");
        }

    }
}
