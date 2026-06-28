using System.IO;
using System.Text.Json.Nodes;
using static BilingualTooltips.Configuration.BilingualTooltipsConfigMigrator;

namespace BilingualTooltips.Configuration;

internal static class BilingualTooltipsConfigMigrationV0ToV1
{
    private const int TargetVersion = 1;

    public static BilingualTooltipsConfig Migrate(JsonObject root)
    {
        var config = new BilingualTooltipsConfig
        {
            Version = TargetVersion,
        };

        CopyLegacyLanguage(root, "LanguageItemTooltipName", value => config.LanguageItemTooltipName = value);
        CopyLegacyLanguage(root, "LanguageItemTooltipDescription", value => config.LanguageItemTooltipDescription = value);
        CopyLegacyLanguage(root, "LanguageActionTooltipName", value => config.LanguageActionTooltipName = value);
        CopyLegacyLanguage(root, "LanguageActionTooltipDescription", value => config.LanguageActionTooltipDescription = value);
        CopyLegacyLanguage(root, "ContentsFinderName", value => config.GameUiNameLanguage = value);
        CopyLegacyLanguage(root, "ContentsFinderDescription", value => config.GameUiDescriptionLanguage = value);

        CopyInt(root, "ItemNameColourKey", value => config.ItemNameColourKey = value);
        CopyInt(root, "ItemDescriptionColourKey", value => config.ItemDescriptionColourKey = value);
        CopyInt(root, "ActionNameColourKey", value => config.ActionNameColourKey = value);
        CopyInt(root, "ActionDescriptionColourKey", value => config.ActionDescriptionColourKey = value);
        CopyInt(root, "ContentNameColourKey", value => config.GameUiNameColourKey = value);
        CopyInt(root, "ContentDescColourKey", value => config.GameUiDescriptionColourKey = value);

        CopyBool(root, "TemporaryEnableOnly", value => config.TooltipShortcutEnabled = value);
        CopyHotkey(root, "TemporaryEnableHotkey", value => config.TooltipShortcutHotkey = value);

        return config;
    }

    private static void CopyLegacyLanguage(JsonObject root, string name, Action<BttLanguage> assign)
    {
        var legacyValue = ReadInt(root, name);
        if (legacyValue == null) return;

        var value = legacyValue.Value switch
        {
            0 => BttLanguage.Japanese,
            1 => BttLanguage.English,
            2 => BttLanguage.German,
            3 => BttLanguage.French,
            4 => BttLanguage.Off,
            _ => throw new InvalidDataException($"Unsupported legacy language value {legacyValue.Value} for {name}."),
        };

        assign(value);
    }
}
