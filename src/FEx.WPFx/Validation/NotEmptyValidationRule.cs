using System.Globalization;
using System.Windows.Controls;

namespace FEx.WPFx.Validation;

public class NotEmptyValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
        string.IsNullOrWhiteSpace((value ?? "").ToString())
            ? new(false, "Field is required.")
            : ValidationResult.ValidResult;
}