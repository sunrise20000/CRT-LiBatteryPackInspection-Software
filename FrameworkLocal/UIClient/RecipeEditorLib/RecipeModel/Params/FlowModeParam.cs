using System;
using System.Linq;
using System.Windows;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class FlowModeParam : ComboxParam
    {
        #region Variables

        public enum FlowModeEnum
        {
            Purge,
            Vent,
            Run
        }

        #endregion

        #region Constructors

        public FlowModeParam()
        {
        }

        public FlowModeParam(string initValue) : base(initValue)
        {
        }

        #endregion

        #region Properties

        public override string Value
        {
            get => _value;
            set
            {
                var isSwitchToVent = false;
                if (string.Equals(value, FlowModeEnum.Purge.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (Previous is FlowModeParam pp)
                    {
                        if (string.Equals(pp.Value, FlowModeEnum.Run.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isSwitchToVent = true;
                        }
                    }

                    if (Next is FlowModeParam np)
                    {
                        if (string.Equals(np.Value, FlowModeEnum.Run.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isSwitchToVent = true;
                        }
                    }
                }
                else if (string.Equals(value, FlowModeEnum.Run.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (Previous is FlowModeParam pp)
                    {
                        if (string.Equals(pp.Value, FlowModeEnum.Purge.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isSwitchToVent = true;
                        }
                    }

                    if (Next is FlowModeParam np)
                    {
                        if (string.Equals(np.Value, FlowModeEnum.Purge.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            isSwitchToVent = true;
                        }
                    }
                }

                if (isSwitchToVent)
                    MessageBox.Show("Flow mode cannot be switched directly between 'Purge' and 'Run'.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                _value = isSwitchToVent ? FlowModeEnum.Vent.ToString() : value;
                NotifyOfPropertyChange(nameof(Value));

                Feedback?.Invoke(this);

                _isSaved = _value.Equals(_valueSnapshot);
                NotifyOfPropertyChange(nameof(IsSaved));
            }
        }

        #endregion

        #region Methods

        private void InnerValidate()
        {
            // 判断Flow Mode是否直接在Purge和Run之间切换。
            var sourceKeywords = Enum.GetNames(typeof(FlowModeEnum)).Select(x => x.ToLower());
            
            var currentStepOption = Value.ToLower();
            var flowModeValid = true;

            if (Previous != null)
            {
                var previousStepOption = ((ComboxParam)Previous).Value.ToLower();
                if ((previousStepOption == FlowModeEnum.Purge.ToString().ToLower()
                     && currentStepOption == FlowModeEnum.Run.ToString().ToLower())
                    ||
                    (previousStepOption == FlowModeEnum.Run.ToString().ToLower()
                     && currentStepOption == FlowModeEnum.Purge.ToString().ToLower()))
                {
                    flowModeValid = false;
                }
            }

            if (Next != null)
            {
                var nextStepOption = ((ComboxParam)Next).Value.ToLower();
                if ((nextStepOption == FlowModeEnum.Purge.ToString().ToLower()
                     && currentStepOption == FlowModeEnum.Run.ToString().ToLower())
                    ||
                    (nextStepOption == FlowModeEnum.Run.ToString().ToLower()
                     && currentStepOption == FlowModeEnum.Purge.ToString().ToLower()))
                {
                    flowModeValid = false;
                }
            }

            IsValidated = flowModeValid;
            ValidationError = !flowModeValid
                ? "Flow mode cannot be switched directly between 'Purge' and 'Run'"
                : null;
        }

        public override void Validate()
        {
         
            var linkedList = Flatten();

            foreach (var param in linkedList)
            {
                if(param is FlowModeParam p)
                    p.InnerValidate();
            }
        }

        #endregion
    }
}
