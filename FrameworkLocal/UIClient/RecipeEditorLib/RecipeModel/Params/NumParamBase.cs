using System.Collections.Generic;

namespace RecipeEditorLib.RecipeModel.Params
{
    public abstract class NumParamBase<TValue> : ParamBaseWithGenericValue<TValue>
    {
        #region Variables

        protected TValue _minimun;
        protected TValue _maximun;

        #endregion

        #region Constructors

        protected NumParamBase()
        {
        }

        protected NumParamBase(TValue initValue) : base(initValue)
        {
        }

        protected NumParamBase(TValue initValue, TValue minimum, TValue maximum) : base(initValue)
        {
            if (minimum.GetType() != typeof(TValue))
                minimum = default;
            
            if (maximum.GetType() != typeof(TValue))
                maximum = default;
            
            _minimun = minimum;
            _maximun = maximum;
        }

        #endregion

        public TValue Minimun
        {
            get => _minimun;
            set
            {
                _minimun = value;
                NotifyOfPropertyChange(nameof(Minimun));
            }
        }

        
        public TValue Maximun
        {
            get => _maximun;
            set
            {
                _maximun = value;
                NotifyOfPropertyChange(nameof(Maximun));
            }
        }


         #region Methods

        public override void Validate()
        {
            var ret = Comparer<TValue>.Default.Compare(_minimun, _maximun);

            if (ret > 0)
            {
                IsValidated = false;
                ValidationError = "Config error: minimum value is greater than maximum value";
                return;
            }

            if (double.TryParse(Value.ToString(), out var dblValue))
            {
                var retV2Min = Comparer<TValue>.Default.Compare(Value, _minimun);
                var retV2Max = Comparer<TValue>.Default.Compare(Value, _maximun);

                if (retV2Min < 0 || retV2Max > 0)
                {
                    IsValidated = false;
                    ValidationError = $"actual value ({_value}) is outside the range of {_minimun} to {_maximun}";
                }
                else
                {
                    IsValidated = true;
                    ValidationError = null;
                }
            }
            else
            {
                IsValidated = false;
                ValidationError = "Format error: not a number";
            }

        }

        #endregion
    }
}
