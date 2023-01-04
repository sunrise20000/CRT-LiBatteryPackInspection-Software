using Aitex.Core.Util;
using SicUI.Models;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.UI.Client.ClientBase;

namespace SicUI
{
    public class ProcessMonitorViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        public string TargetModule { get; set; }


        [Subscription("SelectedRecipeName")]
        public string RecipeName { get; set; }

        [Subscription("RecipeStepNumber")]
        public int RecipeStepNumber { get; set; }

        [Subscription("RecipeStepName")]
        public string RecipeStepName { get; set; }

        [Subscription("RecipeStepElapseTime")]
        public int RecipeStepElapseTime { get; set; }

        [Subscription("RecipeStepTime")]
        public int RecipeStepTime { get; set; }

        [Subscription("RecipeTotalElapseTime")]
        public int RecipeTotalElapseTime { get; set; }

        [Subscription("RecipeTotalTime")]
        public int RecipeTotalTime { get; set; }

        [Subscription("TC1.HeaterModeSetPoint")]
        public float TC1HeaterMode { get; set; }

        [Subscription("TC2.HeaterModeSetPoint")]
        public float TC2HeaterMode { get; set; }

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

        [Subscription("TC1.L2InputTempSetPoint")]
        public float L2InputTemp { get; set; }

        [Subscription("TC1.L3InputTempSetPoint")]
        public float L3InputTemp { get; set; }

        [Subscription("TC2.L3InputTempSetPoint")]
        public float SCRL3InputTemp { get; set; }

        [Subscription("PMServo.ActualSpeedFeedback")]
        public float ActualSpeedFeedback { get; set; }

        [Subscription("PT1.DeviceData")]
        public AITPressureMeterData ChamPress { get; set; }

        [Subscription("TC1.TempCtrlTCIN")]
        public float PM1Temprature { get; set; }

        [Subscription("Status")]
        public string Status { get; set; }

        public bool IsPMProcess => Status == "Process" || Status == "PostProcess" || Status == "Paused" ||
                                   Status == "PMMacroPause" || Status == "PMMacro" || Status == "PostPMMacro";

        public bool IsPreProcess => Status == "PreProcess" || Status == "PrePMMacro";

        public string StepNumber
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeStepNumber}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";

            }
            set
            {

            }
        }

        public string StepName
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeStepName}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";
            }
        }

        public string StepTime
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeStepElapseTime}/{RecipeStepTime}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";

            }
        }

        public string RecipeTime
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeTotalElapseTime}/{RecipeTotalTime}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";
            }
        }

        public string PsuMode
        {
            get
            {
                switch (TC1HeaterMode)
                {
                    case 0: return "Power";
                    case 1: return "Pyro";
                }
                return "Power";

            }
        }

        public string ScrMode
        {
            get
            {
                switch (TC2HeaterMode)
                {
                    case 0: return "Power";
                    case 1: return "TC";
                    case 2: return "Pyro";
                }
                return "Power";

            }
        }

        public string ChamberPressureFeedback => ChamPress?.FeedBack.ToString(ChamPress?.FormatString);
    }
}
