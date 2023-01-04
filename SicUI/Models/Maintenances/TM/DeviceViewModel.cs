using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using SicUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Event;

using System.Threading;
using Aitex.Core.Common.DeviceData;

namespace SicUI.Client.Models.Platform.TM
{
    public class DeviceViewModel : SicUIViewModelBase
    {
        [Subscription("LoadLock.IsOnline")]
        public bool LoadLockIsOnline { get; set; }

        [Subscription("PM1.IsOnline")]
        public bool PM1IsOnline { get; set; }

        [Subscription("PM1.Status")]
        public string PM1Status{ get; set; }

        [Subscription("Load.Rotation.IsServoBusy")]
        public bool LdRotationIsBusy { get; set; }

        [Subscription("Load.Rotation.IsServoOn")]
        public bool LdRotationIsServoOn { get; set; }

        [Subscription("Load.Rotation.IsMoveDone")]
        public bool LdRotationIsServoDone { get; set; }

        [Subscription("Load.Rotation.IsServoError")]
        public bool LdRotationIsServoError { get; set; }

        [Subscription("TM.LLTrayPresence.DeviceData")]
        public AITSensorData LoadTrayPresence { get; set; }

        [Subscription("TM.LoadTrayHomeSensor.DeviceData")]
        public AITSensorData LoadTrayHomeSensor { get; set; }

        [Subscription("TM.LLWaferPlaced.DeviceData")]
        public AITSensorData LoadWaferPlaced { get; set; }

        [Subscription("Load.Rotation.CurPos")]
        public double LdRotationCurPos { get; set; }

        [Subscription("Load.Rotation.CCD1Degree")]
        public double LdCCD1Degree { get; set; }

        [Subscription("Load.Rotation.CCD2Degree")]
        public double LdCCD2Degree { get; set; }

        public bool LdRotationBtnEnable => !LdRotationIsBusy && !LdRotationIsServoError && !LoadLockIsOnline;

        #region AutoTransferCondition

        [Subscription("Scheduler.AutoTransferConditionText")]
        public string AutoTransferConditionText { get; set; }
        [Subscription("Scheduler.WhichCondition")]
        public string WhichCondition { get; set; }

        public void AutoTransferCond(string sCond)
        {
            AutoTransferConditionText = "";
            WhichCondition = sCond;
        }

        #endregion

        public DeviceViewModel()
        {
            AutoTransferConditionText = "";            
        }

        public void LdRotationStop()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.Stop");
        }

