using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace FEx.WPFx;

public class WPFUtilities
{
    /// <summary>
    ///     Overrides formatting on UI.
    /// </summary>
    /// <param name="culture">
    ///     The culture to use. If <c>null</c>,
    ///     <see cref="System.Globalization.CultureInfo.CurrentCulture" /> is used.
    /// </param>
    public static void OverrideFormattingOnUI(CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }
}