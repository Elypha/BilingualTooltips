using System.Collections.Concurrent;
using Dalamud.Game.Gui;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using ClientLanguage = Dalamud.Game.ClientLanguage;

namespace BilingualTooltips.Modules;

public static class GameTextResolver
{
    public enum ContentType
    {
        Unknown,
        Duty,
        Roulette,
        CosmicMission,
    }

    public readonly record struct TooltipText(string Name, string Description);

    public readonly record struct ActionReference(DetailKind Kind, uint RowId);

    public readonly record struct ContentReference(ContentType Type, uint RowId);

    private sealed record ActionTooltipRule(
        Func<HoveredAction, uint> Id,
        Func<uint, BttLanguage, string> Name,
        Func<uint, BttLanguage, string> Description
    );

    private static readonly Dictionary<DetailKind, ActionTooltipRule> ActionRules = new()
    {
        [DetailKind.Action] = new(
            static action => action.ActionId,
            static (id, language) => Read<Lumina.Excel.Sheets.Action>(id, language, static row => row.Name),
            static (id, language) => Read<ActionTransient>(id, language, static row => row.Description)),
        [DetailKind.GeneralAction] = new(
            static action => action.BaseActionId,
            static (id, language) => Read<GeneralAction>(id, language, static row => row.Name),
            static (id, language) => Read<GeneralAction>(id, language, static row => row.Description)),
        [DetailKind.Trait] = new(
            static action => action.ActionId,
            static (id, language) => Read<Trait>(id, language, static row => row.Name),
            static (id, language) => Read<TraitTransient>(id, language, static row => row.Description)),
        [DetailKind.MainCommand] = new(
            static action => action.ActionId,
            static (id, language) => Read<MainCommand>(id, language, static row => row.Name),
            static (id, language) => Read<MainCommand>(id, language, static row => row.Description)),
        [DetailKind.Companion] = new(
            static action => action.ActionId,
            static (id, language) => Read<Companion>(id, language, static row => row.Singular),
            static (id, language) => Read<CompanionTransient>(id, language, static row => row.Description)),
        [DetailKind.Mount] = new(
            static action => action.ActionId,
            static (id, language) => Read<Mount>(id, language, static row => row.Singular),
            static (id, language) => Read<MountTransient>(id, language, static row => row.Description)),

        // DeepDungeon
        [DetailKind.DeepDungeonItem] = new(
            static action => action.ActionId,
            static (id, language) => Read<DeepDungeonItem>(id, language, static row => row.Name),
            static (id, language) => Read<DeepDungeonItem>(id, language, static row => row.Tooltip)),
        // extra deep-dungeon and Bozja/Zadnor special tooltip sources, pending test
        [DetailKind.DeepDungeonEquipment] = new(
            static action => action.ActionId,
            static (id, language) => Read<DeepDungeonEquipment>(id, language, static row => row.Name),
            static (id, language) => Read<DeepDungeonEquipment>(id, language, static row => row.Description)),
        [DetailKind.DeepDungeonEquipment2] = new(
            static action => action.ActionId,
            static (id, language) => Read<DeepDungeonEquipment>(id, language, static row => row.Name),
            static (id, language) => Read<DeepDungeonEquipment>(id, language, static row => row.Description)),
        [DetailKind.DeepDungeonMagicStone] = new(
            static action => action.ActionId,
            static (id, language) => Read<DeepDungeonMagicStone>(id, language, static row => row.Name),
            static (id, language) => Read<DeepDungeonMagicStone>(id, language, static row => row.Tooltip)),
        [DetailKind.DeepDungeonDemiclone] = new(
            static action => action.ActionId,
            static (id, language) => Read<DeepDungeonDemiclone>(id, language, static row => row.TitleCase),
            static (id, language) => Read<DeepDungeonDemiclone>(id, language, static row => row.Description)),
        [DetailKind.DeepDungeon4GimmickEffect] = new(
            static action => action.ActionId,
            static (id, language) => Read<DeepDungeon4GimmickEffectTransient>(id, language, static row => row.Name),
            static (id, language) => Read<DeepDungeon4GimmickEffectTransient>(id, language, static row => row.Description)),
        [DetailKind.MYCTemporaryItem] = new(
            static action => action.ActionId,
            static (id, language) => ReadMycTemporaryItemAction(id, language, static row => row.Name),
            static (id, language) => ReadMycTemporaryItemActionTransient(id, language, static row => row.Description)),
    };

