using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using System.Collections.Generic;
using System.Linq;


namespace Miosuke;

public class PrintMessage
{
    // -------------------------------- [  chat  ] --------------------------------
    public static void Chat(XivChatType channel, string prefixText, ushort? colour, List<Payload> payloadList)
    {
        var prefix = TextColoured(prefixText, colour ?? 557);

        if (channel == XivChatType.None)
        {
            Service.Chat.Print(new XivChatEntry
            {
                Message = new SeString(prefix.Concat(payloadList).ToList()),
            });
        }
        else
        {
            Service.Chat.Print(new XivChatEntry
            {
                Message = new SeString(prefix.Concat(payloadList).ToList()),
                Type = channel,
            });
        }
    }

    public static void Chat(XivChatType channel, List<Payload> payloadList)
    {
        if (channel == XivChatType.None)
        {
            Service.Chat.Print(new XivChatEntry
            {
                Message = new SeString(payloadList),
            });
        }
        else
        {
            Service.Chat.Print(new XivChatEntry
            {
                Message = new SeString(payloadList),
                Type = channel,
            });
        }
    }

    /// <summary>
    /// A text with foreground colour. See available colours via /xldata.
    /// </summary>
    /// <returns>A List of Dalamud.Game.Text.SeStringHandling.Payload</returns>
    public static List<Payload> TextColoured(string text, ushort colour)
    {
        return new List<Payload> {
            new UIForegroundPayload(colour),
            new TextPayload(text),
            new UIForegroundPayload(0),
        };
    }


    // -------------------------------- [  toast  ] --------------------------------
    public static void ToastNormal(string text)
    {
        Service.Toasts.ShowNormal(text);
    }
}
