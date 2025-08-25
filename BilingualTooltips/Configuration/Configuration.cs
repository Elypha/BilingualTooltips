using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using System.Collections.Generic;
using System;
using System.Numerics;
using Miosuke.Configuration;


namespace BilingualTooltips.Configuration;

[Serializable]
public class BilingualTooltipsConfig : IMioConfig
{
    public int Version = 0;

    // ----------------- General -----------------
    public bool Enabled = true;
    public bool TemporaryEnableOnly = false;
    public VirtualKey[] TemporaryEnableHotkey = [VirtualKey.CONTROL, VirtualKey.B];
    public bool ItemTooltipPanelHotkeyEnabled = false;
    public bool ItemTooltipPanelHotkeyOpenWindow = true;
    public bool ItemTooltipPanelUpdateOnHotkey = true;
    public VirtualKey[] ItemTooltipPanelHotkey = [VirtualKey.CONTROL, VirtualKey.MENU, VirtualKey.L];
    public GameLanguage ItemTooltipPanelText1 = GameLanguage.Japanese;
    public GameLanguage ItemTooltipPanelText2 = GameLanguage.English;
    public GameLanguage ItemTooltipPanelText3 = GameLanguage.German;
    public GameLanguage ItemTooltipPanelText4 = GameLanguage.French;
    public GameLanguage LanguageItemTooltipName = GameLanguage.Japanese;
    public GameLanguage LanguageItemTooltipDescription = GameLanguage.Japanese;
    public GameLanguage LanguageActionTooltipName = GameLanguage.Japanese;
    public GameLanguage LanguageActionTooltipDescription = GameLanguage.Japanese;
    public GameLanguage ContentsFinderName = GameLanguage.Japanese;
    public GameLanguage ContentsFinderDescription = GameLanguage.Japanese;

    public int ItemNameColourKey = 3;
    public int ItemDescriptionColourKey = 3;
    public int ActionNameColourKey = 3;
    public int ActionDescriptionColourKey = 3;

    public int ContentNameColourKey = 11;
    public int ContentDescColourKey = 11;

    public bool EnableTheme = false;

    public float OffsetItemNameOriginal = 4.5f;
    public float OffsetItemNameTranslation = 2.0f;
    public float OffsetActionNameOriginal = -1.0f;
    public float OffsetActionNameTranslation = -8.5f;
    public float OffsetContentNameOriginal = -1.5f;
    public float OffsetContentNameTranslation = 7.0f;

    public string CustomTheme = "";
}
