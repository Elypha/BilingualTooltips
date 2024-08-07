using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text;
using Miosuke;
using System.Threading.Tasks;
using System;

namespace Miosuke;

public class HotkeyUi
{
    public HotkeyUi()
    {
    }

    public void Dispose()
    {
    }

    // -------------------------------- module --------------------------------
    public bool doSetInputFocused = false;
    public bool isEditingHotkey = false;
    public bool isInputActive = false;
    public List<VirtualKey> userHotkeyList = [];
    public string userHotkeyString = "";
    public bool isHotkeyChanged = false;

    public bool DrawConfigUi(string uniqueName, ref VirtualKey[] hotkey, uint inputWidth = 150)
    {
        // get user hotkey
        userHotkeyString = GetHotkeyString(hotkey);

        // update user hotkey from user input
        if (isEditingHotkey)
        {
            if (ImGui.GetIO().KeyAlt && !userHotkeyList.Contains(VirtualKey.MENU)) userHotkeyList.Add(VirtualKey.MENU);
            if (ImGui.GetIO().KeyShift && !userHotkeyList.Contains(VirtualKey.SHIFT)) userHotkeyList.Add(VirtualKey.SHIFT);
            if (ImGui.GetIO().KeyCtrl && !userHotkeyList.Contains(VirtualKey.CONTROL)) userHotkeyList.Add(VirtualKey.CONTROL);

            for (var i = 0; i < ImGui.GetIO().KeysDown.Count && i < 160; i++)
            {
                if (ImGui.GetIO().KeysDown[i])
                {
                    var vkey = (VirtualKey)i;

                    if (vkey == VirtualKey.ESCAPE)
                    {
                        // cancel editing
                        isEditingHotkey = false;
                        break;
                    }

                    if (!userHotkeyList.Contains(vkey))
                    {
                        userHotkeyList.Add(vkey);
                    }
                }
            }

            userHotkeyList.Sort();
            userHotkeyString = GetHotkeyString(userHotkeyList);
        }

        // draw hotkey input bar
        DrawHotkeyInput(uniqueName, inputWidth);

        // buttons and actions
        ImGui.SameLine();
        if (!isEditingHotkey)
        {
            if (ImGui.Button($"Edit##{uniqueName}-button-edit"))
            {
                // start editing
                isEditingHotkey = true;
                userHotkeyList = [];
            }
        }
        else
        {
            if (userHotkeyList.Count > 0)
            {
                // save and stop editing
                if (ImGui.Button($"Save##{uniqueName}-button-save"))
                {
                    isEditingHotkey = false;
                    if (userHotkeyList.Count > 0)
                    {
                        isHotkeyChanged = true;
                        hotkey = [.. userHotkeyList];
                    }
                }
            }
            else
            {
                // if no hotkey, show cancel button, cancel editing
                if (ImGui.Button($"Cancel##{uniqueName}-button-cancel"))
                {
                    isEditingHotkey = false;
                }
            }
        }

        // when editing, if buttons are not being clicked, and if input is not active, set focus
        if (isEditingHotkey && !ImGui.IsItemFocused() && !isInputActive)
        {
            doSetInputFocused = true;
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Click 'Edit' to set a new hotkey.\nClick 'Save' or a blank area to save.\nPress ESC on your keyboard to cancel."
        );

        // if changed, return true
        if (isHotkeyChanged)
        {
            isHotkeyChanged = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void DrawHotkeyInput(string uniqueName, uint inputWidth)
    {
        // set style
        if (isEditingHotkey)
        {
            ImGui.PushStyleColor(ImGuiCol.Border, 0xFF00A5FF);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2);
        }

        ImGui.SetNextItemWidth(inputWidth);
        ImGui.InputText($"##{uniqueName}-input-hotkey", ref userHotkeyString, 100, ImGuiInputTextFlags.ReadOnly);

        // set focus when required
        if (doSetInputFocused)
        {
            doSetInputFocused = false;
            ImGui.SetKeyboardFocusHere(-1);
        }
        isInputActive = ImGui.IsItemActive();

        // reset style
        if (isEditingHotkey)
        {
            ImGui.PopStyleColor(1);
            ImGui.PopStyleVar();
        }
    }

    public static string GetHotkeyString(IEnumerable<VirtualKey> hotkey)
    {
        return string.Join("+", hotkey.Select(k => k.GetKeyName()));
    }
}
