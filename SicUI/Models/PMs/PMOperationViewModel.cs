using System;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Caliburn.Micro;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using System.Windows;
using SicUI.Controls;
using System.Windows.Media;
using MECF.Framework.UI.Client.CenterViews.Editors;
using Xceed.Wpf.Toolkit.Core.Converters;

namespace SicUI.Models.PMs
{
    public class PMOperationViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        [Subscription("Status")]
        public string Status { get; set; }

        [Subscription("IsService")]
        public bool IsService { get; set; }

        #region Enable
        //维护模式
        public bool IsServiceMode => (Status == "Idle" || Status == "Safety" || Status == "VacIdle" || Status == "ProcessIdle" || Status == "Error" || Status == "ServiceIdle") && !IsOnline;

        public bool IsEnableGasMap => (Status == "Idle" || Status == "VacIdle" || Status == "ServiceIdle") && !IsOnline;

        public bool EnableProcessIdle => (Status == "Idle" || Status == "VacIdle" || Status == "ProcessIdle" || Status == "Safety");

        public bool VacIdleEnable => (Status == "Idle" || Status == "VacIdle" || Status == "ProcessIdle" || Status == "Safety");

        public bool EnableIdleRoutine => (Status == "Idle" || Status == "Safety" || Status == "VacIdle") && !IsOnline;

        public bool EnableIdle => (Status == "ProcessIdle" || Status == "VacIdle" || Status == "Safety" || Status == "Idle" || Status == "ServiceIdle") && !IsOnline;

        public bool EnableService => (Status == "Idle" || Status == "Safety" || Status == "VacIdle" || Status == "Error" || Status == "ProcessIdle") && !IsOnline;

        public bool EnableExchangeMo => (Status == "Idle" || Status == "Safety" || Status == "VacIdle") && !IsOnline && IsService;

        public string LableStatue => IsService ? "Service" : Status;

        //LeakCheck后只能执行ATM Idle,其他Routine按钮不允许执行
        private bool EnableLeakCheck = true;

