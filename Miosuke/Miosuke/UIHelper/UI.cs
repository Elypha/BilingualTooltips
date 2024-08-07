using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;


namespace Miosuke;

public class UI
{
    // -------------------------------- ImGui utils --------------------------------
    public static readonly Vector4 titleColour = HSLA_to_Decimal(25, 0.65, 0.75, 1.0);
    public static readonly Vector4 textColour = HSLA_to_Decimal(0, 0.0, 0.95, 1.0);
    public static readonly Vector4 textColourDim = HSLA_to_Decimal(0, 0.0, 0.6, 1.0);
    public static readonly Vector4 bulletTitleColour = HSLA_to_Decimal(35, 0.75, 0.75, 1.0);
    public static readonly Vector4 bulletListColour = HSLA_to_Decimal(0, 0.0, 0.8, 1.0);
    public static readonly Vector4 ColourHq = HSLA_to_Decimal(40, 0.9, 0.7, 1.0);
    public static readonly Vector4 ColourCyan = HSLA_to_Decimal(200, 0.85, 0.6, 1.0);
    public static readonly Vector4 ColourRedLight = HSLA_to_Decimal(5, 0.75, 0.6, 1.0);
    public static readonly Vector4 ColourWhite = HSLA_to_Decimal(0, 0, 1.0, 1.0);
    public static readonly Vector4 ColourKhaki = HSLA_to_Decimal(25, 0.65, 0.75, 1.0);


    public static void BulletTextList(string title, string? description, List<string> list)
    {

        ImGui.TextColored(bulletTitleColour, title);

        if (description is not null)
        {
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(description);
        }

        ImGui.Indent();

        ImGui.PushTextWrapPos();
        ImGui.PushStyleColor(ImGuiCol.Text, bulletListColour);
        foreach (var text in list)
        {
            ImGui.Text(text);
        }
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();

        ImGui.Unindent();
    }

    public static void ChangelogList(string title, List<string> list)
    {
        ImGui.TextColored(titleColour, title);
        ImGui.Separator();
        ImGui.Indent();

        ImGui.PushTextWrapPos();
        ImGui.PushStyleColor(ImGuiCol.Text, textColour);
        foreach (var text in list)
        {
            ImGui.Text(text);
        }
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();

        ImGui.Unindent();
        ImGui.Text("");
    }

    public static void AlignRight(string text)
    {
        var posX = ImGui.GetCursorPosX()
            + ImGui.GetColumnWidth()
            - ImGui.CalcTextSize(text).X
            - ImGui.GetScrollX()
            - (1 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetCursorPosX(posX);
    }


    // -------------------------------- ffxiv utils --------------------------------
    public static void RenderSeString(SeString seString)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
        foreach (var payload in seString.Payloads)
        {
            if (payload is TextPayload textPayload)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X);
                // show \n as plain text
                var plain_text = textPayload.Text?.Replace("\n", "\\n");
                ImGui.Text(plain_text);
                ImGui.SameLine();
            }
            else if (payload is UIForegroundPayload uiForegroundPayload)
            {
                if (uiForegroundPayload.IsEnabled)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Colour_uint_to_Decimal(uiForegroundPayload.UIColor.UIForeground));
                }
                else
                {
                    ImGui.PopStyleColor();
                }
            }
            // else if (payload is UIGlowPayload uiGlowPayload)
            // {
            // }
        }
    }
    private static Vector4 Colour_uint_to_Decimal(uint color)
    {
        var r = (byte)(color >> 24);
        var g = (byte)(color >> 16);
        var b = (byte)(color >> 8);
        var a = (byte)color;

        return new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }


    // -------------------------------- colour utils --------------------------------
    /// <summary>
    /// (255, 255, 255, 1) -> (1, 1, 1, 1)
    /// </summary>
    public static Vector4 RGBA_to_Decimal(byte r, byte g, byte b, byte a)
    {
        return new Vector4(r / 255f, g / 255f, b / 255f, a / 1f);
    }

    /// <summary>
    /// (360, 1, 1, 1) -> (1, 1, 1, 1)
    /// </summary>
    public static Vector4 HSLA_to_Decimal(double h, double s, double l, double a = 1.0)
    {
        var rgba = HSLA_to_RGBA(h, s, l, a);
        return new Vector4(rgba.X / 255f, rgba.Y / 255f, rgba.Z / 255f, rgba.W / 1f);
    }

    /// <summary>
    /// (360, 1, 1, 1) -> (255, 255, 255, 1)
    /// </summary>
    public static Vector4 HSLA_to_RGBA(double h, double s, double l, double a = 1.0)
    {
        double v;
        double r, g, b;

        h /= 360.0;

        r = l;   // default to gray
        g = l;
        b = l;
        v = (l <= 0.5) ? (l * (1.0 + s)) : (l + s - l * s);

        if (v > 0)
        {
            double m;
            double sv;
            int sextant;
            double fract, vsf, mid1, mid2;

            m = l + l - v;
            sv = (v - m) / v;
            h *= 6.0;
            sextant = (int)h;
            fract = h - sextant;
            vsf = v * sv * fract;
            mid1 = m + vsf;
            mid2 = v - vsf;

            switch (sextant)
            {
                case 0:
                    r = v;
                    g = mid1;
                    b = m;
                    break;
                case 1:
                    r = mid2;
                    g = v;
                    b = m;
                    break;
                case 2:
                    r = m;
                    g = v;
                    b = mid1;
                    break;
                case 3:
                    r = m;
                    g = mid2;
                    b = v;
                    break;
                case 4:
                    r = mid1;
                    g = m;
                    b = v;
                    break;
                case 5:
                    r = v;
                    g = m;
                    b = mid2;
                    break;
            }
        }
        return new Vector4((float)r * 255, (float)g * 255, (float)b * 255, (float)a);
    }
}
