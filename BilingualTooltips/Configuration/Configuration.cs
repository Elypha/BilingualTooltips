using BilingualTooltips.Modules.Dialogue.Data;
using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.ClientState.Keys;
using Miosuke.Configuration;

namespace BilingualTooltips.Configuration;

[Serializable]
public class BilingualTooltipsConfig : IMioConfig
{
    // internal
    // --------------------------------
    public int Version = 0;
    public const int LookupHistoryLimitMin = 1;
    public const int LookupHistoryLimitMax = 9999;

    // general
    // --------------------------------
    public bool EnableTheme = true;
    public string CustomTheme = "";

    // history
    // --------------------------------
    public bool LookupHistoryEnabled = true;
    public int LookupHistoryLimit = 2000;
    public BttLookupHistorySource LookupHistorySources = BttLookupHistorySource.All;
    public bool LookupHistoryHotkeyEnabled = false;
    public VirtualKey[] LookupHistoryHotkey = [VirtualKey.CONTROL, VirtualKey.MENU, VirtualKey.H];
    public bool LookupHistoryShowcaseEnabled = true;

    // tooltips
    // --------------------------------
    public BttLanguage LanguageItemTooltipName = BttLanguage.Japanese;
    public BttLanguage LanguageItemTooltipDescription = BttLanguage.Japanese;
    public BttLanguage LanguageActionTooltipName = BttLanguage.Japanese;
    public BttLanguage LanguageActionTooltipDescription = BttLanguage.Japanese;

    public bool TooltipShortcutEnabled = false;
    public VirtualKey[] TooltipShortcutHotkey = [VirtualKey.CONTROL, VirtualKey.MENU, VirtualKey.T];

    public int ItemNameColourKey = 3;
    public int ItemDescriptionColourKey = 3;
    public int ActionNameColourKey = 3;
    public int ActionDescriptionColourKey = 3;
    public float OffsetItemNameNative = 4.5f;
    public float OffsetItemNameTranslation = 2.0f;
    public float OffsetActionNameNative = -0.5f;
    public float OffsetActionNameTranslation = -8.0f;
    public ushort TooltipNameMaxLineWidth = 300;

    // game ui
    // --------------------------------
    public BttLanguage GameUiNameLanguage = BttLanguage.Japanese;
    public BttLanguage GameUiDescriptionLanguage = BttLanguage.Japanese;

    public int GameUiNameColourKey = 11;
    public int GameUiDescriptionColourKey = 11;
    public float OffsetGameUiNameNative = -1.5f;
    public float OffsetGameUiNameTranslation = 7.0f;

    public BttLanguage CosmicMissionNameLanguage = BttLanguage.Japanese;
    public int CosmicMissionNameColourKey = 10;
    public float OffsetCosmicMissionNameNative = -1.5f;
    public float OffsetCosmicMissionNameTranslation = 22.5f;

    // npc dialogue
    // --------------------------------
    public string TalkDialogueVersionManifestUrl = "";
    public string TalkDialogueDataPath = "";

    public bool TalkDialogueEnabled = false;
    public BttLanguage TalkDialogueInputLanguage = BttLanguage.Auto;
    public BttLanguage TalkDialogueTargetLanguage = BttLanguage.Japanese;
    public bool TalkDialogueOverlayShowSourceText = false;
    public bool TalkDialogueOverlayShowMatchDetails = false;
    public bool TalkDialogueOverlayEnabled = true;
    public bool TalkDialogueOpenWindowAutomatically = true;
    public bool TalkDialogueOverlayShortcutEnabled = false;
    public VirtualKey[] TalkDialogueOverlayShortcutHotkey = [VirtualKey.CONTROL, VirtualKey.MENU, VirtualKey.D];

    public string TalkDialogueRenderProfileFirstName = "";
    public string TalkDialogueRenderProfileLastName = "";
    public BttDialogueGender TalkDialogueRenderProfileGender = BttDialogueGender.Unspecified;
}