    private static readonly ConcurrentDictionary<(Type RowType, ClientLanguage Language), object> Sheets = [];

    private static ExcelSheet<T> Sheet<T>(ClientLanguage language)
        where T : struct, IExcelRow<T> =>
        (ExcelSheet<T>)Sheets.GetOrAdd(
            (typeof(T), language),
            static key => Service.Data.GetExcelSheet<T>(key.Language));

    private static string Read<T>(uint rowId, BttLanguage language, Func<T, ReadOnlySeString> read)
        where T : struct, IExcelRow<T> =>
        language.TryGetClientLanguage(out var clientLanguage)
            ? Read(rowId, clientLanguage, read)
            : string.Empty;

    private static string Read<T>(uint rowId, ClientLanguage language, Func<T, ReadOnlySeString> read)
        where T : struct, IExcelRow<T> =>
        Sheet<T>(language).TryGetRow(rowId, out var row)
            ? read(row).ExtractText()
            : string.Empty;

    // action and item tooltips
    // --------------------------------
    public static bool TryGetActionTooltip(HoveredAction action, BttLanguage language, out TooltipText text)
    {
        text = default;
        return TryGetActionReference(action, out var reference)
            && TryGetActionTooltip(reference, language, out text);
    }

    public static bool TryGetActionReference(HoveredAction action, out ActionReference reference)
    {
        reference = default;
        if (!ActionRules.TryGetValue(action.DetailKind, out var rule)) return false;

        var rowId = rule.Id(action);
        if (rowId == 0) return false;

        reference = new ActionReference(action.DetailKind, rowId);
        return true;
    }

    public static bool TryGetActionTooltip(ActionReference reference, BttLanguage language, out TooltipText text)
    {
        return TryGetActionTooltip(reference.RowId, reference.Kind, language, out text);
    }

