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
    public bool EnableTheme { get; set; } = false;
    public ushort ItemNameColourKey { get; set; } = 3;
    public ushort ItemDescriptionColourKey { get; set; } = 3;
    public ushort ActionNameColourKey { get; set; } = 3;
    public ushort ActionDescriptionColourKey { get; set; } = 3;
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
