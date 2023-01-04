using System.Globalization;
using System.Windows.Controls;

namespace SicUI.Models.Operations.Overviews
{
    internal class CassAWaferAmountValidationRule: ValidationRule
    {
        public int Min { get; set; }

        public int Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (int.TryParse(value.ToString(), out var waferAmount))
            {
                if (waferAmount < Min || waferAmount > Max)
                    return new ValidationResult(false, $"wafer amount in cassette should be between range {Min} ~ {Max}.");

                return new ValidationResult(true, null);
            }

            else
            {
                return new ValidationResult(false, "only number accepted.");
            }
            
        }
    }
}
