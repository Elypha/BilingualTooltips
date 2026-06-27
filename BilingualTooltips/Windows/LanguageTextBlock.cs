using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

internal static class LanguageTextBlock
{
    public static void Draw(
        string? title,
        string body,
        Vector4 accentColour,
        string? accentTooltip = null,
        Vector4? bodyColour = null)
    {
        Ui.DrawAccentTextBlock(title, body, accentColour, accentTooltip, bodyColour);
    }

    public static Vector4 LanguageColour(BttLanguage language) => language switch
    {
        BttLanguage.Japanese => Ui.HslaToDecimal(235, 0.58, 0.48),
        BttLanguage.English => Ui.HslaToDecimal(0, 0.00, 0.86), // Saint George white
        BttLanguage.German => Ui.HslaToDecimal(48, 1.00, 0.53), // German flag yellow
        BttLanguage.French => Ui.HslaToDecimal(226, 0.97, 0.54), // French blue
        BttLanguage.SimplifiedChinese => Ui.HslaToDecimal(6, 0.86, 0.47), // China red
        BttLanguage.TraditionalChinese => Ui.HslaToDecimal(178, 0.36, 0.67), // sky-blue jade
        BttLanguage.Korean => Ui.HslaToDecimal(347, 0.76, 0.50), // mugunghwa crimson
        _ => Ui.ColourWhite3,
    };
}
