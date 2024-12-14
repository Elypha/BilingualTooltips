using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using Miosuke.Action;
using Miosuke;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BilingualTooltips.Modules;

public static class SheetHelper
{
    // Action
    public static ExcelSheet<Lumina.Excel.Sheets.Action> SheetActionNameJa = Service.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<Lumina.Excel.Sheets.Action> SheetActionNameEn = Service.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<Lumina.Excel.Sheets.Action> SheetActionNameDe = Service.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<Lumina.Excel.Sheets.Action> SheetActionNameFr = Service.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.French);
    public static ExcelSheet<ActionTransient> SheetActionTransientDescJa = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<ActionTransient> SheetActionTransientDescEn = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<ActionTransient> SheetActionTransientDescDe = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<ActionTransient> SheetActionTransientDescFr = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.French);

    // GeneralAction
    public static ExcelSheet<GeneralAction> SheeGeneralActiontNameJa = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<GeneralAction> SheeGeneralActiontNameEn = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<GeneralAction> SheeGeneralActiontNameDe = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<GeneralAction> SheeGeneralActiontNameFr = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.French);
    public static ExcelSheet<GeneralAction> SheetGeneralActionDescJa = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<GeneralAction> SheetGeneralActionDescEn = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<GeneralAction> SheetGeneralActionDescDe = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<GeneralAction> SheetGeneralActionDescFr = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.French);

    // Trait
    public static ExcelSheet<Trait> SheetTraitNameJa = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<Trait> SheetTraitNameEn = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<Trait> SheetTraitNameDe = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<Trait> SheetTraitNameFr = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.French);
    public static ExcelSheet<TraitTransient> SheetTraitTransientDescJa = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<TraitTransient> SheetTraitTransientDescEn = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<TraitTransient> SheetTraitTransientDescDe = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<TraitTransient> SheetTraitTransientDescFr = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.French);

    // Item
    public static ExcelSheet<Item> SheetItemJa = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<Item> SheetItemEn = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<Item> SheetItemDe = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<Item> SheetItemFr = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.French);

    // ContentFinder
    public static ExcelSheet<ContentFinderCondition> SheetContentFinderConditionJa = Service.Data.GetExcelSheet<ContentFinderCondition>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<ContentFinderCondition> SheetContentFinderConditionEn = Service.Data.GetExcelSheet<ContentFinderCondition>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<ContentFinderCondition> SheetContentFinderConditionDe = Service.Data.GetExcelSheet<ContentFinderCondition>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<ContentFinderCondition> SheetContentFinderConditionFr = Service.Data.GetExcelSheet<ContentFinderCondition>(Dalamud.Game.ClientLanguage.French);

    // Roulette
    public static ExcelSheet<ContentRoulette> SheetContentRouletteJa = Service.Data.GetExcelSheet<ContentRoulette>(Dalamud.Game.ClientLanguage.Japanese);
    public static ExcelSheet<ContentRoulette> SheetContentRouletteEn = Service.Data.GetExcelSheet<ContentRoulette>(Dalamud.Game.ClientLanguage.English);
    public static ExcelSheet<ContentRoulette> SheetContentRouletteDe = Service.Data.GetExcelSheet<ContentRoulette>(Dalamud.Game.ClientLanguage.German);
    public static ExcelSheet<ContentRoulette> SheetContentRouletteFr = Service.Data.GetExcelSheet<ContentRoulette>(Dalamud.Game.ClientLanguage.French);

    public static string? GetActionName(HoveredAction action, GameLanguage lang)
    {
        try
        {
            var type = action.ActionKind;
            var id = type switch
            {
                HoverActionKind.GeneralAction => action.BaseActionID,
                _ => action.ActionID,
            };
            return GetActionName(id, type, lang);
        }
        catch (System.NotImplementedException)
        {
            return null;
        }
    }

    public static string GetActionName(uint id, HoverActionKind type, GameLanguage lang)
    {
        return type switch
        {
            HoverActionKind.Action => lang switch
            {
                GameLanguage.Japanese => SheetActionNameJa.GetRow(id).Name.ExtractText(),
                GameLanguage.English => SheetActionNameEn.GetRow(id).Name.ExtractText(),
                GameLanguage.German => SheetActionNameDe.GetRow(id).Name.ExtractText(),
                GameLanguage.French => SheetActionNameFr.GetRow(id).Name.ExtractText(),
                _ => throw new System.NotImplementedException(),
            },
            HoverActionKind.GeneralAction => lang switch
            {
                GameLanguage.Japanese => SheeGeneralActiontNameJa.GetRow(id).Name.ExtractText(),
                GameLanguage.English => SheeGeneralActiontNameEn.GetRow(id).Name.ExtractText(),
                GameLanguage.German => SheeGeneralActiontNameDe.GetRow(id).Name.ExtractText(),
                GameLanguage.French => SheeGeneralActiontNameFr.GetRow(id).Name.ExtractText(),
                _ => throw new System.NotImplementedException(),
            },
            HoverActionKind.Trait => lang switch
            {
                GameLanguage.Japanese => SheetTraitNameJa.GetRow(id).Name.ExtractText(),
                GameLanguage.English => SheetTraitNameEn.GetRow(id).Name.ExtractText(),
                GameLanguage.German => SheetTraitNameDe.GetRow(id).Name.ExtractText(),
                GameLanguage.French => SheetTraitNameFr.GetRow(id).Name.ExtractText(),
                _ => throw new System.NotImplementedException(),
            },
            _ => throw new System.NotImplementedException(),
        };
    }

    public static string? GetActionDescription(HoveredAction action, GameLanguage lang)
    {
        try
        {
            var type = action.ActionKind;
            var id = type switch
            {
                HoverActionKind.GeneralAction => action.BaseActionID,
                _ => action.ActionID,
            };
            return GetActionDescription(id, type, lang);
        }
        catch (System.NotImplementedException)
        {
            return null;
        }
    }

    public static string GetActionDescription(uint id, HoverActionKind type, GameLanguage lang)
    {
        return type switch
        {
            HoverActionKind.Action => lang switch
            {
                GameLanguage.Japanese => SheetActionTransientDescJa.GetRow(id).Description.ExtractText(),
                GameLanguage.English => SheetActionTransientDescEn.GetRow(id).Description.ExtractText(),
                GameLanguage.German => SheetActionTransientDescDe.GetRow(id).Description.ExtractText(),
                GameLanguage.French => SheetActionTransientDescFr.GetRow(id).Description.ExtractText(),
                _ => throw new System.NotImplementedException(),
            },
            HoverActionKind.GeneralAction => lang switch
            {
                GameLanguage.Japanese => SheetGeneralActionDescJa.GetRow(id).Description.ExtractText(),
                GameLanguage.English => SheetGeneralActionDescEn.GetRow(id).Description.ExtractText(),
                GameLanguage.German => SheetGeneralActionDescDe.GetRow(id).Description.ExtractText(),
                GameLanguage.French => SheetGeneralActionDescFr.GetRow(id).Description.ExtractText(),
                _ => throw new System.NotImplementedException(),
            },
            HoverActionKind.Trait => lang switch
            {
                GameLanguage.Japanese => SheetTraitTransientDescJa.GetRow(id).Description.ExtractText(),
                GameLanguage.English => SheetTraitTransientDescEn.GetRow(id).Description.ExtractText(),
                GameLanguage.German => SheetTraitTransientDescDe.GetRow(id).Description.ExtractText(),
                GameLanguage.French => SheetTraitTransientDescFr.GetRow(id).Description.ExtractText(),
                _ => throw new System.NotImplementedException(),
            },
            _ => throw new System.NotImplementedException(),
        };
    }

    public static string? GetItemName(ulong id, GameLanguage lang)
    {
        var rowId = GetRealItemId(id);
        return lang switch
        {
            GameLanguage.Japanese => SheetItemJa.GetRow(rowId).Name.ExtractText(),
            GameLanguage.English => SheetItemEn.GetRow(rowId).Name.ExtractText(),
            GameLanguage.German => SheetItemDe.GetRow(rowId).Name.ExtractText(),
            GameLanguage.French => SheetItemFr.GetRow(rowId).Name.ExtractText(),
            _ => null,
        };
    }

    public static string? GetItemDescription(ulong id, GameLanguage lang)
    {
        var rowId = GetRealItemId(id);
        return lang switch
        {
            GameLanguage.Japanese => SheetItemJa.GetRow(rowId).Description.ExtractText(),
            GameLanguage.English => SheetItemEn.GetRow(rowId).Description.ExtractText(),
            GameLanguage.German => SheetItemDe.GetRow(rowId).Description.ExtractText(),
            GameLanguage.French => SheetItemFr.GetRow(rowId).Description.ExtractText(),
            _ => null,
        };
    }

    public static uint GetRealItemId(ulong id)
    {
        return id < 2000000 ? (uint)id % 500000 : (uint)id;
    }

    public static string? GetContentName(string name, GameLanguage lang)
    {
        var type = GetContentType(name, out var RowId);

        return type switch
        {
            ContentType.Duty => lang switch
            {
                GameLanguage.Japanese => SheetContentFinderConditionJa.GetRow(RowId).Name.ExtractText(),
                GameLanguage.English => SheetContentFinderConditionEn.GetRow(RowId).Name.ExtractText(),
                GameLanguage.German => SheetContentFinderConditionDe.GetRow(RowId).Name.ExtractText(),
                GameLanguage.French => SheetContentFinderConditionFr.GetRow(RowId).Name.ExtractText(),
                _ => null,
            },
            ContentType.Roulette => lang switch
            {
                GameLanguage.Japanese => SheetContentRouletteJa.GetRow(RowId).Name.ExtractText(),
                GameLanguage.English => SheetContentRouletteEn.GetRow(RowId).Name.ExtractText(),
                GameLanguage.German => SheetContentRouletteDe.GetRow(RowId).Name.ExtractText(),
                GameLanguage.French => SheetContentRouletteFr.GetRow(RowId).Name.ExtractText(),
                _ => null,
            },
            _ => null,
        };
    }

    public enum ContentType
    {
        Unknown,
        Duty,
        Roulette,
    }

    public static ContentType GetContentType(string name, out uint RowId)
    {
        // remove line break
        name = name.Replace(Dalamud.Game.Text.SeStringHandling.Payloads.NewLinePayload.Payload.Text, "");

        RowId = P.ClientLanguage switch
        {
            Dalamud.Game.ClientLanguage.Japanese => SheetContentFinderConditionJa.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            Dalamud.Game.ClientLanguage.English => SheetContentFinderConditionEn.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            Dalamud.Game.ClientLanguage.German => SheetContentFinderConditionDe.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            Dalamud.Game.ClientLanguage.French => SheetContentFinderConditionFr.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            _ => 0,
        };
        if (RowId != 0) return ContentType.Duty;

        RowId = P.ClientLanguage switch
        {
            Dalamud.Game.ClientLanguage.Japanese => SheetContentRouletteJa.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            Dalamud.Game.ClientLanguage.English => SheetContentRouletteEn.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            Dalamud.Game.ClientLanguage.German => SheetContentRouletteDe.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            Dalamud.Game.ClientLanguage.French => SheetContentRouletteFr.Where(x => x.Name.ToString() == name).FirstOrDefault().RowId,
            _ => 0,
        };
        if (RowId != 0) return ContentType.Roulette;

        Service.Log.Debug($"Unknown content name: {name}");

        return ContentType.Unknown;
    }
}