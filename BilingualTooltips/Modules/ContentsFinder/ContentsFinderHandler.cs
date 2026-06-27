using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.Addon.Lifecycle;

namespace BilingualTooltips.Modules;

public unsafe partial class ContentsFinderHandler
{
    private readonly BilingualTooltipsPlugin _plugin;
    private readonly ContentsFinderNameController _journalDetail;
    private readonly ContentsFinderNameController _contentsFinderConfirm;
    private bool _disposed;

    public ContentsFinderHandler(BilingualTooltipsPlugin plugin)
    {
        _plugin = plugin;
        _journalDetail = new ContentsFinderNameController(
            ContentsFinderNameAddonContract.JournalDetail,
            ShouldShowContentName,
            ResolveJournalDetailContent,
            GetContentNameAppearance,
            LocateParentScopedJournalDetailAddon,
            RecordContentLookup);
        _contentsFinderConfirm = new ContentsFinderNameController(
            ContentsFinderNameAddonContract.ContentsFinderConfirm,
            ShouldShowContentName,
            ResolveContentsFinderConfirmContent,
            GetContentNameAppearance,
            LocateTopLevelContentsFinderConfirmAddon,
            RecordContentLookup);
    }

    public void StartHook()
    {
        _disposed = false;
        RegisterContentsFinderListeners();
        RegisterJournalDetailListeners();
        RegisterContentsFinderConfirmListeners();
        Service.ClientState.CfPop += ClientStateOnCfPop;
    }

    public void StopHook()
    {
        Service.ClientState.CfPop -= ClientStateOnCfPop;
        UnregisterContentsFinderConfirmListeners();
        UnregisterJournalDetailListeners();
        UnregisterContentsFinderListeners();
    }

    public void Dispose()
    {
        _disposed = true;
        ReleaseJournalDetail();
        ReleaseContentsFinderConfirm();
        ClearContentsFinderConfirmIdentity();
    }

    public void ReleaseJournalDetail() => _journalDetail.ReleaseCurrent();

    public void ReleaseContentsFinderConfirm() => _contentsFinderConfirm.ReleaseCurrent();

    private void RegisterContentsFinderListeners()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ContentsFinder", ContentsFinderRefreshHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreHide, "ContentsFinder", ContentsFinderCleanupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ContentsFinder", ContentsFinderCleanupHandler);
    }

    private void UnregisterContentsFinderListeners()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ContentsFinder", ContentsFinderRefreshHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreHide, "ContentsFinder", ContentsFinderCleanupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "ContentsFinder", ContentsFinderCleanupHandler);
    }

    private void RegisterJournalDetailListeners()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreHide, _journalDetail.AddonName, JournalDetailCleanupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, _journalDetail.AddonName, JournalDetailCleanupHandler);
    }

    private void UnregisterJournalDetailListeners()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreHide, _journalDetail.AddonName, JournalDetailCleanupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, _journalDetail.AddonName, JournalDetailCleanupHandler);
    }

    private void RegisterContentsFinderConfirmListeners()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, _contentsFinderConfirm.AddonName, ContentsFinderConfirmPostSetupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreHide, _contentsFinderConfirm.AddonName, ContentsFinderConfirmCleanupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, _contentsFinderConfirm.AddonName, ContentsFinderConfirmCleanupHandler);
    }

    private void UnregisterContentsFinderConfirmListeners()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, _contentsFinderConfirm.AddonName, ContentsFinderConfirmPostSetupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreHide, _contentsFinderConfirm.AddonName, ContentsFinderConfirmCleanupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, _contentsFinderConfirm.AddonName, ContentsFinderConfirmCleanupHandler);
    }

    private bool ShouldShowContentName() =>
        _plugin.Config.GameUiNameLanguage != BttLanguage.Off;

    private ContentsFinderNameAppearance GetContentNameAppearance() =>
        new(
            (ushort)_plugin.Config.GameUiNameColourKey,
            _plugin.Config.OffsetGameUiNameNative,
            _plugin.Config.OffsetGameUiNameTranslation);

    private ContentsFinderNameContent? ResolveContentName(GameTextResolver.ContentReference reference, string displayName)
    {
        if (!GameTextResolver.TryGetContentName(reference, _plugin.Config.GameUiNameLanguage, out var translation))
            return null;

        translation = FormatContentNameTranslation(translation);
        if (string.IsNullOrWhiteSpace(translation)) return null;

        var key = BttLookupKey.Content(reference);
        return new ContentsFinderNameContent(
            key,
            string.IsNullOrWhiteSpace(displayName) ? BttLookupDisplay.DisplayId(key) : displayName,
            translation);
    }

    private static string ResolveCurrentClientContentName(GameTextResolver.ContentReference reference) =>
        GameTextResolver.TryGetContentName(
            reference,
            BttLanguageExtensions.FromClientLanguage(Service.ClientState.ClientLanguage),
            out var displayName)
            ? displayName
            : BttLookupDisplay.DisplayId(BttLookupKey.Content(reference));

    private void RecordContentLookup(ContentsFinderNameContent content) => _plugin.RecordLookup(content.Key, content.DisplayName);

    private static string FormatContentNameTranslation(string translation) =>
        translation.StartsWith("the ", StringComparison.Ordinal)
            ? "The " + translation[4..]
            : translation;
}
