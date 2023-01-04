using System.Text.RegularExpressions;
using Microsoft.DwayneNeed.Numerics;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class Sets3RatioParam : ParamBaseWithGenericValue<string>
    {
        #region Variables


        #endregion

        #region Constructors

        public Sets3RatioParam()
        {
            _value = "1:1:1";
            Ratio = new [] { 1.0, 1.0, 1.0 };
        }

        public Sets3RatioParam(string initValue) : base(initValue)
        {
            Ratio = new[] { 1.0, 1.0, 1.0 };
            ParseRatio(initValue);
        }

        #endregion

        #region Properties

        public override string Value
        {
            get => _value;
            set
            {
                _value = value;
                NotifyOfPropertyChange(nameof(value));

                Feedback?.Invoke(this);

                _isSaved = _value.Equals(_valueSnapshot);
                NotifyOfPropertyChange(nameof(IsSaved));

                Validate();
                ParseRatio(value);
            }
        }


        public double[] Ratio { get; private set; }


        #endregion

        #region Methods

        private void ParseRatio(string value)
        {
            var nums = value.Split(':');
            if (nums.Length == 3
                && double.TryParse(nums[0], out var r1)
                && double.TryParse(nums[1], out var r2)
                && double.TryParse(nums[2], out var r3))
            {
                Ratio[0] = r1;
                Ratio[1] = r2;
                Ratio[2] = r3;
            }
            else
            {
                Ratio[0] = double.NaN;
                Ratio[1] = double.NaN;
                Ratio[2] = double.NaN;
            }
        }

        public override void Validate()
        {
            if (ValidateRatioString(_value))
            {
                IsValidated = true;
                ValidationError = null;

            }
            else
            {
                IsValidated = false;
                ValidationError = "Format error";
            }
        }

        #endregion

        public static bool ValidateRatioString(string value)
        {
            var nums = value.Split(':');
            return nums.Length == 3
                   && double.TryParse(nums[0], out _)
                   && double.TryParse(nums[1], out _)
                   && double.TryParse(nums[2], out _);
        }
    }
}
