using Aitex.Core.Common.DeviceData;
using Aitex.Core.Util;
using Aitex.Core.RT.Event;
using Caliburn.Micro;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SicUI.Models.PMs
{
    public class PMCommMonitorViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {

        public PMCommMonitorViewModel()
        {
            ColorRun1 = DefaultBackground;
            ColorRun2 = DefaultBackground;
            ColorRun3 = DefaultBackground;
            ColorRun4 = DefaultBackground;
            ColorStop1 = DefaultBackground;
            ColorStop2 = DefaultBackground;
            ColorStop3 = DefaultBackground;
            ColorStop4 = DefaultBackground;

            ColorRun5 = DefaultBackground;
            ColorRun6 = DefaultBackground;
            ColorRun7 = DefaultBackground;
            ColorRun8 = DefaultBackground;
            ColorStop5 = DefaultBackground;
            ColorStop6 = DefaultBackground;
            ColorStop7 = DefaultBackground;
            ColorStop8 = DefaultBackground;

            ColorRun9 = DefaultBackground;
            ColorRun10 = DefaultBackground;
            ColorRun11 = DefaultBackground;
            ColorRun12 = DefaultBackground;
            ColorStop9 = DefaultBackground;
            ColorStop10 = DefaultBackground;
            ColorStop11 = DefaultBackground;
            ColorStop12 = DefaultBackground;
        }
        #region properties

        public Visibility TipsVisble => IsConfinementRingUp && !IsOnline ? Visibility.Hidden : Visibility.Visible;

        public bool IsPermission { get => this.Permission == 3; }
        public bool IsActionEnable => IsConfinementRingUp && !IsOnline;
        public bool IsActionEnable1 => false;

        [Subscription("ConfinementRing.RingUpFaceback")]
        public bool IsConfinementRingUp { get; set; }

        [Subscription("IsOnline")]
        public bool IsOnline { get; set; }


        [Subscription("IsBusy")]
        public bool IsBusy { get; set; }

        [Subscription("Status")]
        public string Status { get; set; }

        #region TC1
        [Subscription("TC1.L1WorkingOPFeedBack")]
        public float L1WorkingOP { get; set; }

        [Subscription("TC1.L2WorkingOPFeedBack")]
        public float L2WorkingOP { get; set; }

        [Subscription("TC1.L3WorkingOPFeedBack")]
        public float L3WorkingOP { get; set; }

        [Subscription("TC1.L1PVFeedBack")]
        public float L1PV { get; set; }

        [Subscription("TC1.L2PVFeedBack")]
        public float L2PV { get; set; }

        [Subscription("TC1.L3PVFeedBack")]
        public float L3PV { get; set; }

        [Subscription("TC1.L1TempHighAlarmFeedBack")]
        public bool L1TempHighAlarm { get; set; }

        [Subscription("TC1.L2TempHighAlarmFeedBack")]
        public bool L2TempHighAlarm { get; set; }

        [Subscription("TC1.L3TempHighAlarmFeedBack")]
        public bool L3TempHighAlarm { get; set; }

        [Subscription("TC1.L1TempLowAlarmFeedBack")]
        public bool L1TempLowAlarm { get; set; }

        [Subscription("TC1.L2TempLowAlarmFeedBack")]
        public bool L2TempLowAlarm { get; set; }

        [Subscription("TC1.L3TempLowAlarmFeedBack")]
        public bool L3TempLowAlarm { get; set; }

        public string strL1LoopMode
        {
            get { return L1LoopMode == 0 ? "Auto" : "Manual"; }
        }

        [Subscription("TC1.L1LoopModeSetPoint")]
        public float L1LoopMode { get; set; }

        public string strL2LoopMode
        {
            get { return L2LoopMode == 0 ? "Auto" : "Manual"; }
        }

        [Subscription("TC1.L2LoopModeSetPoint")]
        public float L2LoopMode { get; set; }

        public string strL3LoopMode
        {
            get { return L3LoopMode == 0 ? "Auto" : "Manual"; }
        }

        [Subscription("TC1.L3LoopModeSetPoint")]
        public float L3LoopMode { get; set; }

        [Subscription("TC1.L1TargetSPSetPoint")]
        public float L1TargetSP { get; set; }

        [Subscription("TC1.L2TargetSPSetPoint")]
        public float L2TargetSP { get; set; }

        [Subscription("TC1.L3TargetSPSetPoint")]
        public float L3TargetSP { get; set; }

        [Subscription("TC1.L1TargetOPSetPoint")]
        public float L1TargetOP { get; set; }

        [Subscription("TC1.L2TargetOPSetPoint")]
        public float L2TargetOP { get; set; }

        [Subscription("TC1.L3TargetOPSetPoint")]
        public float L3TargetOP { get; set; }

        [Subscription("TC1.L1PropBandSetPoint")]
        public float L1PropBand { get; set; }

        [Subscription("TC1.L2PropBandSetPoint")]
        public float L2PropBand { get; set; }

        [Subscription("TC1.L3PropBandSetPoint")]
        public float L3PropBand { get; set; }

        [Subscription("TC1.L1IntegralSetPoint")]
        public float L1Integral { get; set; }

        [Subscription("TC1.L2IntegralSetPoint")]
        public float L2Integral { get; set; }

        [Subscription("TC1.L3IntegralSetPoint")]
        public float L3Integral { get; set; }

        [Subscription("TC1.L1DerivativeSetPoint")]
        public float L1Derivative { get; set; }

        [Subscription("TC1.L2DerivativeSetPoint")]
        public float L2Derivative { get; set; }

        [Subscription("TC1.L3DerivativeSetPoint")]
        public float L3Derivative { get; set; }

        [Subscription("TC1.L1InputTempSetPoint")]
        public float L1InputTemp { get; set; }

        [Subscription("TC1.L2InputTempSetPoint")]
        public float L2InputTemp { get; set; }

        [Subscription("TC1.L3InputTempSetPoint")]
        public float L3InputTemp { get; set; }

        public string strTCPyroMode
        {
            get { return TCPyroMode == 0 ? "TC" : "Pyro"; }
        }
        [Subscription("TC1.TCPyroModeSetPoint")]
        public float TCPyroMode { get; set; }

        [Subscription("TC1.L1TempHighLimitSetPoint")]
        public float L1TempHighLimit { get; set; }

        [Subscription("TC1.L2TempHighLimitSetPoint")]
        public float L2TempHighLimit { get; set; }

        [Subscription("TC1.L3TempHighLimitSetPoint")]
        public float L3TempHighLimit { get; set; }

        [Subscription("TC1.L1TempLowLimitSetPoint")]
        public float L1TempLowLimit { get; set; }

        [Subscription("TC1.L2TempLowLimitSetPoint")]
        public float L2TempLowLimit { get; set; }

        [Subscription("TC1.L3TempLowLimitSetPoint")]
        public float L3TempLowLimit { get; set; }

        private List<string> _HeaterModeGroup = new List<string>() { "Power", "TC", "Pyro" };
        public List<string> HeaterModeGroup
        {
            get { return _HeaterModeGroup; }
            set { _HeaterModeGroup = value; NotifyOfPropertyChange("HeaterModeGroup"); }
        }

        public string heaterMode
        {
            get
            {
                switch (HeaterMode)
                {
                    case 0: return "Power";
                    case 1: return "TC";
                    case 2: return "Pyro";
                }
                return "Power";

            }
        }
        private string _SelectedHeaterMode;
        public string SelectedHeaterMode
        {
            get { return _SelectedHeaterMode; }
            set { _SelectedHeaterMode = value; NotifyOfPropertyChange("SelectedHeaterMode"); }
        }
        [Subscription("TC1.HeaterModeSetPoint")]
        public float HeaterMode { get; set; }

        [Subscription("TC1.PowerRefSetPoint")]
        public float PowerRef { get; set; }

        [Subscription("TC1.L1RatioSetPoint")]
        public float L1Ratio { get; set; }

        [Subscription("TC1.L2RatioSetPoint")]
        public float L2Ratio { get; set; }

        [Subscription("TC1.L3RatioSetPoint")]
        public float L3Ratio { get; set; }



        [Subscription("TC1.L1RatedSetPoint")]
        public float L1Rated { get; set; }

        [Subscription("TC1.L2RatedSetPoint")]
        public float L2Rated { get; set; }

        [Subscription("TC1.L3RatedSetPoint")]
        public float L3Rated { get; set; }
        [Subscription("TC1.L1RecipeValueSetPoint")]
        public float L1RecipeValue { get; set; }
        [Subscription("TC1.L2RecipeValueSetPoint")]
        public float L2RecipeValue { get; set; }
        [Subscription("TC1.L3RecipeValueSetPoint")]
        public float L3RecipeValue { get; set; }

        [Subscription("TC1.L1VoltageLimited")]
        public float L1VoltageLimited { get; set; }

        [Subscription("TC1.L2VoltageLimited")]
        public float L2VoltageLimited { get; set; }

        [Subscription("TC1.L3VoltageLimited")]
        public float L3VoltageLimited { get; set; }
        [Subscription("TC1.TempCtrlTCIN")]
        public float TempCtrlTC1IN { get; set; }


        private string _psuPowerRef;
        public string PSUPowerRef
        {
            get
            {
                return _psuPowerRef;
            }
            set
            {
                _psuPowerRef = value;
            }
        }

        #endregion



        #region TC2
        [Subscription("TC2.L1WorkingOPFeedBack")]
        public float L1WorkingOP2 { get; set; }

        [Subscription("TC2.L2WorkingOPFeedBack")]
        public float L2WorkingOP2 { get; set; }

        [Subscription("TC2.L3WorkingOPFeedBack")]
        public float L3WorkingOP2 { get; set; }

        [Subscription("TC2.L1PVFeedBack")]
        public float L1PV2 { get; set; }

        [Subscription("TC2.L2PVFeedBack")]
        public float L2PV2 { get; set; }

        [Subscription("TC2.L3PVFeedBack")]
        public float L3PV2 { get; set; }

        [Subscription("TC2.L1TempHighAlarmFeedBack")]
        public bool L1TempHighAlarm2 { get; set; }

        [Subscription("TC2.L2TempHighAlarmFeedBack")]
        public bool L2TempHighAlarm2 { get; set; }

        [Subscription("TC2.L3TempHighAlarmFeedBack")]
        public bool L3TempHighAlarm2 { get; set; }

        [Subscription("TC2.L1TempLowAlarmFeedBack")]
        public bool L1TempLowAlarm2 { get; set; }

        [Subscription("TC2.L2TempLowAlarmFeedBack")]
        public bool L2TempLowAlarm2 { get; set; }

        [Subscription("TC2.L3TempLowAlarmFeedBack")]
        public bool L3TempLowAlarm2 { get; set; }

        public string strL1LoopMode2
        {
            get { return L1LoopMode2 == 0 ? "Auto" : "Manual"; }
        }

        [Subscription("TC2.L1LoopModeSetPoint")]
        public float L1LoopMode2 { get; set; }

        public string strL2LoopMode2
        {
            get { return L2LoopMode2 == 0 ? "Auto" : "Manual"; }
        }

        [Subscription("TC2.L2LoopModeSetPoint")]
        public float L2LoopMode2 { get; set; }

        public string strL3LoopMode2
        {
            get { return L3LoopMode2 == 0 ? "Auto" : "Manual"; }
        }

        [Subscription("TC2.L3LoopModeSetPoint")]
        public float L3LoopMode2 { get; set; }

        [Subscription("TC2.L1TargetSPSetPoint")]
        public float L1TargetSP2 { get; set; }

        [Subscription("TC2.L2TargetSPSetPoint")]
        public float L2TargetSP2 { get; set; }

        [Subscription("TC2.L3TargetSPSetPoint")]
        public float L3TargetSP2 { get; set; }

        [Subscription("TC2.L1TargetOPSetPoint")]
        public float L1TargetOP2 { get; set; }

        [Subscription("TC2.L2TargetOPSetPoint")]
        public float L2TargetOP2 { get; set; }

        [Subscription("TC2.L3TargetOPSetPoint")]
        public float L3TargetOP2 { get; set; }

        [Subscription("TC2.L1PropBandSetPoint")]
        public float L1PropBand2 { get; set; }

        [Subscription("TC2.L2PropBandSetPoint")]
        public float L2PropBand2 { get; set; }

        [Subscription("TC2.L3PropBandSetPoint")]
        public float L3PropBand2 { get; set; }


        [Subscription("TC2.L1InputTempSetPoint")]
        public float L1InputTemp2 { get; set; }

        [Subscription("TC2.L2InputTempSetPoint")]
        public float L2InputTemp2 { get; set; }

        [Subscription("TC2.L3InputTempSetPoint")]
        public float L3InputTemp2 { get; set; }

        public string strTCPyroMode2
        {
            get { return TCPyroMode2 == 0 ? "TC" : "Pyro"; }
        }
        [Subscription("TC2.TCPyroModeSetPoint")]
        public float TCPyroMode2 { get; set; }

        [Subscription("TC2.L1TempHighLimitSetPoint")]
        public float L1TempHighLimit2 { get; set; }

        [Subscription("TC2.L2TempHighLimitSetPoint")]
        public float L2TempHighLimit2 { get; set; }

        [Subscription("TC2.L3TempHighLimitSetPoint")]
        public float L3TempHighLimit2 { get; set; }

        [Subscription("TC2.L1TempLowLimitSetPoint")]
        public float L1TempLowLimit2 { get; set; }

        [Subscription("TC2.L2TempLowLimitSetPoint")]
        public float L2TempLowLimit2 { get; set; }

        [Subscription("TC2.L3TempLowLimitSetPoint")]
        public float L3TempLowLimit2 { get; set; }

        private List<string> _HeaterModeGroup2 = new List<string>() { "Power", "TC", "Pyro" };
        public List<string> HeaterModeGroup2
        {
            get { return _HeaterModeGroup; }
            set { _HeaterModeGroup = value; NotifyOfPropertyChange("HeaterModeGroup"); }
        }

        public string heaterMode2
        {
            get
            {
                switch (HeaterMode2)
                {
                    case 0: return "Power";
                    case 1: return "TC";
                    case 2: return "Pyro";
                }
                return "Power";

            }
        }
        private string _SelectedHeaterMode2;
        public string SelectedHeaterMode2
        {
            get { return _SelectedHeaterMode2; }
            set { _SelectedHeaterMode2 = value; NotifyOfPropertyChange("SelectedHeaterMode2"); }
        }
        [Subscription("TC2.HeaterModeSetPoint")]
        public float HeaterMode2 { get; set; }

        [Subscription("TC2.PowerRefSetPoint")]
        public float PowerRef2 { get; set; }

        [Subscription("TC2.L1RatioSetPoint")]
        public float L1Ratio2 { get; set; }

        [Subscription("TC2.L2RatioSetPoint")]
        public float L2Ratio2 { get; set; }

        [Subscription("TC2.L3RatioSetPoint")]
        public float L3Ratio2 { get; set; }

        [Subscription("TC2.L1RatedSetPoint")]
        public float L1Rated2 { get; set; }

        [Subscription("TC2.L2RatedSetPoint")]
        public float L2Rated2 { get; set; }

        [Subscription("TC2.L3RatedSetPoint")]
        public float L3Rated2 { get; set; }

        [Subscription("TC2.L1RecipeValueSetPoint")]
        public float L1RecipeValue2 { get; set; }
        [Subscription("TC2.L2RecipeValueSetPoint")]
        public float L2RecipeValue2 { get; set; }
        [Subscription("TC2.L3RecipeValueSetPoint")]
        public float L3RecipeValue2 { get; set; }

        [Subscription("TC2.L1VoltageLimited")]
        public float L1VoltageLimited2 { get; set; }

        [Subscription("TC2.L2VoltageLimited")]
        public float L2VoltageLimited2 { get; set; }

        [Subscription("TC2.L3VoltageLimited")]
        public float L3VoltageLimited2 { get; set; }

        [Subscription("TC2.TempCtrlTCIN")]
        public float TempCtrlTC2IN { get; set; }
        #endregion






        #endregion

        [Subscription("TempOmron.ActualTemp")]
        public float[] ActualTemp { get; set; }

        [Subscription("TempOmron.SettingTemp")]
        public float[] SettingTemp { get; set; }


        public List<string> channel = new List<string>() { "ALL", "Channel1", "Channel2", "Channel3" };
        public List<string> Channels
        {
            get { return channel; }
            set { channel = value; NotifyOfPropertyChange("Channels"); }
        }

        private string _selectedChannelRun;
        public string SelectedChannelRun
        {
            get { return _selectedChannelRun; }
            set
            {
                _selectedChannelRun = value;
                NotifyOfPropertyChange("SelectedChannelRun");
            }
        }

        private string _selectedChannelStop;
        public string SelectedChannelStop
        {
            get { return _selectedChannelStop; }
            set
            {
                _selectedChannelStop = value;
                NotifyOfPropertyChange("SelectedChannelStop");
            }
        }

        public void SetRun(string name, object data)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.TempOmron.WriteSingelData", name, data);
            SetColor(name);
        }
        public void SetStop(string name, object data)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.TempOmron.WriteSingelData", name, data);
            SetColor(name);
        }


        public void SetTemp(string name, object data)
        {
            double dTemp = 0;
            if (!Double.TryParse(data.ToString(), out dTemp))
            {
                return;
            }
            if (dTemp < 0)
            {
                dTemp = 0;
            }
            if (dTemp > 90)
            {
                dTemp = 90;
            }

            InvokeClient.Instance.Service.DoOperation($"{SystemName}.TempOmron.WriteConfigData", name, dTemp);
        }
        public void SetTargetSP(string TCname, string LpName, object data1)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.Set{LpName}TargetSP", new object[] { data1 });
        }

        public void SetInputTemp(string TCname, object data1, object data2, object data3)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetInputTemp", new object[] { data1, data2, data3 });
        }

        public void SetTempHighLimit(string TCname, object data1, object data2, object data3)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetTempHighLimit", new object[] { data1, data2, data3 });
        }

        public void SetTempLowLimit(string TCname, object data1, object data2, object data3)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetTempLowLimit", new object[] { data1, data2, data3 });
        }

        public void SetHeaterMode(string TCname, object data)
        {
            float ControlMode = 0;
            switch (SelectedHeaterMode)
            {
                case "Power": ControlMode = 0; break;
                case "TC": ControlMode = 1; break;
                case "Pyro": ControlMode = 2; break;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetHeaterMode", ControlMode);
        }

        public void SetHeaterMode2(string TCname, object data)
        {
            float ControlMode = 0;
            switch (SelectedHeaterMode2)
            {
                case "Power": ControlMode = 0; break;
                case "TC": ControlMode = 1; break;
                case "Pyro": ControlMode = 2; break;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetHeaterMode2", ControlMode);
        }


        public void SetPowerRef(string TCname, object data)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetPowerRef", data);
        }

        public void SetRatio(string TCname, object data1, object data2, object data3)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetRatio", new object[] { data1, data2, data3 });
        }

        public void SetPowerRef1(string TCname, object data)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetPowerRef1", data);
        }

        public void SetRatedValue(string TCname, object data1, object data2, object data3)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetRatedValue", new object[] { data1, data2, data3 });
        }

        public void SetRecipeValue(string TCname, object data1, object data2, object data3)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{TCname}.SetRecipeValue", new object[] { data1, data2, data3 });
        }
        /// <summary>
        /// 测试用虚拟设备
        /// 可以通过IoTestDevice类添加各种测试功能
        /// </summary>
        public void AlarmTest()
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.TestDevice.TestAlarm");
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            SelectedHeaterMode = HeaterModeGroup[0];
            SelectedHeaterMode2 = HeaterModeGroup2[0];

            //SelectedInnerControlMode = "Power";            


        }



        protected override void Poll()
        {
            base.Poll();

        }

        private void SetColor(string operStr)
        {
            if (operStr == "Channel1RunALL")
            {
                ColorRun1 = HighLight;
                ColorRun2 = HighLight;
                ColorRun3 = HighLight;
                ColorRun4 = HighLight;
                ColorStop1 = DefaultBackground;
                ColorStop2 = DefaultBackground;
                ColorStop3 = DefaultBackground;
                ColorStop4 = DefaultBackground;
            }
            else if (operStr == "Channel1StopALL")
            {
                ColorRun1 = DefaultBackground;
                ColorRun2 = DefaultBackground;
                ColorRun3 = DefaultBackground;
                ColorRun4 = DefaultBackground;
                ColorStop1 = HighLight;
                ColorStop2 = HighLight;
                ColorStop3 = HighLight;
                ColorStop4 = HighLight;
            }
            else if (operStr == "Channel1Run1")
            {
                ColorRun1 = HighLight;
                ColorStop1 = DefaultBackground;
            }
            else if (operStr == "Channel1Stop1")
            {
                ColorRun1 = DefaultBackground;
                ColorStop1 = HighLight;
            }
            else if (operStr == "Channel1Run2")
            {
                ColorRun2 = HighLight;
                ColorStop2 = DefaultBackground;
            }
            else if (operStr == "Channel1Stop2")
            {
                ColorRun2 = DefaultBackground;
                ColorStop2 = HighLight;
            }
            else if (operStr == "Channel1Run3")
            {
                ColorRun3 = HighLight;
                ColorStop3 = DefaultBackground;
            }
            else if (operStr == "Channel1Stop3")
            {
                ColorRun3 = DefaultBackground;
                ColorStop3 = HighLight;
            }
            else if (operStr == "Channel1Run4")
            {
                ColorRun4 = HighLight;
                ColorStop4 = DefaultBackground;
            }
            else if (operStr == "Channel1Stop4")
            {
                ColorRun4 = DefaultBackground;
                ColorStop4 = HighLight;
            }


            if (operStr == "Channel2RunALL")
            {
                ColorRun5 = HighLight;
                ColorRun6 = HighLight;
                ColorRun7 = HighLight;
                ColorRun8 = HighLight;
                ColorStop5 = DefaultBackground;
                ColorStop6 = DefaultBackground;
                ColorStop7 = DefaultBackground;
                ColorStop8 = DefaultBackground;
            }
            else if (operStr == "Channel2StopALL")
            {
                ColorRun5 = DefaultBackground;
                ColorRun6 = DefaultBackground;
                ColorRun7 = DefaultBackground;
                ColorRun8 = DefaultBackground;
                ColorStop7 = HighLight;
                ColorStop5 = HighLight;
                ColorStop6 = HighLight;
                ColorStop8 = HighLight;
            }
            else if (operStr == "Channel2Run1")
            {
                ColorRun5 = HighLight;
                ColorStop5 = DefaultBackground;
            }
            else if (operStr == "Channel2Stop1")
            {
                ColorRun5 = DefaultBackground;
                ColorStop5 = HighLight;
            }
            else if (operStr == "Channel2Run2")
            {
                ColorRun6 = HighLight;
                ColorStop6 = DefaultBackground;
            }
            else if (operStr == "Channel2Stop2")
            {
                ColorRun6 = DefaultBackground;
                ColorStop6 = HighLight;
            }
            else if (operStr == "Channel2Run3")
            {
                ColorRun7 = HighLight;
                ColorStop7 = DefaultBackground;
            }
            else if (operStr == "Channel2Stop3")
            {
                ColorRun7 = DefaultBackground;
                ColorStop7 = HighLight;
            }
            else if (operStr == "Channel2Run4")
            {
                ColorRun8 = HighLight;
                ColorStop8 = DefaultBackground;
            }
            else if (operStr == "Channel2Stop4")
            {
                ColorRun8 = DefaultBackground;
                ColorStop8 = HighLight;
            }

            if (operStr == "Channel3RunALL")
            {
                ColorRun9 = HighLight;
                ColorRun10 = HighLight;
                ColorRun11 = HighLight;
                ColorRun12 = HighLight;
                ColorStop9 = DefaultBackground;
                ColorStop10 = DefaultBackground;
                ColorStop11 = DefaultBackground;
                ColorStop12 = DefaultBackground;
            }
            else if (operStr == "Channel3StopALL")
            {
                ColorRun9 = DefaultBackground;
                ColorRun10 = DefaultBackground;
                ColorRun11 = DefaultBackground;
                ColorRun12 = DefaultBackground;
                ColorStop9 = HighLight;
                ColorStop10 = HighLight;
                ColorStop11 = HighLight;
                ColorStop12 = HighLight;
            }
            else if (operStr == "Channel3Run1")
            {
                ColorRun9 = HighLight;
                ColorStop9 = DefaultBackground;
            }
            else if (operStr == "Channel3Stop1")
            {
                ColorRun9 = DefaultBackground;
                ColorStop9 = HighLight;
            }
            else if (operStr == "Channel3Run2")
            {
                ColorRun10 = HighLight;
                ColorStop10 = DefaultBackground;
            }
            else if (operStr == "Channel3Stop2")
            {
                ColorRun10 = DefaultBackground;
                ColorStop10 = HighLight;
            }
            else if (operStr == "Channel3Run3")
            {
                ColorRun11 = HighLight;
                ColorStop11 = DefaultBackground;
            }
            else if (operStr == "Channel3Stop3")
            {
                ColorRun11 = DefaultBackground;
                ColorStop11 = HighLight;
            }
            else if (operStr == "Channel3Run4")
            {
                ColorRun12 = HighLight;
                ColorStop12 = DefaultBackground;
            }
            else if (operStr == "Channel3Stop4")
            {
                ColorRun12 = DefaultBackground;
                ColorStop12 = HighLight;
            }
        }

        public SolidColorBrush HighLight => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 135, 206, 250)); //高亮
        public SolidColorBrush DefaultBackground => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 99, 152, 204)); //默认背景色


        public SolidColorBrush ColorRun1 { get; set; } 
        public SolidColorBrush ColorStop1 { get; set; }
        public SolidColorBrush ColorRun2 { get; set; }
        public SolidColorBrush ColorStop2 { get; set; }
        public SolidColorBrush ColorRun3 { get; set; }
        public SolidColorBrush ColorStop3 { get; set; }
        public SolidColorBrush ColorRun4 { get; set; }
        public SolidColorBrush ColorStop4 { get; set; }
        public SolidColorBrush ColorRun5 { get; set; }
        public SolidColorBrush ColorStop5 { get; set; }
        public SolidColorBrush ColorRun6 { get; set; }
        public SolidColorBrush ColorStop6 { get; set; }
        public SolidColorBrush ColorRun7 { get; set; }
        public SolidColorBrush ColorStop7 { get; set; }
        public SolidColorBrush ColorRun8 { get; set; }
        public SolidColorBrush ColorStop8 { get; set; }
        public SolidColorBrush ColorRun9 { get; set; }
        public SolidColorBrush ColorStop9 { get; set; }
        public SolidColorBrush ColorRun10 { get; set; }
        public SolidColorBrush ColorStop10 { get; set; }
        public SolidColorBrush ColorRun11 { get; set; }
        public SolidColorBrush ColorStop11 { get; set; }
        public SolidColorBrush ColorRun12 { get; set; }
        public SolidColorBrush ColorStop12 { get; set; }

        #region HeatAll

        public string HeatEnableStr => AllHeatEnable ? "Disable" : "Enable";

        public string PSU1EnableStr => PSU1Running ? "Disable" : "Enable";
        public string PSU2EnableStr => PSU2Running ? "Disable" : "Enable";
        public string PSU3EnableStr => PSU3Running ? "Disable" : "Enable";
        public string SCR1EnableStr => SCR1Running ? "Disable" : "Enable";
        public string SCR2EnableStr => SCR2Running ? "Disable" : "Enable";
        public string SCR3EnableStr => SCR3Running ? "Disable" : "Enable";

        [Subscription("PSU1.AllHeatEnable")]
        public bool AllHeatEnable { get; set; }

        public void SetAllHeatEnable(string psuName)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PSU1.SetAllHeatEnable", !AllHeatEnable);
        }
        

        #region PSU

        [Subscription("PSU1.StatusFeedBack")]
        public bool PSU1Running { get; set; }
        [Subscription("PSU1.AlarmFeedBack")]
        public bool PSU1Alarm { get; set; }

        [Subscription("PSU1.OutputVoltageFeedBack")]
        public float PSU1OutputVoltageFeedBack { get; set; }
        [Subscription("PSU1.OutputArmsFeedBack")]
        public float PSU1OutputArmsFeedBack { get; set; }
        [Subscription("PSU1.OutputPowerFeedBack")]
        public float PSU1OutputPowerFeedBack { get; set; }
        [Subscription("PSU1.SimVoltageFeedBack")]
        public float PSU1SimVoltageFeedBack { get; set; }
        [Subscription("PSU1.SimArmsFeedBack")]
        public float PSU1SimArmsFeedBack { get; set; }

        [Subscription("PSU2.StatusFeedBack")]
        public bool PSU2Running { get; set; }
        [Subscription("PSU2.AlarmFeedBack")]
        public bool PSU2Alarm { get; set; }

        [Subscription("PSU2.OutputVoltageFeedBack")]
        public float PSU2OutputVoltageFeedBack { get; set; }
        [Subscription("PSU2.OutputArmsFeedBack")]
        public float PSU2OutputArmsFeedBack { get; set; }
        [Subscription("PSU2.OutputPowerFeedBack")]
        public float PSU2OutputPowerFeedBack { get; set; }
        [Subscription("PSU2.SimVoltageFeedBack")]
        public float PSU2SimVoltageFeedBack { get; set; }
        [Subscription("PSU2.SimArmsFeedBack")]
        public float PSU2SimArmsFeedBack { get; set; }


        [Subscription("PSU3.StatusFeedBack")]
        public bool PSU3Running { get; set; }
        [Subscription("PSU3.AlarmFeedBack")]
        public bool PSU3Alarm { get; set; }

        [Subscription("PSU3.OutputVoltageFeedBack")]
        public float PSU3OutputVoltageFeedBack { get; set; }
        [Subscription("PSU3.OutputArmsFeedBack")]
        public float PSU3OutputArmsFeedBack { get; set; }
        [Subscription("PSU3.OutputPowerFeedBack")]
        public float PSU3OutputPowerFeedBack { get; set; }
        [Subscription("PSU3.SimVoltageFeedBack")]
        public float PSU3SimVoltageFeedBack { get; set; }
        [Subscription("PSU3.SimArmsFeedBack")]
        public float PSU3SimArmsFeedBack { get; set; }

        public float Resistance1
        {
            get
            {

                return PSU1OutputArmsFeedBack == 0 ? 0 : PSU1OutputVoltageFeedBack / PSU1OutputArmsFeedBack;
            }
        }
        public float Resistance2
        {
            get
            {

                return PSU2OutputArmsFeedBack == 0 ? 0 : PSU2OutputVoltageFeedBack / PSU2OutputArmsFeedBack;
            }
        }
        public float Resistance3
        {
            get
            {

                return PSU3OutputArmsFeedBack == 0 ? 0 : PSU3OutputVoltageFeedBack / PSU3OutputArmsFeedBack;
            }
        }

        public void SetPSUEnable(string psuName)
        {
            if (!AllHeatEnable)
            {
                MessageBox.Show("Set HeatEnable first!");
                return;
            }

            bool setValue = PSU1Running;
            if (psuName == "PSU1")
            {
                setValue = PSU1Running;
            }
            else if (psuName == "PSU2")
            {
                setValue = PSU2Running;
            }
            else if (psuName == "PSU3")
            {
                setValue = PSU3Running;
            }

            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{psuName}.SetPSUEnable", !setValue);
        }
        public void SetPSUReset(string psuName)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{psuName}.SetPSUReset", true);
        }

        #endregion

        #region SCR
        [Subscription("SCR1.StatusFeedBack")]
        public bool SCR1Running { get; set; }
        [Subscription("SCR1.VoltageFeedBack")]
        public float SCR1VoltageFeedBack { get; set; }
        [Subscription("SCR1.ArmsFeedBack")]
        public float SCR1ArmsFeedBack { get; set; }
        [Subscription("SCR1.PowerFeedBack")]
        public float SCR1PowerFeedBack { get; set; }

        [Subscription("SCR2.StatusFeedBack")]
        public bool SCR2Running { get; set; }
        [Subscription("SCR2.VoltageFeedBack")]
        public float SCR2VoltageFeedBack { get; set; }
        [Subscription("SCR2.ArmsFeedBack")]
        public float SCR2ArmsFeedBack { get; set; }
        [Subscription("SCR2.PowerFeedBack")]
        public float SCR2PowerFeedBack { get; set; }

        [Subscription("SCR3.StatusFeedBack")]
        public bool SCR3Running { get; set; }
        [Subscription("SCR3.VoltageFeedBack")]
        public float SCR3VoltageFeedBack { get; set; }
        [Subscription("SCR3.ArmsFeedBack")]
        public float SCR3ArmsFeedBack { get; set; }
        [Subscription("SCR3.PowerFeedBack")]
        public float SCR3PowerFeedBack { get; set; }

        public float SCRResistance1
        {
            get
            {

                return SCR1ArmsFeedBack == 0 ? 0 : SCR1VoltageFeedBack / SCR1ArmsFeedBack;
            }
        }
        public float SCRResistance2
        {
            get
            {
                return SCR2ArmsFeedBack == 0 ? 0 : SCR2VoltageFeedBack / SCR2ArmsFeedBack;
            }
        }
        public float SCRResistance3
        {
            get
            {
                return SCR3ArmsFeedBack == 0 ? 0 : SCR3VoltageFeedBack / SCR3ArmsFeedBack;
            }
        }

        public void SetSCREnable(string psuName)
        {
            if (!AllHeatEnable)
            {
                MessageBox.Show("Set HeatEnable first!");
                return;
            }

            bool setValue = SCR1Running;
            if (psuName == "SCR1")
            {
                setValue = SCR1Running;
            }
            else if (psuName == "SCR2")
            {
                setValue = SCR2Running;
            }
            else if (psuName == "SCR3")
            {
                setValue = SCR3Running;
            }
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{psuName}.SetEnable", !setValue);
        }

        public void SetSCRReset(string psuName)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{psuName}.SetReset", true);
        }

        #endregion

        //qbh 20220208
        #region PLC
        [Subscription("PLC_IsConnected")]
        public bool PLCIsConnected { get; set; }
        #endregion
        //qbh_
        #endregion

        //qbh 20220221
        #region MFC

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


        //[Subscription("Mfc30.DeviceData")]
        //public AITMfcData Mfc30Data { get; set; }

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
        //qbh_

      


        //#region TC1界面值
        //private string _tc1Ratio1;
        //public string TC1Ratio1
        //{
        //    get { return _tc1Ratio1; }
        //    set
        //    {
        //        _tc1Ratio1 = value;
        //        //NotifyOfPropertyChange("TC1Ratio1");
        //    }
        //}
        //private string _tc1Ratio3;
        //public string TC1Ratio3
        //{
        //    get { return _tc1Ratio3; }
        //    set
        //    {
        //        _tc1Ratio3 = value;
        //        //NotifyOfPropertyChange("TC1Ratio3");
        //    }
        //}
        //private string _tc1PowerRef;
        //public string TC1PowerRef
        //{
        //    get { return _tc1PowerRef; }
        //    set
        //    {
        //        _tc1PowerRef = value;
        //        //NotifyOfPropertyChange("TC1PowerRef");
        //    }
        //}
        //private string _tc1L1Targert;
        //public string Tc1L1Targert
        //{
        //    get { return _tc1L1Targert; }
        //    set
        //    {
        //        _tc1L1Targert = value;
        //        //NotifyOfPropertyChange("Tc1L1Targert");
        //    }
        //}
        //private string _tc1L2Targert;
        //public string Tc1L2Targert
        //{
        //    get { return _tc1L2Targert; }
        //    set
        //    {
        //        _tc1L2Targert = value;
        //        //NotifyOfPropertyChange("Tc1L2Targert");
        //    }
        //}
        //private string _tc1L3Targert;
        //public string Tc1L3Targert
        //{
        //    get { return _tc1L3Targert; }
        //    set
        //    {
        //        _tc1L3Targert = value;
        //        //NotifyOfPropertyChange("Tc1L3Targert");
        //    }
        //}
        //#endregion

        //#region TC2界面值
        //private string _tc2Ratio1;
        //public string TC2Ratio1
        //{
        //    get { return _tc2Ratio1; }
        //    set
        //    {
        //        _tc2Ratio1 = value;
        //        NotifyOfPropertyChange("TC2Ratio1");
        //    }
        //}
        //private string _tc2Ratio2;
        //public string TC2Ratio2
        //{
        //    get { return _tc2Ratio2; }
        //    set
        //    {
        //        _tc2Ratio2 = value;
        //        NotifyOfPropertyChange("TC2Ratio2");
        //    }
        //}
        //private string _tc2PowerRef;
        //public string TC2PowerRef
        //{
        //    get { return _tc2PowerRef; }
        //    set
        //    {
        //        _tc2PowerRef = value;
        //        NotifyOfPropertyChange("TC2PowerRef");
        //    }
        //}
        //private string _tc2L1Targert;
        //public string Tc2L1Targert
        //{
        //    get { return _tc2L1Targert; }
        //    set
        //    {
        //        _tc2L1Targert = value;
        //        NotifyOfPropertyChange("Tc2L1Targert");
        //    }
        //}
        //private string _tc2L2Targert;
        //public string Tc2L2Targert
        //{
        //    get { return _tc2L2Targert; }
        //    set
        //    {
        //        _tc2L2Targert = value;
        //        NotifyOfPropertyChange("Tc2L2Targert");
        //    }
        //}
        //private string _tc2L3Targert;
        //public string Tc2L3Targert
        //{
        //    get { return _tc2L3Targert; }
        //    set
        //    {
        //        _tc2L3Targert = value;
        //        NotifyOfPropertyChange("Tc2L3Targert");
        //    }
        //}
        //#endregion
    }
}