    public static bool TryGetActionTooltip(uint id, DetailKind kind, BttLanguage language, out TooltipText text)
    {
        text = default;
        if (!ActionRules.TryGetValue(kind, out var rule)) return false;
        if (language == BttLanguage.Off) return false;

        var name = rule.Name(id, language);
        var description = rule.Description(id, language);
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description)) return false;

        text = new TooltipText(name, description);
        return true;
    }

    public static bool TryGetItemTooltip(ulong id, BttLanguage language, out TooltipText text)
    {
        text = default;
        if (language == BttLanguage.Off || id > uint.MaxValue) return false;
        if (!language.TryGetClientLanguage(out var clientLanguage)) return false;

        var rawItemId = (uint)id;
        var name = ItemUtil.GetItemName(rawItemId, includeIcon: false, clientLanguage).ExtractText();
        // take care of event items
        var description = ItemUtil.IsEventItem(rawItemId)
            ? Read<EventItemHelp>(rawItemId, language, static row => row.Description)
            : Read<Item>(ItemUtil.GetBaseId(rawItemId).ItemId, language, static row => row.Description);
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description)) return false;

        text = new TooltipText(name, description);
        return true;
    }

    // content references
    // --------------------------------
    private static bool TryCreateContentReference(ContentType type, uint rowId, out ContentReference reference)
    {
        reference = default;
        if (type == ContentType.Unknown || rowId == 0) return false;

        reference = new ContentReference(type, rowId);
        return true;
    }

    public static unsafe bool TryGetSelectedContentReference(out ContentReference reference)
    {
        reference = default;

        var agent = AgentContentsFinder.Instance();
        if (agent == null) return false;

        var selectedDuty = agent->SelectedDuty;
        var type = selectedDuty.ContentType switch
        {
            ContentsType.Regular => ContentType.Duty,
            ContentsType.Roulette => ContentType.Roulette,
            _ => ContentType.Unknown,
        };

        return TryCreateContentReference(type, selectedDuty.Id, out reference);
    }

    public static bool TryGetContentFinderConditionReference(uint rowId, out ContentReference reference) =>
        TryCreateContentReference(ContentType.Duty, rowId, out reference);

    public static bool TryGetCosmicMissionReference(uint rowId, out ContentReference reference) =>
        TryCreateContentReference(ContentType.CosmicMission, rowId, out reference);

    public static bool TryGetContentName(ContentReference reference, BttLanguage language, out string translation)
    {
        translation = string.Empty;
        return TryGetContentName(reference.Type, reference.RowId, language, out translation);
    }

    private static bool TryGetContentName(ContentType type, uint rowId, BttLanguage language, out string translation)
    {
        translation = string.Empty;
        if (language == BttLanguage.Off) return false;

        translation = type switch
        {
            ContentType.Duty => Read<ContentFinderCondition>(rowId, language, static row => row.Name),
            ContentType.Roulette => Read<ContentRoulette>(rowId, language, static row => row.Name),
            ContentType.CosmicMission => Read<WKSMissionUnit>(rowId, language, static row => row.Name),
            _ => string.Empty,
        };

        return !string.IsNullOrWhiteSpace(translation);
    }

    // out-of-pattern source adapters
    // --------------------------------
    private static string ReadMycTemporaryItemAction(
        uint rowId,
        BttLanguage language,
        Func<Lumina.Excel.Sheets.Action, ReadOnlySeString> read)
    {
        return language.TryGetClientLanguage(out var clientLanguage)
            ? ReadMycTemporaryItemAction(rowId, clientLanguage, read)
            : string.Empty;
    }

    private static string ReadMycTemporaryItemAction(
        uint rowId,
        ClientLanguage language,
        Func<Lumina.Excel.Sheets.Action, ReadOnlySeString> read)
    {
        return !TryGetMycTemporaryItemActionId(rowId, language, out var actionId)
            ? string.Empty
            : Sheet<Lumina.Excel.Sheets.Action>(language).TryGetRow(actionId, out var action)
                ? read(action).ExtractText()
                : string.Empty;
    }

    private static string ReadMycTemporaryItemActionTransient(
        uint rowId,
        BttLanguage language,
        Func<ActionTransient, ReadOnlySeString> read)
    {
        return language.TryGetClientLanguage(out var clientLanguage)
            ? ReadMycTemporaryItemActionTransient(rowId, clientLanguage, read)
            : string.Empty;
    }

    private static string ReadMycTemporaryItemActionTransient(
        uint rowId,
        ClientLanguage language,
        Func<ActionTransient, ReadOnlySeString> read)
    {
        return !TryGetMycTemporaryItemActionId(rowId, language, out var actionId)
            ? string.Empty
            : Sheet<ActionTransient>(language).TryGetRow(actionId, out var transient)
                ? read(transient).ExtractText()
                : string.Empty;
    }

    private static bool TryGetMycTemporaryItemActionId(uint rowId, ClientLanguage language, out uint actionId)
    {
        if (Sheet<MYCTemporaryItem>(language).TryGetRow(rowId, out var temporaryItem))
        {
            actionId = temporaryItem.Action.RowId;
            return actionId != 0;
        }

        if (Sheet<Lumina.Excel.Sheets.Action>(language).TryGetRow(rowId, out _))
        {
            actionId = rowId;
            return true;
        }

        actionId = 0;
        return false;
    }
}