        public SolidColorBrush StatueLabelColor
        {
            get
            {
                if (IsService)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 50, 205, 50));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                }
            }
        }


        public Visibility VisbleOnlyIdle
        {
            get
            {
                return IsService ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region MFC

        public double V90CountMFC => Mfc31Data.FeedBack + (V63.IsOpen ? Mfc25Data.FeedBack : 0) + (V62.IsOpen ? Mfc22Data.FeedBack : 0) + (V61.IsOpen ? Mfc19Data.FeedBack : 0);
        public double V89CountMFC => Mfc29Data.FeedBack + (V63.IsOpen ? Mfc26Data.FeedBack : 0) + (V62.IsOpen ? Mfc23Data.FeedBack : 0) + (V61.IsOpen ? Mfc20Data.FeedBack : 0);
        public double V88CountMFC => Mfc28Data.FeedBack + (V63.IsOpen ? Mfc15Data.FeedBack : 0) + (V62.IsOpen ? Mfc9Data.FeedBack : 0)+ (V61.IsOpen ? Mfc2Data.FeedBack : 0);

        public double ACountMFC => PT4Data.OpenDegree > 0 ? (Mfc1Data.FeedBack + (V39.IsOpen ? 0 : Mfc6Data.FeedBack) + (V40.IsOpen ? 0 : Mfc5Data.FeedBack) 
            + (V41.IsOpen ? 0 : Mfc7Data.FeedBack) + Mfc8Data.FeedBack
            + (V53.IsOpen ? 0 : Mfc10Data.FeedBack + Mfc11Data.FeedBack) + Mfc12Data.FeedBack + (V54.IsOpen ? 0 : Mfc13Data.FeedBack)
            + (V55.IsOpen ? 0 : Mfc14Data.FeedBack) + (V59.IsOpen ? 0 : Mfc16Data.FeedBack)) : 0;

        public double BCountMFC => PT1Data.OpenDegree > 0 ? Mfc3Data.FeedBack + Mfc4Data.FeedBack - Mfc6Data.FeedBack : 0;

        //public double PC7Count => PT7Data.OpenDegree > 0 ? (V63.IsOpen ? Mfc15Data.FeedBack + (V59.IsOpen ? Mfc16Data.FeedBack : 0) - Mfc25Data.FeedBack - Mfc26Data.FeedBack : 0) : 0;

        //public double PC6Count => PT6Data.OpenDegree > 0 ? (V62.IsOpen ? Mfc9Data.FeedBack + (V54.IsOpen ? Mfc13Data.FeedBack : 0) + (V55.IsOpen ? Mfc14Data.FeedBack : 0)
        //    + (V53.IsOpen ? (PT3Data.OpenDegree > 0 ? Mfc11Data.FeedBack + Mfc10Data.FeedBack + Mfc12Data.FeedBack : 0) : 0) - Mfc22Data.FeedBack - Mfc23Data.FeedBack : 0) : 0;

        //public double PC5Count => PT5Data.OpenDegree > 0 ? (V61.IsOpen ? Mfc2Data.FeedBack + (V39.IsOpen ? Mfc6Data.FeedBack : 0) + (V40.IsOpen ? Mfc5Data.FeedBack : 0) + (V41.IsOpen ? (PT2Data.OpenDegree > 0 ? Mfc7Data.FeedBack + Mfc8Data.FeedBack : 0) : 0) - Mfc19Data.FeedBack - Mfc20Data.FeedBack : 0) : 0;




        [Subscription("Mfc1.DeviceData")]
        public AITMfcData Mfc1Data { get; set; }

        [Subscription("Mfc2.DeviceData")]
        public AITMfcData Mfc2Data { get; set; }

        [Subscription("Mfc3.DeviceData")]
        public AITMfcData Mfc3Data { get; set; }

        [Subscription("Mfc4.DeviceData")]
        public AITMfcData Mfc4Data { get; set; }

        [Subscription("Mfc5.DeviceData")]
        public AITMfcData Mfc5Data { get; set; }

        [Subscription("Mfc6.DeviceData")]
        public AITMfcData Mfc6Data { get; set; }

        [Subscription("Mfc7.DeviceData")]
        public AITMfcData Mfc7Data { get; set; }

        [Subscription("Mfc8.DeviceData")]
        public AITMfcData Mfc8Data { get; set; }

        [Subscription("Mfc9.DeviceData")]
        public AITMfcData Mfc9Data { get; set; }

        [Subscription("Mfc10.DeviceData")]
        public AITMfcData Mfc10Data { get; set; }

        [Subscription("Mfc11.DeviceData")]
        public AITMfcData Mfc11Data { get; set; }

        [Subscription("Mfc12.DeviceData")]
        public AITMfcData Mfc12Data { get; set; }

        [Subscription("Mfc13.DeviceData")]
        public AITMfcData Mfc13Data { get; set; }

        [Subscription("Mfc14.DeviceData")]
        public AITMfcData Mfc14Data { get; set; }

        [Subscription("Mfc15.DeviceData")]
        public AITMfcData Mfc15Data { get; set; }

        [Subscription("Mfc16.DeviceData")]
        public AITMfcData Mfc16Data { get; set; }



        [Subscription("Mfc19.DeviceData")]
        public AITMfcData Mfc19Data { get; set; }

        [Subscription("Mfc20.DeviceData")]
        public AITMfcData Mfc20Data { get; set; }



        [Subscription("Mfc22.DeviceData")]
        public AITMfcData Mfc22Data { get; set; }

        [Subscription("Mfc23.DeviceData")]
        public AITMfcData Mfc23Data { get; set; }



        [Subscription("Mfc25.DeviceData")]
        public AITMfcData Mfc25Data { get; set; }

        [Subscription("Mfc26.DeviceData")]
        public AITMfcData Mfc26Data { get; set; }

        [Subscription("Mfc27.DeviceData")]
        public AITMfcData Mfc27Data { get; set; }

        [Subscription("Mfc28.DeviceData")]
        public AITMfcData Mfc28Data { get; set; }

        [Subscription("Mfc29.DeviceData")]
        public AITMfcData Mfc29Data { get; set; }


        [Subscription("Mfc40.DeviceData")]
        public AITMfcData Mfc40Data { get; set; }

        [Subscription("Mfc31.DeviceData")]
        public AITMfcData Mfc31Data { get; set; }



        [Subscription("Mfc32.DeviceData")]
        public AITMfcData Mfc32Data { get; set; }

        [Subscription("Mfc33.DeviceData")]
        public AITMfcData Mfc33Data { get; set; }



        [Subscription("Mfc35.DeviceData")]
        public AITMfcData Mfc35Data { get; set; }

        [Subscription("Mfc36.DeviceData")]
        public AITMfcData Mfc36Data { get; set; }

        [Subscription("Mfc37.DeviceData")]
        public AITMfcData Mfc37Data { get; set; }

        [Subscription("Mfc38.DeviceData")]
        public AITMfcData Mfc38Data { get; set; }

        #endregion

        #region Pressure
        [Subscription("Pressure1.DeviceData")]
        public AITPressureMeterData PT1Data { get; set; }

        [Subscription("Pressure2.DeviceData")]
        public AITPressureMeterData PT2Data { get; set; }
        [Subscription("Pressure3.DeviceData")]
        public AITPressureMeterData PT3Data { get; set; }


        [Subscription("Pressure4.DeviceData")]
        public AITPressureMeterData PT4Data { get; set; }
        [Subscription("Pressure5.DeviceData")]
        public AITPressureMeterData PT5Data { get; set; }
        [Subscription("Pressure6.DeviceData")]
        public AITPressureMeterData PT6Data { get; set; }

        [Subscription("Pressure7.DeviceData")]
        public AITPressureMeterData PT7Data { get; set; }

        [Subscription("PT1.DeviceData")]
        public AITPressureMeterData ChamPress { get; set; }

        [Subscription("PT2.DeviceData")]
        public AITPressureMeterData ForelinePress { get; set; }

        public string ChamPressFeedBack
        {
            get { return ChamPress.FeedBack.ToString(ChamPress.FormatString); }
            set { }
        }



        #endregion


        #region Valve

        [Subscription("V25.DeviceData")]
        public AITValveData V25 { get; set; }

        [Subscription("V27.DeviceData")]
        public AITValveData V27 { get; set; }

        [Subscription("V31.DeviceData")]
        public AITValveData V31 { get; set; }

        [Subscription("V32.DeviceData")]
        public AITValveData V32 { get; set; }

        [Subscription("V33.DeviceData")]
        public AITValveData V33 { get; set; }

        [Subscription("V33s.DeviceData")]
        public AITValveData V33s { get; set; }

        [Subscription("V35.DeviceData")]
        public AITValveData V35 { get; set; }

        [Subscription("V36.DeviceData")]
        public AITValveData V36 { get; set; }

        [Subscription("V37.DeviceData")]
        public AITValveData V37 { get; set; }

        [Subscription("V37s.DeviceData")]
        public AITValveData V37s { get; set; }


        [Subscription("V39.DeviceData")]
        public AITValveData V39 { get; set; }

        [Subscription("V39s.DeviceData")]
        public AITValveData V39s { get; set; }

        [Subscription("V40.DeviceData")]
        public AITValveData V40 { get; set; }

        [Subscription("V40s.DeviceData")]
        public AITValveData V40s { get; set; }


        [Subscription("V41.DeviceData")]
        public AITValveData V41 { get; set; }

        [Subscription("V41s.DeviceData")]
        public AITValveData V41s { get; set; }


        [Subscription("V42.DeviceData")]
        public AITValveData V42 { get; set; }

        [Subscription("V43.DeviceData")]
        public AITValveData V43 { get; set; }

        [Subscription("V43s.DeviceData")]
        public AITValveData V43s { get; set; }


        [Subscription("V45.DeviceData")]
        public AITValveData V45 { get; set; }


        [Subscription("V46.DeviceData")]
        public AITValveData V46 { get; set; }

        [Subscription("V46s.DeviceData")]
        public AITValveData V46s { get; set; }

        [Subscription("V48.DeviceData")]
        public AITValveData V48 { get; set; }

        [Subscription("V48s.DeviceData")]
        public AITValveData V48s { get; set; }


        [Subscription("V49.DeviceData")]
        public AITValveData V49 { get; set; }

        [Subscription("V50.DeviceData")]
        public AITValveData V50 { get; set; }

        [Subscription("V50s.DeviceData")]
        public AITValveData V50s { get; set; }

        [Subscription("V51.DeviceData")]
        public AITValveData V51 { get; set; }

        [Subscription("V51s.DeviceData")]
        public AITValveData V51s { get; set; }


        [Subscription("V52.DeviceData")]
        public AITValveData V52 { get; set; }

        [Subscription("V52s.DeviceData")]
        public AITValveData V52s { get; set; }

        [Subscription("V53.DeviceData")]
        public AITValveData V53 { get; set; }

        [Subscription("V53s.DeviceData")]
        public AITValveData V53s { get; set; }

        [Subscription("V54.DeviceData")]
        public AITValveData V54 { get; set; }

        [Subscription("V54s.DeviceData")]
        public AITValveData V54s { get; set; }

        [Subscription("V55.DeviceData")]
        public AITValveData V55 { get; set; }

        [Subscription("V56.DeviceData")]
        public AITValveData V56 { get; set; }

        [Subscription("V58.DeviceData")]
        public AITValveData V58 { get; set; }

        [Subscription("V58s.DeviceData")]
        public AITValveData V58s { get; set; }



        [Subscription("V59.DeviceData")]
        public AITValveData V59 { get; set; }

        [Subscription("V60.DeviceData")]
        public AITValveData V60 { get; set; }

        [Subscription("V61.DeviceData")]
        public AITValveData V61 { get; set; }

        [Subscription("V62.DeviceData")]
        public AITValveData V62 { get; set; }

        [Subscription("V63.DeviceData")]
        public AITValveData V63 { get; set; }

        [Subscription("V64.DeviceData")]
        public AITValveData V64 { get; set; }

        [Subscription("V65.DeviceData")]
        public AITValveData V65 { get; set; }

        [Subscription("V68.DeviceData")]
        public AITValveData V68 { get; set; }

        [Subscription("V69.DeviceData")]
        public AITValveData V69 { get; set; }

        [Subscription("V70.DeviceData")]
        public AITValveData V70 { get; set; }

        [Subscription("V72.DeviceData")]
        public AITValveData V72 { get; set; }

        [Subscription("V73.DeviceData")]
        public AITValveData V73 { get; set; }

        [Subscription("V74.DeviceData")]
        public AITValveData V74 { get; set; }

        [Subscription("V75.DeviceData")]
        public AITValveData V75 { get; set; }

        [Subscription("V76.DeviceData")]
        public AITValveData V76 { get; set; }

        [Subscription("V87.DeviceData")]
        public AITValveData V87 { get; set; }

        [Subscription("V88.DeviceData")]
        public AITValveData V88 { get; set; }

        [Subscription("V89.DeviceData")]
        public AITValveData V89 { get; set; }

        [Subscription("V90.DeviceData")]
        public AITValveData V90 { get; set; }

        [Subscription("V91.DeviceData")]
        public AITValveData V91 { get; set; }

        [Subscription("V92.DeviceData")]
        public AITValveData V92 { get; set; }

        [Subscription("V93.DeviceData")]
        public AITValveData V93 { get; set; }

        [Subscription("V94.DeviceData")]
        public AITValveData V94 { get; set; }

        [Subscription("V95.DeviceData")]
        public AITValveData V95 { get; set; }

        [Subscription("V96.DeviceData")]
        public AITValveData V96 { get; set; }

        [Subscription("V97.DeviceData")]
        public AITValveData V97 { get; set; }

        [Subscription("V99.DeviceData")]
        public AITValveData V99 { get; set; }

        [Subscription("V99s.DeviceData")]
        public AITValveData V99s { get; set; }


        [Subscription("EPV2.DeviceData")]
        public AITValveData EPV2 { get; set; }


        #endregion

        #region Temp


        [Subscription("TC2.L3InputTempSetPoint")]
        public float SCRL3InputTemp { get; set; }

        [Subscription("TC1.L1InputTempSetPoint")]
        public float L1InputTemp { get; set; }


        [Subscription("TC1.L2InputTempSetPoint")]
        public float L2InputTemp { get; set; }

        [Subscription("TC1.L3InputTempSetPoint")]
        public float L3InputTemp { get; set; }


        [Subscription("SHFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp1 { get; set; }

        [Subscription("ChamTopFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp2 { get; set; }

        [Subscription("ChamMiddleFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp3 { get; set; }

        [Subscription("ChamBottomFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp4 { get; set; }

        [Subscription("BottomPlateFlowTemp.DeviceData")]
        public AITDeviceData FlowTempr5 { get; set; }

        [Subscription("PowerRod1FlowTemp.DeviceData")]
        public AITDeviceData FlowTemp6 { get; set; }

        [Subscription("PowerRod2FlowTemp.DeviceData")]
        public AITDeviceData FlowTemp7 { get; set; }

        [Subscription("ForelineColdTrapFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp8 { get; set; }

        [Subscription("InSituFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp9 { get; set; }

        [Subscription("SideWallPowerRodFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp10 { get; set; }

        [Subscription("TMPump1FlowTemp.DeviceData")]
        public AITDeviceData TMPump1Temp { get; set; }

        [Subscription("TMPump2FlowTemp.DeviceData")]
        public AITDeviceData TMPump2Temp { get; set; }

        [Subscription("InletTotalFlowTemp.DeviceData")]
        public AITDeviceData FlowTemp12 { get; set; }




        [Subscription("SHFlowTemp.FeedBack")]
        public float FlowTemp1Faceback { get; set; }

        [Subscription("ChamTopFlowTemp.FeedBack")]
        public float FlowTemp2Faceback { get; set; }

        [Subscription("ChamMiddleFlowTemp.FeedBack")]
        public float FlowTemp3Faceback { get; set; }

        [Subscription("ChamMiddleFlow2Temp.FeedBack")]
        public float FlowTemp4Faceback { get; set; }

        [Subscription("ChamBottomFlowTemp.FeedBack")]
        public float FlowTemp5Faceback { get; set; }

        [Subscription("BottomPlateFlowTemp.FeedBack")]
        public float FlowTemp6Faceback { get; set; }

        [Subscription("PowerRod1FlowTemp.FeedBack")]
        public float FlowTemp7Faceback { get; set; }

        [Subscription("PowerRod2FlowTemp.FeedBack")]
        public float FlowTemp8Faceback { get; set; }

        [Subscription("ForelineColdTrapFlowTemp.FeedBack")]
        public float FlowTemp9Faceback { get; set; }

        [Subscription("InSituFlowTemp.FeedBack")]
        public float FlowTemp10Faceback { get; set; }

        [Subscription("TMPump1FlowTemp.FeedBack")]
        public float TMPump1TempFaceback { get; set; }

        [Subscription("TMPump2FlowTemp.FeedBack")]
        public float TMPump2TempFaceback { get; set; }

        [Subscription("TMFlow1Temp.FeedBack")]
        public float FlowTemp12Faceback { get; set; }

        [Subscription("TMFlow2Temp.FeedBack")]
        public float FlowTemp13Faceback { get; set; }
        [Subscription("TransformerFlowTemp.FeedBack")]
        public float FlowTemp14Faceback { get; set; }



        [Subscription("SHFlowTemp.FlowSW")]
        public bool FlowSW1 { get; set; }

        [Subscription("ChamTopFlowTemp.FlowSW")]
        public bool FlowSW2 { get; set; }

        [Subscription("ChamMiddleFlowTemp.FlowSW")]
        public bool FlowSW3 { get; set; }

        [Subscription("ChamMiddleFlow2Temp.FlowSW")]
        public bool FlowSW4 { get; set; }

        [Subscription("ChamBottomFlowTemp.FlowSW")]
        public bool FlowSW5 { get; set; }

        [Subscription("BottomPlateFlowTemp.FlowSW")]
        public bool FlowSW6 { get; set; }

        [Subscription("PowerRod1FlowTemp.FlowSW")]
        public bool FlowSW7 { get; set; }

        [Subscription("PowerRod2FlowTemp.FlowSW")]
        public bool FlowSW8 { get; set; }

        [Subscription("ForelineColdTrapFlowTemp.FlowSW")]
        public bool FlowSW9 { get; set; }

        [Subscription("InSituFlowTemp.FlowSW")]
        public bool FlowSW10 { get; set; }

        [Subscription("TMPump1FlowTemp.FlowSW")]
        public bool TMPump1FlowSW { get; set; }

        [Subscription("TMPump2FlowTemp.FlowSW")]
        public bool TMPump2FlowSW { get; set; }

        [Subscription("TMFlow1Temp.FlowSW")]
        public bool FlowSW12 { get; set; }

        [Subscription("TMFlow2Temp.FlowSW")]
        public bool FlowSW13 { get; set; }

        [Subscription("TransformerFlowTemp.FlowSW")]
        public bool FlowSW14 { get; set; }


        #endregion

        #region Pump
        [Subscription("Pump.DeviceData")]
        public AITPumpData PumpData { get; set; }

        [Subscription("PMDRYVacuumPump.DeviceDataMP")]
        public AITPumpData PumpDataMP { get; set; }

        [Subscription("PMDRYVacuumPump.DeviceDataBP")]
        public AITPumpData PumpDataBP { get; set; }

        #endregion

        #region Sensor

        [Subscription("SensorChamLidClosed.DeviceData")]
        public AITSensorData Sensor1 { get; set; }

        [Subscription("CleanRoutineSucceed.DeviceData")]
        public AITSensorData CleanRoutineSucceed { get; set; }

        //[Subscription("Sensor2.DeviceData")]
        //public AITSensorData Sensor2 { get; set; }

        //[Subscription("SensorChamBodyOTSW.DeviceData")]
        //public AITSensorData Sensor3 { get; set; }

        [Subscription("SensorChamCabDoorClosed.DeviceData")]
        public AITSensorData Sensor4 { get; set; }

        [Subscription("SensorChamPressAboveATMSW.DeviceData")]
        public AITSensorData Sensor5 { get; set; }

        [Subscription("SensorChamAtSafeProcessPressSW.DeviceData")]
        public AITSensorData Sensor6 { get; set; }

        //[Subscription("Sensor7.DeviceData")]
        //public AITSensorData Sensor7 { get; set; }

        [Subscription("SensorDORPressATMSW.DeviceData")]
        public AITSensorData Sensor8 { get; set; }

        [Subscription("SensorHeaterTempBelow900CSW.DeviceData")]
        public AITSensorData Sensor9 { get; set; }

        [Subscription("SensorConfinementRingUp.DeviceData")]
        public AITSensorData Sensor10 { get; set; }

        ////[Subscription("Sensor11.DeviceData")]
        ////public AITSensorData Sensor11 { get; set; }

        ////[Subscription("Sensor12.DeviceData")]
        ////public AITSensorData Sensor12 { get; set; }

        ////[Subscription("Sensor13.DeviceData")]
        ////public AITSensorData Sensor13 { get; set; }

        ////[Subscription("Sensor14.DeviceData")]
        ////public AITSensorData Sensor14 { get; set; }

        ////[Subscription("Sensor15.DeviceData")]
        ////public AITSensorData Sensor15 { get; set; }

        ////[Subscription("Sensor16.DeviceData")]
        ////public AITSensorData Sensor16 { get; set; }

        ////[Subscription("Sensor17.DeviceData")]
        ////public AITSensorData Sensor17 { get; set; }

        ////[Subscription("Sensor18.DeviceData")]
        ////public AITSensorData Sensor18 { get; set; }

        ////[Subscription("Sensor19.DeviceData")]
        ////public AITSensorData Sensor19 { get; set; }

        ////[Subscription("Sensor20.DeviceData")]
        ////public AITSensorData Sensor20 { get; set; }

        ////[Subscription("Sensor21.DeviceData")]
        ////public AITSensorData Sensor21 { get; set; }

        [Subscription("SensorPMH2DetectorSW.DeviceData")]
        public AITSensorData Sensor22 { get; set; }

        [Subscription("SensorGBHCLDetectorSW.DeviceData")]
        public AITSensorData Sensor23 { get; set; }

        [Subscription("SensorGBDoorClosed.DeviceData")]
        public AITSensorData Sensor24 { get; set; }

        [Subscription("SensorDryPumpAlarm.DeviceData")]
        public AITSensorData Sensor25 { get; set; }

        [Subscription("SensorPumpExhaustPressSW.DeviceData")]
        public AITSensorData Sensor26 { get; set; }

        [Subscription("SensorPumpExhaustDPSW.DeviceData")]
        public AITSensorData Sensor27 { get; set; }

        [Subscription("SensorScrubberIntlkSW.DeviceData")]
        public AITSensorData Sensor28 { get; set; }

        [Subscription("SensorFacilityIntlkSW.DeviceData")]
        public AITSensorData Sensor29 { get; set; }

        [Subscription("SensorReactorWaterLeakSW.DeviceData")]
        public AITSensorData Sensor30 { get; set; }

        [Subscription("SensorGBWaterLeakSW.DeviceData")]
        public AITSensorData Sensor33 { get; set; }

        [Subscription("SensorPRWaterLeakSW.DeviceData")]
        public AITSensorData Sensor34 { get; set; }



        #endregion


        [Subscription("PSU1.AllHeatEnable")]
        public bool AllHeatEnable { get; set; }

        //底部温度和侧壁功率
        [Subscription("TC1.TempCtrlTCIN")]
        public float TC1Temp2 { get; set; }

        [Subscription("SCR1.PowerFeedBack")]
        public float SCR1Power { get; set; }

        [Subscription("SCR2.PowerFeedBack")]
        public float SCR2Power { get; set; }

        [Subscription("SCR3.PowerFeedBack")]
        public float SCR3Power { get; set; }

        [Subscription("PSU1.OutputPowerFeedBack")]
        public float PSU1Power { get; set; }

        [Subscription("PSU2.OutputPowerFeedBack")]
        public float PSU2Power { get; set; }

        [Subscription("PSU3.OutputPowerFeedBack")]
        public float PSU3Power { get; set; }



        [Subscription("PMServo.ActualSpeedFeedback")]
        public float ActualSpeedFeedback { get; set; }


        [Subscription("PSU1.OutputArmsFeedBack")]
        public float OutputArmsFeedBack { get; set; }

        public string PSUArmStr => OutputArmsFeedBack.ToString("0.0");

        [Subscription("PSU1.OutputPowerFeedBack")]
        public float OutputPowerFeedBack { get; set; }

        public string PSUPowerStr => OutputPowerFeedBack.ToString("0.0");

        [Subscription("GasConnector.GasConnectorTightenFeedback")]
        public bool IsGasConnectorTighten { get; set; }



        [Subscription("TV.DeviceData")]
        public AITThrottleValveData TV { get; set; }

        [Subscription("SHLid.Status")]
        public string SHLidStatus { get; set; }

        public Visibility SHLidIsOpen
        {
            get
            {
                if (SHLidStatus == "Loosen") return Visibility.Hidden;
                return Visibility.Visible;
            }
        }

        public string LidTopColor
        {
            get
            {
                if (SHLidStatus == "Unknown") return "Yellow";
                if (SHLidStatus == "Error") return "Red";
                return "DimGray";
            }
        }

        [Subscription("BottomSection.Status")]
        public string BottomSectionStatus { get; set; }

        [Subscription("ChamberLiftPin.UpFeedback")]
        public bool UpFeedback { get; set; }

        [Subscription("ChamberLiftPin.DownFeedback")]
        public bool DownFeedback { get; set; }

        [Subscription("ChamberLiftPin.State")]
        public string LiftStatus
        {
            get;
            set;
        }

        public bool IsLiftUpEnable
        {
            get { return IsPMIdle && LiftStatus != "Up" && !IsOnline; }
        }
        public bool IsLiftDownEnable
        {
            get { return IsPMIdle && LiftStatus != "Down" && !IsOnline; }
        }

        [Subscription("ChamberDoor.State")]
        public string ChamberDoorState { get; set; }

        public bool IsChamberDoorOpenEnable
        {
            get { return IsPMIdle && ChamberDoorState != "Open" && !IsOnline; }
        }
        public bool IsChamberDoorCloseEnable
        {
            get { return IsPMIdle && ChamberDoorState != "Close" && !IsOnline; }
        }

        public string DoorColor
        {
            get
            {
                if (ChamberDoorState == "Unknown") return "Yellow";
                if (ChamberDoorState == "Error") return "Red";
                return "DimGray";
            }
        }



        [Subscription("VentValve.DeviceData")]
        public AITValveData VentValve { get; set; }

        [Subscription("Gas1Valve.DeviceData")]
        public AITValveData Gas1Valve { get; set; }

        [Subscription("Gas2Valve.DeviceData")]
        public AITValveData Gas2Valve { get; set; }

        [Subscription("Gas3Valve.DeviceData")]
        public AITValveData Gas3Valve { get; set; }

        [Subscription("Gas4Valve.DeviceData")]
        public AITValveData Gas4Valve { get; set; }

        [Subscription("FinalValve.DeviceData")]
        public AITValveData FinalValve { get; set; }

        [Subscription("ElectricalCoolingValve.DeviceData")]
        public AITValveData ElectricalCoolingValve { get; set; }

        [Subscription("ChamberLidCoolingValve.DeviceData")]
        public AITValveData ChamberLidCoolingValve { get; set; }

        [Subscription("MicrowaveCoolingValve.DeviceData")]
        public AITValveData MicrowaveCoolingValve { get; set; }

        [Subscription("RF.DeviceData")]
        public AITRfData RfData { get; set; }

        [Subscription("Microwave.DeviceData")]
        public AITRfData MicrowaveData { get; set; }

        [Subscription("ChamberHeater.DeviceData")]
        public AITHeaterData ChamberHeaterData { get; set; }

        [Subscription("ThrottleValve.DeviceData")]
        public AITThrottleValveData ThrottleValveData { get; set; }

        [Subscription("ChamberPressure.DeviceData")]
        public AITPressureMeterData ChamberPressure { get; set; }

        //[Subscription("CDAPressure.DeviceData")]
        //public AITPressureMeterData CDAPressure { get; set; }

        [Subscription("ChamberMonitorPressure.DeviceData")]
        public AITPressureMeterData ChamberMonitorPressure { get; set; }

        //[Subscription("Gas1Pressure.DeviceData")]
        //public AITPressureMeterData Gas1PressureData { get; set; }

        //[Subscription("Gas2Pressure.DeviceData")]
        //public AITPressureMeterData Gas2PressureData { get; set; }

        //[Subscription("Gas3Pressure.DeviceData")]
        //public AITPressureMeterData Gas3PressureData { get; set; }

        //[Subscription("Gas4Pressure.DeviceData")]
        //public AITPressureMeterData Gas4PressureData { get; set; }


        [Subscription("SensorEmo.DeviceData")]
        public AITSensorData SensorEmo { get; set; }

        [Subscription("SensorLidClosed.DeviceData")]
        public AITSensorData SensorLidClosed { get; set; }

        [Subscription("SensorDoor1Open.DeviceData")]
        public AITSensorData SensorDoor1Open { get; set; }

        [Subscription("SensorDoor2Open.DeviceData")]
        public AITSensorData SensorDoor2Open { get; set; }

        [Subscription("SensorDoor3Open.DeviceData")]
        public AITSensorData SensorDoor3Open { get; set; }

        [Subscription("SensorDoor4Open.DeviceData")]
        public AITSensorData SensorDoor4Open { get; set; }

        [Subscription("SensorVentFlowSwitch.DeviceData")]
        public AITSensorData SensorVentFlowSwitch { get; set; }

        [Subscription("SensorVacuumSwitch.DeviceData")]
        public AITSensorData SensorVacuumSwitch { get; set; }

        [Subscription("MainPump.DeviceData")]
        public AITPumpData MainPumpData { get; set; }

        [Subscription("SelectedRecipeName")]
        public string SelectedRecipeName
        {
            get;
            set;
        }

        public string SelectedRecipeNameDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedRecipeName))
                    return string.Empty;
                return SelectedRecipeName.Substring("Sic\\Process\\".Length);
            }
        }

        [Subscription("StepName")]
        public string RecipeStepName { get; set; }

        [Subscription("StepNumber")]
        public string RecipeStepNumber { get; set; }

        [Subscription("ElapseTime")]
        public double RecipeElapseTime { get; set; }

        [Subscription("IsOnline")]
        public bool IsOnline { get; set; }
        public string OnlineState => IsOnline ? "Online" : "Offline";
        public string OnlineButtomContent => IsOnline ? "Offline" : "Online";

        public bool IsOnlineEnable
        {
            get { return IsPMIdle && !IsOnline; }
        }
        public bool IsOfflineEnable
        {
            get { return IsPMIdle && IsOnline; }
        }

        [Subscription("Status")]
        public string PMStatus { get; set; }
        public string PMStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(PMStatus); }
        }
        public bool IsAbortEnable
        {
            get { return !IsPMIdle && PMStatus != "Init" && !IsPMError; }
        }
        public bool IsPMError
        {
            get { return PMStatus == "Error"; }
        }

        public bool IsPMIdle
        {
            get { return PMStatus == "Idle"; }
        }

        public bool IsEnableOperation
        {
            get
            {
                return PMStatus == "Idle" && !IsOnline;
            }
        }

        public bool IsHomed => PMStatus != "Init";
        public bool IsStartButtonEnable => PMStatus == "Idle" && !IsOnline;
        public bool IsSkipButtonEnable => PMStatus == "Process" && !IsOnline;
        public bool IsStopButtonEnable => PMStatus == "Process" && !IsOnline;

        public WaferInfo PMWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos[Module].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos[Module].WaferManager.Wafers[0];
                return new WaferInfo();
            }
        }

        public bool IsPermission { get => this.Permission == 3; }

        public string Module => SystemName;


        public bool IsEnableOnline
        {
            get { return !IsOnline; }
        }

        public bool IsEnableOffline
        {
            get { return IsOnline; }
        }
        #region MoveSpeed

        private const double movespeed = 0;
        public double V31MoveSpeed
        {
            get
            {
                if (V31.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V32MoveSpeed
        {
            get
            {
                if (V32.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V33MoveSpeed
        {
            get
            {
                if (V33.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V33sMoveSpeed
        {
            get
            {
                if (V33s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V35MoveSpeed
        {
            get
            {
                if (V35.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V36MoveSpeed
        {
            get
            {
                if (V36.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V37sMoveSpeed
        {
            get
            {
                if (V37s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V37MoveSpeed
        {
            get
            {
                if (V37.Feedback) return movespeed;
                else return 0;
            }
        }

        public double V43MoveSpeed
        {
            get
            {
                if (V43.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V45MoveSpeed
        {
            get
            {
                if (V45.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V46MoveSpeed
        {
            get
            {
                if (V46.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V46sMoveSpeed
        {
            get
            {
                if (V46s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V46_46sMoveSpeed
        {
            get
            {
                if (V46.Feedback || V46s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V43sMoveSpeed
        {
            get
            {
                if (V43s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V48MoveSpeed
        {
            get
            {
                if (V48.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V50MoveSpeed
        {
            get
            {
                if (V50.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V50sMoveSpeed
        {
            get
            {
                if (V50s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V50_50sMoveSpeed
        {
            get
            {
                if (V50.Feedback || V50s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V49MoveSpeed
        {
            get
            {
                if (V49.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V48sMoveSpeed
        {
            get
            {
                if (V48s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V51MoveSpeed
        {
            get
            {
                if (V51.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V51sMoveSpeed
        {
            get
            {
                if (V51s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V51_51sMoveSpeed
        {
            get
            {
                if (V51.Feedback || V51s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V52MoveSpeed
        {
            get
            {
                if (V52.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V52sMoveSpeed
        {
            get
            {
                if (V52s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V52_52sMoveSpeed
        {
            get
            {
                if (V52.Feedback || V52s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V58MoveSpeed
        {
            get
            {
                if (V58.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V58sMoveSpeed
        {
            get
            {
                if (V58s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V58_58sMoveSpeed
        {
            get
            {
                if (V58.Feedback || V58s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V53sMoveSpeed
        {
            get
            {
                if (V53s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V54sMoveSpeed
        {
            get
            {
                if (V54s.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V56MoveSpeed
        {
            get
            {
                if (V56.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V60MoveSpeed
        {
            get
            {
                if (V60.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V59MoveSpeed
        {
            get
            {
                if (V59.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V53MoveSpeed
        {
            get
            {
                if (V53.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V54MoveSpeed
        {
            get
            {
                if (V54.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V55MoveSpeed
        {
            get
            {
                if (V55.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V61MoveSpeed
        {
            get
            {
                if (V61.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V62MoveSpeed
        {
            get
            {
                if (V62.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V63MoveSpeed
        {
            get
            {
                if (V63.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V62_63MoveSpeed
        {
            get
            {
                if (V62.Feedback || V63.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V68MoveSpeed
        {
            get
            {
                if (V68.Feedback) return movespeed;
                else return 0;
            }
        }

        public double V69MoveSpeed
        {
            get
            {
                if (V69.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V70MoveSpeed
        {
            get
            {
                if (V70.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V72MoveSpeed
        {
            get
            {
                if (V72.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V73MoveSpeed
        {
            get
            {
                if (V73.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V74MoveSpeed
        {
            get
            {
                if (V74.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V75MoveSpeed
        {
            get
            {
                if (V75.Feedback) return movespeed;
                else return 0;
            }
        }
        public double V76MoveSpeed
        {
            get
            {
                if (V76.Feedback) return movespeed;
                else return 0;
            }
        }


        #endregion

        [IgnorePropertyChange]
        public string TVModeSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string TVPositionSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string TVPressureSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string Gas1MfcSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string Gas2MfcSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string Gas3MfcSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string Gas4MfcSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string MicrowavePowerSetPoint
        {
            get;
            set;
        }
        [IgnorePropertyChange]
        public string RfPowerSetPoint
        {
            get;
            set;
        }
        public ICommand CmdTvEnable { get; set; }
        public ICommand CmdTvPostionToZero { get; set; }
        public ICommand CmdTurnGasValve { get; set; }
        public ICommand CmdSetMfcFlow { get; set; }

        public PMOperationViewModel()
        {
            this.DisplayName = "Operation";
            CmdTvEnable = new DelegateCommand<object>(PerformCmdTvEnable);
            CmdTurnGasValve = new DelegateCommand<object>(PerformCmdTurnGasValve);
            CmdSetMfcFlow = new DelegateCommand<object>(PerformCmdSetMfcFlow);
        }
        private void PerformCmdTvEnable(object data)
        {
            var pair = (KeyValuePair<string, string>)data;
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.TV.SetTVValveEnable", pair.Value);
        }
        private void PerformCmdTurnGasValve(object data)
        {
            var pair = (KeyValuePair<string, string>)data;
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{pair.Key}.{AITValveOperation.GVTurnValve}", pair.Value);
        }
        private void PerformCmdSetMfcFlow(object param)
        {
            object[] args = (object[])param; //0:devicename, 1:operation, 2:args
            if (args.Length == 3)
            {
                InvokeClient.Instance.Service.DoOperation($"{args[0]}.Ramp", args[2]);
            }
        }

        /// <summary>
        /// 是否显示真实的MFC值，否则加随机扰动。
        /// </summary>
        private bool _isAddMfcValueDisturbance = true;

        public double MfcValueDisturbance
        {
            get;
            set;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            //var serviceMode = QueryDataClient.Instance.Service.GetConfig("System.IsServiceControlMode");
            //EnableServiceControl = (bool)serviceMode;
            var roleID = BaseApp.Instance.UserContext.RoleID;
            //HasMfcValueVisibilityPermission = RoleAccountProvider.Instance.GetMenuPermission(roleID, "PM1.Process.MfcValueVisibility") == 3;
            HasMfcValueVisibilityPermission = true;

            _isAddMfcValueDisturbance = RoleAccountProvider.Instance.GetMenuPermission(roleID, "PM1.Process.MfcValueDisturbance") != (int)MenuPermissionEnum.MP_NONE;

            if (_isAddMfcValueDisturbance)
            {
                var abc = QueryDataClient.Instance.Service.GetConfig($"PM.{Module}.MFC.Disturbance");
                if (double.TryParse(abc.ToString(), out var dbl))
                    MfcValueDisturbance = dbl;
                else
                    MfcValueDisturbance = 0;

            }
            else
                MfcValueDisturbance = 0;

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            base.InitPM();

            //权限
            string roleID = BaseApp.Instance.UserContext.RoleID;
            reactorStatusEnable = RoleAccountProvider.Instance.GetMenuPermission(roleID, "PM1.Main.ReactorStatus") == 3;
            reactorServiceEnable = RoleAccountProvider.Instance.GetMenuPermission(roleID, "PM1.Main.ReactorService") == 3;
        }

        /// <summary>
        /// 是否具有查看MFC值的权限。
        /// </summary>
        public bool HasMfcValueVisibilityPermission
        {
            get;
            set;
        }

        private bool reactorStatusEnable;
        public bool ReactorStatusEnable
        {
            get
            {
                return reactorStatusEnable;
            }
        }

        private bool reactorServiceEnable;
        public bool ReactorServiceEnable
        {
            get
            {
                return reactorServiceEnable;
            }
        }

        public void MoveLiftPinUp()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ChamberLiftPin.MoveUp");
        }

        public void MoveLiftPinDown()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ChamberLiftPin.MoveDown");
        }

        public void BottomSectionUp()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.BottomSection.Up");
        }

        public void BottomSectionDown()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.BottomSection.Down");
        }

        public void OpenChamberDoor()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ChamberDoor.Open");
        }

        public void CloseChamberDoor()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.ChamberDoor.Close");
        }

        public void SetOnline()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.PutOnline");
        }

        public void SetOffline()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.PutOffline");
        }

        public void OnOffline()
        {
            if (IsOnline)
            {
                InvokeClient.Instance.Service.DoOperation($"{Module}.SetOffline");
            }
            else
            {
                InvokeClient.Instance.Service.DoOperation($"{Module}.SetOnline");
            }
        }

        public void Home()
        {
            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
             "Are you sure perform the operation Home");
            if (selection == DialogButton.Yes)
            {
                InvokeClient.Instance.Service.DoOperation($"{Module}.Home");
            }
        }

        public void Reset()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.Reset");
        }

        public void ToService()
        {
            if (IsService == false)
            {
                var retDialog = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                    "Are you sure perform the operation Service?");
                if (retDialog == DialogButton.No)
                    return;
            }
            else
            {
                var retDialog = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                    "Are you sure to exit Service mode?");
                if (retDialog == DialogButton.No)
                    return;
            }

            // warn the operator for the high temperature if switching to ServiceIdle from ProcessIdle.
            if (Status == "ProcessIdle" && IsService == false)
            {
                DialogBox.ShowDialog(DialogButton.OK, DialogType.WARNING,
                    "Be aware of HIGH TEMPERATURE due to switching from ProcessIdle state !!");
            }

            InvokeClient.Instance.Service.DoOperation($"{Module}.SetToServiceIdle");
        }

        public bool ShowChoosenDialog(string strInfo)
        {
            ChooseDialogBoxViewModel dialog = new ChooseDialogBoxViewModel();
            dialog.DisplayName = "Tips";
            dialog.InfoStr = strInfo;


            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if (!bret.HasValue || !bret.Value)
            {
                return false;
            }
            return true;
        }

        public void SelectRecipe()
        {
            var dialog = new RecipeSelectDialogViewModel
            {
                DisplayName = "Select Recipe"
            };

            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if (bret == true)
            {
                var array = dialog.DialogResult.Split(new char[] { '\\' });

                InvokeClient.Instance.Service.DoOperation($"{SystemName}.SelectRecipe", dialog.DialogResult);
            }
        }

        public void Start()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.RunRecipe", SelectedRecipeName);
        }

        public void Skip()
        {
            InvokeClient.Instance.Service.DoOperation($"{Module}.RecipeSkipStep");
        }


        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            base.InvokeAfterUpdateProperty(data);
        }

        public void SetTVMode()
        {
            if (string.IsNullOrEmpty(TVModeSetPoint))
            {
                DialogBox.ShowWarning("Select TV mode first");
                return;
            }

            PressureCtrlMode mode = PressureCtrlMode.Undefined;
            switch (TVModeSetPoint)
            {
                case "Pressure":
                    mode = PressureCtrlMode.TVPressureCtrl;
                    break;
                case "Position":
                    mode = PressureCtrlMode.TVPositionCtrl;
                    break;
                case "Close":
                    mode = PressureCtrlMode.TVClose;
                    break;
                default:
                    DialogBox.ShowWarning($"Invalid TV mode {TVModeSetPoint}");
                    break;
            }

            InvokeClient.Instance.Service.DoOperation($"{ThrottleValveData.Module}.{ThrottleValveData.DeviceName}.{AITThrottleValveOperation.SetMode}", mode.ToString());
        }

        public void SetTVPosition()
        {
            if (string.IsNullOrEmpty(TVPositionSetPoint) || !float.TryParse(TVPositionSetPoint, out float setpoint))
            {
                DialogBox.ShowWarning($"Invalid value {TVPositionSetPoint}");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"{ThrottleValveData.Module}.{ThrottleValveData.DeviceName}.{AITThrottleValveOperation.SetPosition}", setpoint.ToString());
        }

        public void SetTVPressure()
        {
            if (string.IsNullOrEmpty(TVPressureSetPoint) || !float.TryParse(TVPressureSetPoint, out float setpoint))
            {
                DialogBox.ShowWarning($"Invalid value {TVPressureSetPoint}");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"{ThrottleValveData.Module}.{ThrottleValveData.DeviceName}.{AITThrottleValveOperation.SetPressure}", setpoint.ToString());
        }

        public void SetMfcFlow(int index)
        {
            string gasLine = "";
            string value = "";
            if (index == 1)
            {
                value = Gas1MfcSetPoint;
                gasLine = "GasLine1";
            }
            else if (index == 2)
            {
                value = Gas2MfcSetPoint;
                gasLine = "GasLine2";
            }
            else if (index == 3)
            {
                value = Gas3MfcSetPoint;
                gasLine = "GasLine3";
            }
            else if (index == 4)
            {
                value = Gas4MfcSetPoint;
                gasLine = "GasLine4";
            }

            if (string.IsNullOrEmpty(gasLine) || string.IsNullOrEmpty(value) || !float.TryParse(value, out float setpoint))
            {
                DialogBox.ShowWarning($"Invalid value {value}");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{gasLine}.Flow", setpoint.ToString());

        }
        public void SetMicrowavePower()
        {
            if (string.IsNullOrEmpty(MicrowavePowerSetPoint) || !float.TryParse(MicrowavePowerSetPoint, out float setpoint))
            {
                DialogBox.ShowWarning($"Invalid value {MicrowavePowerSetPoint}");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"{MicrowaveData.Module}.{MicrowaveData.DeviceName}.{AITRfOperation.SetPower}", setpoint.ToString());
        }
        public void SetRfPower()
        {
            if (string.IsNullOrEmpty(RfPowerSetPoint) || !float.TryParse(RfPowerSetPoint, out float setpoint))
            {
                DialogBox.ShowWarning($"Invalid value {RfPowerSetPoint}");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"{RfData.Module}.{RfData.DeviceName}.{AITRfOperation.SetPower}", setpoint.ToString());
        }

        //Routine操作统一为一个函数
        public void PMOperation(string strPMOP)
        {
            if(strPMOP == "LeakCheck")
            {
                EnableLeakCheck = false;
            }

            if (!IsGasConnectorTighten)
            {
                DialogBox.ShowDialog(DialogButton.OK, DialogType.CONFIRM,
              "Gas Connector is not Tighten,can not Execute Opearation");

                return;
            }

            //有些特殊处理
            if (strPMOP.Contains("ToProcessIdle"))
            {
                //如果加热Enable未打开则返回
                if (!AllHeatEnable)
                {
                    ShowChoosenDialog("1.Make sure the HeatEnable is turned on!");
                    return;
                }
                if (!CleanRoutineSucceed.Value)
                {
                    ShowChoosenDialog("3.Make sure the DO_CleanRoutineSucceed[DO-173]!");
                    return;
                }
            }

            if (strPMOP.Contains("TMA"))
            {
                if (!ShowChoosenDialog("1.Make sure the TMA tap has turned off \r\n2.Make sure the confinement in up position!\r\n3.Press OK to continue! "))
                {
                    return;
                }
            }

            if (strPMOP.Contains("TCS"))
            {
                if (!ShowChoosenDialog("1.Make sure the TCS tap has turned off \r\n2.Make sure the confinement in up position!\r\n3.Press OK to continue! "))
                {
                    return;
                }
            }

            //Abort不需要弹框提示
            if (strPMOP.Contains("Abort"))
            {
                InvokeClient.Instance.Service.DoOperation($"{Module}.{strPMOP}");
                return;
            }

            //去除strPMOP中To关键字
            string strCleanTo = strPMOP.Replace("To", "");
            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM, $"Are you sure perform the operation {strCleanTo}");
            if (selection == DialogButton.Yes)
            {
                if (strPMOP.Contains("ExchangeMO"))
                {
                    string strMOType = strPMOP.Replace("ExchangeMO", "");
                    InvokeClient.Instance.Service.DoOperation($"{Module}.ExchangeMO", $"{strMOType}");
                }
                else
                {
                    InvokeClient.Instance.Service.DoOperation($"{Module}.{strPMOP}");
                }
            }
        }

    }
}
