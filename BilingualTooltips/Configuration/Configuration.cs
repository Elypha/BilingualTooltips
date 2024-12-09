﻿using Dalamud.Configuration;
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
    public VirtualKey[] TemporaryEnableHotkey = [VirtualKey.CONTROL, VirtualKey.X];
    // public VirtualKey[] ItemTooltipPanelHotkey = [VirtualKey.CONTROL, VirtualKey.X];
    // public GameLanguage ItemTooltipPanelText1 = GameLanguage.Japanese;
    // public GameLanguage ItemTooltipPanelText2 = GameLanguage.English;
    // public GameLanguage ItemTooltipPanelText3 = GameLanguage.Off;
    // public GameLanguage ItemTooltipPanelText4 = GameLanguage.Off;
    public VirtualKey[] ActionTooltipPanelHotkey = [VirtualKey.CONTROL, VirtualKey.X];
    public GameLanguage LanguageItemTooltipName = GameLanguage.Japanese;
    public GameLanguage LanguageItemTooltipDescription = GameLanguage.Japanese;
    public GameLanguage LanguageActionTooltipName = GameLanguage.Japanese;
    public GameLanguage LanguageActionTooltipDescription = GameLanguage.Japanese;
    public ushort ItemNameColourKey = 3;
    public ushort ItemDescriptionColourKey = 3;
    public ushort ActionNameColourKey = 3;
    public ushort ActionDescriptionColourKey = 3;
    public bool EnableTheme = false;
    public float OffsetItemNameOriginal = 4.5f;
    public float OffsetItemNameTranslation = 2.0f;
    public float OffsetActionNameOriginal = -1.0f;
    public float OffsetActionNameTranslation = -8.5f;
    public Vector2 OffsetGlamName = new Vector2(0.0f, 0.0f);
    public float GlamNameFontSize = 16.0f;
    public List<string> RegexList = [];

    public string CustomTheme = "";
}
