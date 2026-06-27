using ClientLanguage = Dalamud.Game.ClientLanguage;

namespace BilingualTooltips;

public enum BttLanguage
{
    Off,
    Auto,
    Japanese,
    English,
    German,
    French,
    SimplifiedChinese,
    TraditionalChinese,
    Korean,
}

public static class BttLanguageExtensions
{
    public static readonly BttLanguage[] AllLanguages =
    [
        BttLanguage.Japanese,
        BttLanguage.English,
        BttLanguage.German,
        BttLanguage.French,
        BttLanguage.SimplifiedChinese,
        BttLanguage.TraditionalChinese,
        BttLanguage.Korean,
    ];

    public static readonly BttLanguage[] ReleaseLanguages =
    [
        BttLanguage.Japanese,
        BttLanguage.English,
        BttLanguage.German,
        BttLanguage.French,
        BttLanguage.SimplifiedChinese,
    ];

    public static readonly BttLanguage[] PackageLanguages =
    [
        BttLanguage.Japanese,
        BttLanguage.English,
        BttLanguage.German,
        BttLanguage.French,
        BttLanguage.SimplifiedChinese,
        BttLanguage.TraditionalChinese,
        BttLanguage.Korean,
    ];

    public static readonly BttLanguage[] GameClientLanguages =
    [
        BttLanguage.Japanese,
        BttLanguage.English,
        BttLanguage.German,
        BttLanguage.French,
    ];

    public static BttLanguage ResolveInput(this BttLanguage language) =>
        language != BttLanguage.Auto
            ? language
            : FromClientLanguage(Service.ClientState.ClientLanguage);

    public static BttLanguage FromClientLanguage(ClientLanguage language) => language switch
    {
        ClientLanguage.Japanese => BttLanguage.Japanese,
        ClientLanguage.English => BttLanguage.English,
        ClientLanguage.German => BttLanguage.German,
        ClientLanguage.French => BttLanguage.French,
        _ => BttLanguage.Japanese,
    };

    public static bool IsReleaseSupported(this BttLanguage language) => ReleaseLanguages.Contains(language);

    public static bool IsGameClientSupported(this BttLanguage language) => GameClientLanguages.Contains(language);

    public static bool TryGetClientLanguage(this BttLanguage language, out ClientLanguage clientLanguage)
    {
        switch (language)
        {
            case BttLanguage.Japanese:
                clientLanguage = ClientLanguage.Japanese;
                return true;
            case BttLanguage.English:
                clientLanguage = ClientLanguage.English;
                return true;
            case BttLanguage.German:
                clientLanguage = ClientLanguage.German;
                return true;
            case BttLanguage.French:
                clientLanguage = ClientLanguage.French;
                return true;
            default:
                clientLanguage = default;
                return false;
        }
    }

    public static string ToPackageCode(this BttLanguage language) => language switch
    {
        BttLanguage.Japanese => "ja",
        BttLanguage.English => "en",
        BttLanguage.German => "de",
        BttLanguage.French => "fr",
        BttLanguage.SimplifiedChinese => "zh-Hans",
        BttLanguage.TraditionalChinese => "zh-Hant",
        BttLanguage.Korean => "ko",
        _ => "",
    };

    public static BttLanguage FromPackageCode(string code) => code switch
    {
        "ja" => BttLanguage.Japanese,
        "en" => BttLanguage.English,
        "de" => BttLanguage.German,
        "fr" => BttLanguage.French,
        "zh-Hans" => BttLanguage.SimplifiedChinese,
        "zh-Hant" => BttLanguage.TraditionalChinese,
        "ko" => BttLanguage.Korean,
        _ => BttLanguage.Off,
    };

    public static string DisplayName(this BttLanguage language) => language switch
    {
        BttLanguage.Off => "Off",
        BttLanguage.Auto => "Auto",
        BttLanguage.Japanese => "Japanese",
        BttLanguage.English => "English",
        BttLanguage.German => "German",
        BttLanguage.French => "French",
        BttLanguage.SimplifiedChinese => "Chinese (Simplified)",
        BttLanguage.TraditionalChinese => "Chinese (Traditional)",
        BttLanguage.Korean => "Korean",
        _ => language.ToString(),
    };

    public static string DisplayNameWithSupportStatus(this BttLanguage language) =>
        language is BttLanguage.Off or BttLanguage.Auto || language.IsReleaseSupported()
            ? language.DisplayName()
            : $"{language.DisplayName()} (unsupported)";
}
