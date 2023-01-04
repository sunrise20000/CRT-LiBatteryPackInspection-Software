using Aitex.Core.Common.DeviceData;
using Aitex.Core.Util;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicUI.Models.PMs
{
    public class PMLinkedDeviceViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        #region UPS
        // UPS1
        [Subscription("ITAUPSA.SystemStatus")]
        public string systemStatus1 { get; set; }

        [Subscription("ITAUPSA.InputVoltage")]
        public float inputVoltage1 { get; set; }

        [Subscription("ITAUPSA.BatteryVoltage")]
        public float batteryVoltage1 { get; set; }

        [Subscription("ITAUPSA.BatteryRemainsTime")]
        public int batteryRemainsTime1 { get; set; }

        // UPS2
        [Subscription("ITAUPSB.SystemStatus")]
        public string systemStatus2 { get; set; }

        [Subscription("ITAUPSB.InputVoltage")]
        public float inputVoltage2 { get; set; }

        [Subscription("ITAUPSB.BatteryVoltage")]
        public float batteryVoltage2 { get; set; }

        [Subscription("ITAUPSB.BatteryRemainsTime")]
        public int batteryRemainsTime2 { get; set; }

        #endregion

        #region AkOpticsViper
        //[Subscription("AkOpticsViper.InnerOpticsViperItem")]
        //public AITOpticsViperData[] OpticsViperInner { get; set; }

        //public List<AITOpticsViperData> OpticsViperInnerData
        //{
        //    get
        //    {
        //        return OpticsViperInner?.ToList();
        //    }
        //}

        //[Subscription("AkOpticsViper.MiddleOpticsViperItem")]
        //public AITOpticsViperData[] OpticsViperMiddle { get; set; }

        //public List<AITOpticsViperData> OpticsViperMiddleData
        //{
        //    get
        //    {
        //        return OpticsViperMiddle?.ToList();
        //    }
        //}

        //[Subscription("AkOpticsViper.OuterOpticsViperItem")]
        //public AITOpticsViperData[] OpticsViperOuter { get; set; }

        //public List<AITOpticsViperData> OpticsViperOuterData
        //{
        //    get
        //    {
        //        return OpticsViperOuter?.ToList();
        //    }
        //}
        #endregion


        ////[Subscription("PMDRYVacuumPump.RunMode")]
        ////public string RunMode { get; set; }

        ////[Subscription("PMDRYVacuumPump.MPOn")]
        ////public string MPOn { get; set; }

        ////[Subscription("PMDRYVacuumPump.BPOn")]
        ////public string BPOn { get; set; }       

        ////public string Module => SystemName;



        ////[Subscription("AETemp.AETemp1")]
        ////public double Temp1 { get; set; }

        ////[Subscription("AETemp.AETemp2")]
        ////public double Temp2 { get; set; }

        ////[Subscription("AETemp.AETemp3")]
        ////public double Temp3 { get; set; }







        public PMLinkedDeviceViewModel()
        {
            this.DisplayName = "LinkedDevice";
            InitOpticsViperItem();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
          
        }

        private void InitOpticsViperItem()
        {
           // OpticsViperInnerData = new ObservableCollection<OpticsViperItem>();
           // //OpticsViperMiddleData = new ObservableCollection<OpticsViperItem>();
           // OpticsViperOuterData = new ObservableCollection<OpticsViperItem>();

           // OpticsViperInnerData.Add(new OpticsViperItem());
           //// OpticsViperMiddleData.Add(new OpticsViperItem());
           // OpticsViperOuterData.Add(new OpticsViperItem());

           // OpticsViperInnerData.Add(new OpticsViperItem());
           // //OpticsViperMiddleData.Add(new OpticsViperItem());
           // OpticsViperOuterData.Add(new OpticsViperItem());

           // OpticsViperInnerData.Add(new OpticsViperItem());
           // //OpticsViperMiddleData.Add(new OpticsViperItem());
           // OpticsViperOuterData.Add(new OpticsViperItem());
        }
        #region Confinement

        [Subscription("ConfinementRing.RingUpFaceback")]
        public bool IsConfinementRingUpPM1 { get; set; }
        [Subscription("ConfinementRing.RingDownFaceback")]
        public bool IsConfinementRingDownPM1 { get; set; }

        public void ConfinementRingOn()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetServoOn");
        }

        public void ConfinementRingUp()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetBlockUp");
        }

        public void ConfinementRingDown()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetBlockLow");
        }

        public void ConfinementRingHome()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetBlockHome");
        }

        public void SetStbOn()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetStbOn");
        }

        public void SetStbOff()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetStbOff");
        }
        public void ServoAbort()
        {
            InvokeClient.Instance.Service.DoOperation("PM1.NAISServo.SetAbort");
        }
        #endregion
    }
}
