using Aitex.Core.Util;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicUI.Models.PMs
{
    public class PMServoViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        public PMServoViewModel()
        { }

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

        [Subscription("PMServo.ServoEnable")]
        public bool IsServoEnable { get; set; }
        [Subscription("PMServo.ServoReady")]
        public bool IsServoReady { get; set; }

        [Subscription("PMServo.ServoError")]
        public bool IsServoError { get ;  set; }

        [Subscription("PMServo.ActualSpeedFeedback")]
        public float ActualSpeedFeedback { get; set; }
        [Subscription("PMServo.ActualCurrentFeedback")]
        public float ActualCurrentFeedback { get; set; }
        [Subscription("PMServo.AccSpeedFeedback")]
        public float AccSpeedFeedback { get; set; }
        [Subscription("PMServo.DecSpeedFeedback")]
        public float DecSpeedFeedback { get; set; }                

        public void SetServoEnable()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetServoEnable", !IsServoEnable);
        }
        public void SetServoInital()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetServoInital");
        }
        public void SetServoReset()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetServoReset");
        }

        public void SetActualSpeed(object data)
        {
            int speed = 0;
            if (!Int32.TryParse(data.ToString(), out speed))
            {
                return;
            }
            if (speed > 1000)
            {
                speed = 1000;
            }
            if (speed < 0)
            {
                speed = 0;
            }

            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetActualSpeed", speed);
        }

        public void SetAccSpeed(object data)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetAccSpeed", data);
        }

        public void SetDecSpeed(object data)
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.PMServo.SetDecSpeed", data);
        }

    }
}
