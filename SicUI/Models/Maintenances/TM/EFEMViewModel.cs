using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.OperationCenter;
using SicUI.Models;
using System;
using System.Collections.Generic;

namespace SicUI.Client.Models.Platform.TM
{
    public class EFEMViewModel : SicUIViewModelBase
    {
        #region Properties Robot
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
            get { return _waferSlots; }
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
            get { return _traySlots; }
            set { _traySlots = value; NotifyOfPropertyChange("TraySlots"); }
        }
        private int _traySelectedSlot;
        public int TraySelectedSlot
        {
            get { return _traySelectedSlot; }
            set { _traySelectedSlot = value; NotifyOfPropertyChange("TraySelectedSlot"); }
        }

        private List<int> _getSetSPSteps = new List<int>() { 1, 2, 3, 4 };
        public List<int> GetSetSPSteps
        {
            get { return _getSetSPSteps; }
            set { _getSetSPSteps = value; NotifyOfPropertyChange("GetSetSPSteps"); }
        }
        private int _selectedGetSteps;
        public int SelectedGetSteps
        {
            get { return _selectedGetSteps; }
            set { _selectedGetSteps = value; NotifyOfPropertyChange("SelectedGetSteps"); }
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

        [Subscription("WaferRobot.State")]
        public string WaferRobotState
        {
            get;
            set;
        }

        [Subscription("WaferRobot.IsBobotReady")]
        public string WaferRobotEnable
        {
            get;
            set;
        }

        

        [Subscription("TrayRobot.State")]
        public string TrayRobotState
        {
            get;
            set;
        }
        #endregion Properties


        #region OP Robot
        public void WaferRobotExtend()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.SetPosition", WaferSelectedModule);
        }