        public void LdRotationReset()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.ServoReset");
        }

        public void LdRotationServoOn()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.ServoOn");
        }

        public void LdRotationMoveOneCircle()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.MoveOneCircle");
        }

        public void LdRotationRelativeHome()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.MoveRelativeHome");
        }

        public void LdRotationRelativeHomeOffset()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.HomeOffset");
        }

        public void LdRotationMoveCCD1Pos()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.MoveCCD1Pos");
        }

        public void LdRotationMoveCCD2Pos()
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.MoveCCD2Pos");
        }

        public void LdRotationJogCW(float angle)
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.JogCW", angle);
        }

        public void LdRotationJogCCW(float angle)
        {
            InvokeClient.Instance.Service.DoOperation($"Load.Rotation.JogCCW", angle);
        }

        #region CCD
        [Subscription("TM.KeyenceCVX300F.Result")]
        public string CCDResult { get; set; }

        public void CCDTrig()
        {
            int iConditionId = CCDPosSelected == "Pos1" ? 1 : 2;
            
            InvokeClient.Instance.Service.DoOperation("TM.KeyenceCVX300F.ClearResult");
            InvokeClient.Instance.Service.DoOperation("TM.KeyenceCVX300F.GetResult", iConditionId);
        }
        public void RunR0()
        {
            InvokeClient.Instance.Service.DoOperation("TM.KeyenceCVX300F.RunR0");
        }

        private List<string> _CCDPos = new List<string>() { "Pos1", "Pos2" };
        public List<string> CCDPos
        {
            get { return _CCDPos; }
            set { _CCDPos = value; }
        }

        private string _CCDPosSelect = "Pos1";

        public string CCDPosSelected
        {
            get { return _CCDPosSelect; }
            set { _CCDPosSelect = value; }
        }

        #endregion


        #region hw aligner
        //晶圆尺寸
        private List<string> _waferSizes = new List<string>() { "0（出厂值）", "4", "5", "6", "8" };
        public List<string> WaferSizes
        {
            get { return _waferSizes; }
            set { _waferSizes = value; NotifyOfPropertyChange("WaferSize"); }
        }
        private string _waferSizeSelected;
        public string WaferSizeSelected
        {
            get { return _waferSizeSelected; }
            set {
                if (value.CompareTo(_waferSizes[0]) == 0)
                {
                    _waferSizeSelected = "0";
                }
                else
                {
                    _waferSizeSelected = value;
                }
                NotifyOfPropertyChange("WaferSizeSelected"); 
            }
        }
        //晶圆类型
        private List<string> _waferTypes = new List<string>() {
            "0：无Notch或Flat的晶圆",
            "1：有NOtch的晶圆（出厂值）",
            "2：有一个或多个Flat的晶圆"
        };
        public List<string> WaferTypes
        {
            get { return _waferTypes; }
            set { _waferTypes = value; NotifyOfPropertyChange("WaferTypes"); }
        }
        private string _waferTypeSelected;
        public string WaferTypeSelected
        {
            get { return _waferTypeSelected; }
            set { 
                if (value.CompareTo(_waferTypes[0]) == 0)
                {
                    _waferTypeSelected = "0";
                }
                else if(value.CompareTo(_waferTypes[1]) == 0)
                {
                    _waferTypeSelected = "1";
                }
                else
                {
                    _waferTypeSelected = "2";
                }

                
                NotifyOfPropertyChange("WaferTypeSelected"); 
            }
        }
        //寻边材质
        private List<string> _waferMaterial = new List<string>() {
            "0:不透明（出厂值）",
            "1:透明、半透明"
        };
        public List<string> WaferMaterial
        {
            get { return _waferMaterial; }
            set { _waferMaterial = value; NotifyOfPropertyChange("WaferMaterial"); }
        }
        private string _waferMaterialSelected;
        public string WaferMaterialSelected
        {
            get { return _waferMaterialSelected; }
            set { 
                if(value.CompareTo(_waferMaterial[0]) == 0)
                {
                    _waferMaterialSelected = "0";
                }
                else
                {
                    _waferMaterialSelected = "1";
                }
                
                NotifyOfPropertyChange("WaferMaterialSelected"); 
            }
        }
        //qbh 20220701
        public void Aligner_WaferSize(string sValue)
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwWaferSize", WaferSizeSelected);
        }
        public void Aligner_WaferType(string sValue)
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwWaferType", WaferTypeSelected);
        }
        public void Aligner_WaferMaterial(string sValue)
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwWaferMaterial", WaferMaterialSelected);
        }
        public void Aligner_WaferOrientation(string sValue)
        {
            string sDegree = sValue.Trim();
            if(sDegree.Length == 0)
            {
                EV.PostWarningLog("TM", "Aligner parameter error: Wafer Orientation is empty.");
                return;
            }
            int iDegree = -1;
            try
            {
                iDegree = Convert.ToInt32(sDegree);
            }
            catch(Exception ex)
            {
                EV.PostWarningLog("TM", "Aligner parameter error: Wafer Orientation degree should be in 0-3599.");
                return;
            }
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwWaferOrientation", sDegree);
        }
        public void Aligner_SaveParameters()
        {
            InvokeClient.Instance.Service.DoOperation($"TM.HiWinAligner.HwSaveParameters");
        }
        #endregion

        #region UPS
        //UPSA
        [Subscription("PM1.ITAUPSA.SystemStatus")]
        public string UPSA_SystemStatus { get; set; }
        //
        [Subscription("PM1.ITAUPSA.InputVoltage")]
        public float UPSA_InputVoltage { get; set; }
        //
        [Subscription("PM1.ITAUPSA.BatteryVoltage")]
        public float UPSA_BatteryVoltage { get; set; }
        //
        [Subscription("PM1.ITAUPSA.BatteryRemainsTime")]
        public float UPSA_BatteryRemainsTime { get; set; }
        //
        [Subscription("PM1.ITAUPSA.UtilityPowerFailure")]
        public bool UPSA_UtilityPowerFailure { get; set; }
        //
        [Subscription("PM1.ITAUPSA.BatteryUnderVoltage")]
        public bool UPSA_BatteryUnderVoltage { get; set; }

        //UPSB
        [Subscription("PM1.ITAUPSB.SystemStatus")]
        public string UPSB_SystemStatus { get; set; }
        //
        [Subscription("PM1.ITAUPSB.InputVoltage")]
        public float UPSB_InputVoltage { get; set; }
        //
        [Subscription("PM1.ITAUPSB.BatteryVoltage")]
        public float UPSB_BatteryVoltage { get; set; }
        //
        [Subscription("PM1.ITAUPSB.BatteryRemainsTime")]
        public float UPSB_BatteryRemainsTime { get; set; }
        //
        [Subscription("PM1.ITAUPSB.UtilityPowerFailure")]
        public bool UPSB_UtilityPowerFailure { get; set; }
        //
        [Subscription("PM1.ITAUPSB.BatteryUnderVoltage")]
        public bool UPSB_BatteryUnderVoltage { get; set; }
        #endregion

        #region Ring

        [Subscription("PM1.ConfinementRing.RingCurPos")]
        public float RingCurPos { get; set; }

        [Subscription("PM1.ConfinementRing.RingUpPos")]
        public float RingUpPos { get; set; }

        [Subscription("PM1.ConfinementRing.RingDownPos")]
        public float RingDownPos { get; set; }

        [Subscription("PM1.ConfinementRing.RingUpSensor")]
        public bool RingUpSensor { get; set; }

        [Subscription("PM1.ConfinementRing.RingDownSensor")]
        public bool RingDownSensor { get; set; }

        [Subscription("PM1.ConfinementRing.RingDone")]
        public bool RingDone { get; set; }

        [Subscription("PM1.ConfinementRing.RingIsServoOn")]
        public bool RingIsServoOn { get; set; }

        [Subscription("PM1.ConfinementRing.RingIsBusy")]
        public bool RingIsBusy { get; set; }

        [Subscription("PM1.ConfinementRing.RingIsAlarm")]
        public bool RingIsAlarm { get; set; }

        public bool ConfinementRingBtnEnable => !RingIsBusy && !RingIsAlarm && !PM1IsOnline && PM1Status != "Process";

        public void RingServoStop()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.ServoStop");
        }

        public void RingServoOn()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.ServoOn");
        }

        public void RingServoReset()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.ServoReset");
        }

        public void RingMoveUpPos()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.MoveUpPos");
        }

        public void RingMoveDownPos()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.MoveDownPos");
        }

        public void RingJogUp(float distance)
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.JogUp",distance);
        }

        public void RingJogDown(float distance)
        {
            InvokeClient.Instance.Service.DoOperation("PM1.ConfinementRing.JogDown",distance);
        }
        #endregion

    }
}
