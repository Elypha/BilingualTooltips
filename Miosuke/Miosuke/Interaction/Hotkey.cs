using Dalamud.Game.ClientState.Keys;
using System.Collections.Generic;
using System.Linq;
using System;


namespace Miosuke;

public class Hotkey
{
    // -------------------------------- hotkey methods --------------------------------
    public static List<VirtualKey> GetActiveKeys()
    {
        var activeKeys = new List<VirtualKey>();
        foreach (var vk in Service.KeyState.GetValidVirtualKeys())
        {
            if (Service.KeyState[vk]) activeKeys.Add(vk);
        }
        return activeKeys;
    }

    public static bool IsActive(VirtualKey[] keys, bool strict = false)
    {
        // check if all keys are active
        foreach (var vk in keys)
        {
            if (!Service.KeyState.IsVirtualKeyValid(vk)) continue;

            if (!Service.KeyState[vk]) return false;
        }

        // if strict, check if all active keys are in the list
        if (strict)
        {
            foreach (var vk in GetActiveKeys())
            {
                if (!keys.Contains(vk)) return false;
            }
        }

        return true;
    }
}

public static class Extensions
{
    private static readonly Dictionary<VirtualKey, string> NamedKeys = new() {
        { VirtualKey.KEY_0, "0"},
        { VirtualKey.KEY_1, "1"},
        { VirtualKey.KEY_2, "2"},
        { VirtualKey.KEY_3, "3"},
        { VirtualKey.KEY_4, "4"},
        { VirtualKey.KEY_5, "5"},
        { VirtualKey.KEY_6, "6"},
        { VirtualKey.KEY_7, "7"},
        { VirtualKey.KEY_8, "8"},
        { VirtualKey.KEY_9, "9"},
        { VirtualKey.CONTROL, "Ctrl"},
        { VirtualKey.MENU, "Alt"},
        { VirtualKey.SHIFT, "Shift"},
    };

    public static string GetKeyName(this VirtualKey k) => NamedKeys.TryGetValue(k, out var value) ? value : k.ToString();
}
