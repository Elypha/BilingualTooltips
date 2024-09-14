using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using System.Collections.Generic;
using System;


namespace BilingualTooltips;

[Serializable]
public class BilingualTooltipsConfig : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // ----------------- General -----------------
    public bool Enabled { get; set; } = true;
    public bool TemporaryEnableOnly { get; set; } = false;
    public VirtualKey[] TemporaryEnableHotkey { get; set; } = [VirtualKey.CONTROL, VirtualKey.X];
    public GameLanguage LanguageItemTooltipName { get; set; } = GameLanguage.Japanese;
    public GameLanguage LanguageItemTooltipDescription { get; set; } = GameLanguage.Japanese;
    public GameLanguage LanguageActionTooltipName { get; set; } = GameLanguage.Japanese;
    public GameLanguage LanguageActionTooltipDescription { get; set; } = GameLanguage.Japanese;
    public ushort ItemNameColourKey { get; set; } = 3;
    public ushort ItemDescriptionColourKey { get; set; } = 3;
    public ushort ActionNameColourKey { get; set; } = 3;
    public ushort ActionDescriptionColourKey { get; set; } = 3;
    public bool EnableTheme { get; set; } = false;
    public float OffsetItemNameOriginal { get; set; } = 4.5f;
    public float OffsetItemNameTranslation { get; set; } = 2.0f;
    public float OffsetActionNameOriginal { get; set; } = -1.0f;
    public float OffsetActionNameTranslation { get; set; } = -8.5f;
    public List<string> RegexList { get; set; } = new List<string>();


    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