        public void WaferRobotRESP()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.RESP");
        }
        public void WaferRobotREMS()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.REMS");
        }
        public void WaferRobotSVON()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.SVON");
        }
        public void WaferRobotHome()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.HOME");
        }
        public void WaferRobotSTAT()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.STAT");
        }
        public void WaferRobotHomeAll()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.RobotConnectHomeAll");
        }
        public void WaferRobotPick()
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                return;
            }
            int cSlot = WaferSelectedSlot - 1;
            if (!WaferSelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.GETA", WaferSelectedModule, cSlot);
        }
        public void WaferRobotPlace()
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                return;
            }
            int cSlot = WaferSelectedSlot - 1;
            if (!WaferSelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.PUTA", WaferSelectedModule, cSlot);
        }
        public void WaferRobotMap()
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.MAP", WaferSelectedModule);
        }
        public void WaferRobotRSR()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.RSR");
        }
        public void WaferRobotGetSP(int spIndex)
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                return;
            }
            int cSlot = WaferSelectedSlot - 1;
            if (!WaferSelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.GETSP", WaferSelectedModule, cSlot, spIndex);
        }
        public void WaferRobotPutSP(int spIndex)
        {
            if (string.IsNullOrEmpty(WaferSelectedModule))
            {
                return;
            }
            int cSlot = WaferSelectedSlot - 1;
            if (!WaferSelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.PUTSP", WaferSelectedModule, cSlot, spIndex);
        }
        public void WaferRobotAbort()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.Abort");
        }
        public void WaferRobotInPutA()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.InputA");
        }
        public void WaferRobotOutPA()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.OutP");
        }
        public void WaferRobotERR()
        {
            InvokeClient.Instance.Service.DoOperation($"WaferRobot.ERR");
        }
        




        public void TrayRobotExtend()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.SetPosition", TraySelectedModule);
        }
        public void TrayRobotRESP()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.RESP");
        }
        public void TrayRobotREMS()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.REMS");
        }
        public void TrayRobotSVON()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.SVON");
        }
        public void TrayRobotHome()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.HOME");
        }
        public void TrayRobotSTAT()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.STAT");
        }
        
        public void TrayRobotHomeAll()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.RobotConnectHomeAll");
        }
        public void TrayRobotERR()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.ERR");
        }

        public void TrayRobotPick()
        {
            if (string.IsNullOrEmpty(TraySelectedModule))
            {
                return;
            }
            int cSlot = TraySelectedSlot - 1;
            if (!TraySelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.GETB", TraySelectedModule, cSlot);
        }
        public void TrayRobotPlace()
        {
            if (string.IsNullOrEmpty(TraySelectedModule))
            {
                return;
            }
            int cSlot = TraySelectedSlot - 1;
            if (!TraySelectedModule.Contains("Cass"))
            {
                cSlot = 0;
            }
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.PUTB", TraySelectedModule, cSlot);
        }
        public void TrayRobotMAP()
        {
            if (string.IsNullOrEmpty(TraySelectedModule))
            {
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.MAP", TraySelectedModule);
        }
        public void TrayRobotRSR()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.RSR");
        }
        public void TrayRobotAbort()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.Abort");
        }    
        public void TrayRobotInPutA()
        {
            InvokeClient.Instance.Service.DoOperation($"TrayRobot.InputA");
        }

        #endregion OP


        #region OP,Properties Shutter

        public void LoadGroup()
        {
            InvokeClient.Instance.Service.DoOperation($"LoadLock.Group");
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





        public void CreateWafer(string cassName, string startSlotStr,string endSlotStr)
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
        #endregion


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

        public bool IsEfemLLLeftDoorOpen => EfemLLLeftDoorState == FoupDoorState.Open ? true : false;
        public bool IsEfemLLRightDoorOpen => EfemLLRightDoorState == FoupDoorState.Open ? true : false;
        public bool IsEfemUnLoadDoorOpen => EfemUnLoadDoorState == FoupDoorState.Open ? true : false;
        public bool IsLLDoorOpen => LLDoorState == FoupDoorState.Open ? true : false;

        public bool IsEfemLLLeftDoorClose => EfemLLLeftDoorState == FoupDoorState.Close ? true : false;
        public bool IsEfemLLRightDoorClose => EfemLLRightDoorState == FoupDoorState.Close ? true : false;
        public bool IsEfemUnLoadDoorClose => EfemUnLoadDoorState == FoupDoorState.Close ? true : false;


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


        public void OpenSlitValve(string module, string robot)
        {
            InvokeClient.Instance.Service.DoOperation($"EFEM.OpenSlitValve", module, robot);
        }
        public void CloseSlitValve(string module, string robot)
        {
            InvokeClient.Instance.Service.DoOperation($"EFEM.CloseSlitValve", module, robot);
        }
        #endregion

        #region Properties Load/UnLoad/Aligner

        [Subscription("TM.LLPressure.FeedBack")]
        public double LoadPressure { get; set; }

        [Subscription("TM.UnLoadPressure.FeedBack")]
        public double UnLoadPressure { get; set; }

        [Subscription("LoadLock.LLLift.State")]
        public string LLLift { get; set; }

        [Subscription("UnLoad.UnLoadLift.State")]
        public string UnLift { get; set; }

        [Subscription("LoadLock.LLWaferClaw.State")]
        public string LLWaferClaw { get; set; }

        [Subscription("LoadLock.LLTrayClaw.State")]
        public string LLTrayClaw { get; set; }

        [Subscription("UnLoad.UnLoadWaferClaw.State")]
        public string UnLoadWaferClaw { get; set; }

        public bool IsLLLiftUp   => LLLift == "Up";
        public bool IsLLLiftDown => LLLift == "Down";
        public bool IsULLiftUp   => UnLift == "Up";
        public bool IsULLiftDown => UnLift == "Down";
        public bool IsLLWaferClamp => LLWaferClaw == "Clamp";
        public bool IsLLWaferOpen  => LLWaferClaw == "UnClamp";
        public bool IsLLTrayClamp  => LLTrayClaw == "Clamp";
        public bool IsLLTrayOpen   => LLTrayClaw == "UnClamp";
        public bool IsULWaferClamp => UnLoadWaferClaw == "Clamp";
        public bool IsULWaferOpen  => UnLoadWaferClaw == "UnClamp";




        [Subscription("TM.HiWinAligner.HaveWafer")]
        public string AlignerHaveWafer { get; set; }


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

        public void AlignerHomeRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"Aligner.Home");
        }

        public void AlignerAbortRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"Aligner.Abort");
        }

        public void AlignerAlignerRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"Aligner.Aligner");
        }


        [Subscription("EFEM.Status")]
        public string EFEMStatus { get; set; }        

        public void EFEMHomeRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"EFEM.Home");
        }
        public void EFEMAbortRoutine()
        {
            InvokeClient.Instance.Service.DoOperation($"EFEM.Abort");
        }
        #endregion
    }
}
