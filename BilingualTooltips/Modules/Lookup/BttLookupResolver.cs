using BilingualTooltips.Modules.Dialogue;
using BilingualTooltips.Modules.Dialogue.Data;

namespace BilingualTooltips.Modules.Lookup;

public sealed class BttLookupResolver
{
    private readonly BilingualTooltipsPlugin _plugin;

    public BttLookupResolver(BilingualTooltipsPlugin plugin)
    {
        _plugin = plugin;
    }

    public BttLookupResolvedItem Resolve(BttLookupKey key) => key.Kind switch
    {
        BttLookupKind.Item => ResolveItem(key),
        BttLookupKind.Action => ResolveAction(key),
        BttLookupKind.Content => ResolveContent(key),
        BttLookupKind.NpcDialogue => ResolveNpcDialogue(key),
        _ => BttLookupDisplay.Unavailable(key, BttLookupDisplay.UnsupportedLookupKindMessage),
    };

    private BttLookupResolvedItem ResolveItem(BttLookupKey key)
    {
        if (!key.TryGetItem(out var itemId))
            return BttLookupDisplay.Unavailable(key, BttLookupDisplay.InvalidItemKeyMessage);

        var lines = BttLanguageExtensions.AllLanguages
            .Select(language =>
                ResolveGameTextLine(language, () => GameTextResolver.TryGetItemTooltip(itemId, language, out var text)
                    ? text
                    : null))
            .ToArray();

        return new BttLookupResolvedItem(
            key,
            BttLookupDisplay.KindLabel(key),
            BttLookupDisplay.BestTitle(lines, BttLookupDisplay.FallbackTitle(key)),
            BttLookupDisplay.DisplayId(key),
            lines);
    }

    private BttLookupResolvedItem ResolveAction(BttLookupKey key)
    {
        if (!key.TryGetAction(out var action))
            return BttLookupDisplay.Unavailable(key, BttLookupDisplay.InvalidActionKeyMessage);

        var lines = BttLanguageExtensions.AllLanguages
            .Select(language =>
                ResolveGameTextLine(language, () => GameTextResolver.TryGetActionTooltip(action, language, out var text)
                    ? text
                    : null))
            .ToArray();
        return new BttLookupResolvedItem(
            key,
            BttLookupDisplay.KindLabel(key),
            BttLookupDisplay.BestTitle(lines, BttLookupDisplay.FallbackTitle(key)),
            BttLookupDisplay.DisplayId(key),
            lines);
    }

    private BttLookupResolvedLine ResolveGameTextLine(
        BttLanguage language,
        Func<GameTextResolver.TooltipText?> resolve)
    {
        if (!language.IsGameClientSupported())
            return new BttLookupResolvedLine(language, BttLookupDisplay.GameTextSourceUnavailableMessage, false);

        try
        {
            var text = resolve();
            if (text == null)
                return new BttLookupResolvedLine(language, BttLookupDisplay.NoTextInLanguageMessage, false);

            var body = BttLookupDisplay.JoinNameAndDescription(text.Value.Name, text.Value.Description);
            return string.IsNullOrWhiteSpace(body)
                ? new BttLookupResolvedLine(language, BttLookupDisplay.NoTextInLanguageMessage, false)
                : new BttLookupResolvedLine(language, body, true);
        }
        catch
        {
            return new BttLookupResolvedLine(language, BttLookupDisplay.FailedToReadLanguageMessage, false);
        }
    }

    private BttLookupResolvedItem ResolveContent(BttLookupKey key)
    {
        if (!key.TryGetContent(out var content) || !GameTextResolver.TryGetContentName(content, BttLanguage.English, out _))
            return BttLookupDisplay.Unavailable(key, BttLookupDisplay.InvalidContentKeyMessage);

        var lines = BttLanguageExtensions.AllLanguages
            .Select(language =>
            {
                if (!language.IsGameClientSupported())
                    return new BttLookupResolvedLine(language, BttLookupDisplay.GameUiTextSourceUnavailableMessage, false);

                return GameTextResolver.TryGetContentName(content, language, out var name)
                    ? new BttLookupResolvedLine(language, name.Trim(), true)
                    : new BttLookupResolvedLine(language, BttLookupDisplay.NoContentTextInLanguageMessage, false);
            })
            .ToArray();

        return new BttLookupResolvedItem(
            key,
            BttLookupDisplay.KindLabel(key),
            BttLookupDisplay.BestTitle(lines, BttLookupDisplay.FallbackTitle(key)),
            BttLookupDisplay.DisplayId(key),
            lines);
    }

    private BttLookupResolvedItem ResolveNpcDialogue(BttLookupKey key)
    {
        if (!key.TryGetNpcDialogue(out var dialogueKey))
            return BttLookupDisplay.Unavailable(key, BttLookupDisplay.InvalidNpcDialogueKeyMessage);

        var profile = DialogueRenderProfile.FromConfig(_plugin.Config);
        var lines = BttLanguageExtensions.AllLanguages
            .Select(language => ResolveNpcDialogueLine(dialogueKey, language, profile))
            .ToArray();

        return new BttLookupResolvedItem(
            key,
            BttLookupDisplay.KindLabel(key),
            BttLookupDisplay.BestTitle(lines, BttLookupDisplay.FallbackTitle(key)),
            dialogueKey,
            lines);
    }

    private BttLookupResolvedLine ResolveNpcDialogueLine(
        string key,
        BttLanguage language,
        DialogueRenderProfile profile)
    {
        if (!language.IsReleaseSupported())
            return new BttLookupResolvedLine(language, BttLookupDisplay.DialogueDataSourceUnavailableMessage, false);

        try
        {
            if (!_plugin.DialogueStoreProvider.TryGetStore(DialogueShardRequirement.ForLookupHistory(), out var store))
            {
                var message = _plugin.DialogueStoreProvider.IsPreparing
                    ? BttLookupDisplay.DialogueDataLoadingMessage
                    : BttLookupDisplay.NoDialoguePackageInstalledMessage;
                return new BttLookupResolvedLine(language, message, false);
            }

            return store.TryGetEntryText(key, language, profile, out var text)
                ? new BttLookupResolvedLine(language, text.Trim(), true)
                : new BttLookupResolvedLine(language, BttLookupDisplay.NoTextInLanguageMessage, false);
        }
        catch (Exception ex)
        {
            Service.Log.Debug(ex, $"[LookupHistory] Failed to resolve dialogue entry: {key}/{language}");
            return new BttLookupResolvedLine(language, BttLookupDisplay.FailedToReadLanguageMessage, false);
        }
    }
}
