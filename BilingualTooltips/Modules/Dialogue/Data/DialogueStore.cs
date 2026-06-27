using System.IO;
using BilingualTooltips.Modules.Dialogue.Package;

namespace BilingualTooltips.Modules.Dialogue.Data;

public sealed class DialogueStore
{
    private readonly string _packageDirectory;
    private readonly DialoguePackageManifest _manifest;
    private readonly DialogueCommonShard _common;
    private readonly Lock _shardLock = new();
    private readonly Dictionary<BttLanguage, DialogueMatchShard> _matchShards = [];
    private readonly Dictionary<BttLanguage, DialogueTemplateShard> _templateShards = [];

    public string DataPath { get; }
    public string PackageSummary { get; }
    public int EntryCount => _manifest.EntryCount;

    // package metadata
    // --------------------------------
    public DialogueStore(string dataPath)
    {
        DataPath = ResolveDirectoryToManifest(dataPath);
        _packageDirectory = Path.GetDirectoryName(DataPath) ?? "";
        _manifest = ReadManifest(DataPath);
        ValidateInstalledPackage(_packageDirectory);
        var entriesFile = DialoguePackageLayout.RequiredShardPath(
            DialoguePackageStorage.InstalledZstd,
            DialoguePackageLayout.EntriesRole);
        _common = DialogueCommonShard.Load(
            DialogueShardReader.ReadInstalledPackageFile(_packageDirectory, entriesFile),
            _manifest.EntryCount);
        PackageSummary = $"package:{_manifest.EntryCount}";
        Service.Log.Info($"[DialogueStore] Loaded dialogue package metadata from {DataPath} ({PackageSummary})");
    }

    public static bool TryResolveDataPath(string? customPath, out string dataPath, out string reason)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            dataPath = ResolveDirectoryToManifest(customPath);
            var exists = File.Exists(dataPath)
                && string.Equals(Path.GetFileName(dataPath), DialoguePackageLayout.ManifestFileName, StringComparison.OrdinalIgnoreCase);
            reason = exists
                ? "Custom dialogue package manifest."
                : "Custom dialogue package manifest was not found.";
            return exists;
        }

        dataPath = DialoguePaths.UserActivePackageManifestPath;
        if (File.Exists(dataPath))
        {
            reason = "Active dialogue package.";
            return true;
        }

        reason = "No dialogue package is installed.";
        return false;
    }

    public static DialoguePackageInfo? TryReadPackageInfo(string? customPath)
    {
        if (!TryResolveDataPath(customPath, out var dataPath, out _)) return null;

        dataPath = ResolveDirectoryToManifest(dataPath);
        if (!File.Exists(dataPath)) return null;

        try
        {
            var manifest = ReadManifest(dataPath);
            var packageDirectory = Path.GetDirectoryName(dataPath) ?? "";
            ValidateInstalledPackage(packageDirectory);
            return new DialoguePackageInfo(
                dataPath,
                manifest.BuildNumber,
                manifest.GameVersion,
                manifest.BuiltAt,
                manifest.FormatVersion,
                manifest.EntryCount,
                $"package:{manifest.EntryCount}");
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, $"[DialogueStore] Failed to read dialogue package metadata: {dataPath}");
            return new DialoguePackageInfo(dataPath, null, null, null, null, null, "invalid-manifest");
        }
    }

    // dialogue lookup
    // --------------------------------
    public DialogueMatch? Match(string text, BttLanguage inputLanguage, BttLanguage targetLanguage)
    {
        inputLanguage = inputLanguage.ResolveInput();
        if (inputLanguage is BttLanguage.Off or BttLanguage.Auto) return null;
        if (targetLanguage is BttLanguage.Off or BttLanguage.Auto) return null;
        if (!inputLanguage.IsReleaseSupported() || !targetLanguage.IsReleaseSupported()) return null;

        var matchShard = GetMatchShard(inputLanguage);
        var templateShard = GetTemplateShard(targetLanguage);

        var normalised = DialogueText.NormaliseRenderedText(text);
        if (matchShard.TryExact(normalised, out var exactCandidates))
        {
            foreach (var entryId in exactCandidates)
            {
                var match = BuildMatch(templateShard, entryId, inputLanguage, targetLanguage, normalised, "Exact");
                if (match != null) return match;
            }
        }

        foreach (var entryId in matchShard.PatternCandidates(normalised))
        {
            var match = BuildMatch(templateShard, entryId, inputLanguage, targetLanguage, normalised, "Pattern");
            if (match != null) return match;
        }

        return null;
    }

    // shard cache
    // --------------------------------
    public void PrepareShards(DialogueShardRequirement requirement, CancellationToken cancellationToken)
    {
        foreach (var language in requirement.TemplateShards)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (language.IsReleaseSupported()) GetTemplateShard(language);
        }

        foreach (var language in requirement.MatchShards)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (language.IsReleaseSupported()) GetMatchShard(language);
        }
    }

    public bool HasPreparedShards(DialogueShardRequirement requirement)
    {
        lock (_shardLock)
        {
            return requirement.TemplateShards.All(_templateShards.ContainsKey)
                && requirement.MatchShards.All(_matchShards.ContainsKey);
        }
    }

    public void TrimTo(DialogueShardRequirement requirement)
    {
        lock (_shardLock)
        {
            foreach (var language in _templateShards.Keys.Except(requirement.TemplateShards).ToArray())
            {
                _templateShards.Remove(language);
            }

            foreach (var language in _matchShards.Keys.Except(requirement.MatchShards).ToArray())
            {
                _matchShards.Remove(language);
            }
        }
    }

    internal bool TryGetEntryText(
        string key,
        BttLanguage language,
        DialogueRenderProfile profile,
        out string text)
    {
        text = "";
        if (language is BttLanguage.Off or BttLanguage.Auto) return false;
        if (!language.IsReleaseSupported()) return false;
        if (!_common.TryGetEntryId(key, out var entryId)) return false;
        if (!_common.HasLanguage(entryId, language)) return false;

        var templateShard = GetTemplateShard(language);
        var template = templateShard.GetTemplate(entryId);
        if (template.IsEmpty) return false;

        text = template.Render(profile);
        return !string.IsNullOrWhiteSpace(text);
    }

    private DialogueMatch? BuildMatch(
        DialogueTemplateShard templateShard,
        int entryId,
        BttLanguage inputLanguage,
        BttLanguage targetLanguage,
        string sourceText,
        string matchKind)
    {
        if (!_common.HasLanguage(entryId, targetLanguage)) return null;

        var targetTemplate = templateShard.GetTemplate(entryId);
        if (targetTemplate.IsEmpty) return null;

        return new DialogueMatch
        {
            Key = _common.Keys[entryId],
            InputLanguage = inputLanguage,
            TargetLanguage = targetLanguage,
            SourceText = sourceText,
            TargetTemplate = targetTemplate,
            MatchKind = matchKind,
        };
    }

    private DialogueMatchShard GetMatchShard(BttLanguage inputLanguage)
    {
        lock (_shardLock)
        {
            if (_matchShards.TryGetValue(inputLanguage, out var loaded))
                return loaded;

            var matchFile = DialoguePackageLayout.RequiredShardPath(
                DialoguePackageStorage.InstalledZstd,
                DialoguePackageLayout.MatchRole,
                inputLanguage.ToPackageCode());
            var shard = DialogueMatchShard.Load(
                DialogueShardReader.ReadInstalledPackageFile(_packageDirectory, matchFile),
                _manifest.EntryCount);
            _matchShards[inputLanguage] = shard;
            Service.Log.Debug($"[DialogueStore] Loaded match shard for {inputLanguage.DisplayName()}");
            return shard;
        }
    }

    private DialogueTemplateShard GetTemplateShard(BttLanguage language)
    {
        lock (_shardLock)
        {
            if (_templateShards.TryGetValue(language, out var loaded))
                return loaded;

            var templateFile = DialoguePackageLayout.RequiredShardPath(
                DialoguePackageStorage.InstalledZstd,
                DialoguePackageLayout.TemplateRole,
                language.ToPackageCode());
            var shard = DialogueTemplateShard.Load(
                DialogueShardReader.ReadInstalledPackageFile(_packageDirectory, templateFile),
                _manifest.EntryCount);
            _templateShards[language] = shard;
            Service.Log.Debug($"[DialogueStore] Loaded template shard for {language.DisplayName()}");
            return shard;
        }
    }

    // package validation
    // --------------------------------
    private static string ResolveDirectoryToManifest(string path) => Directory.Exists(path)
        ? Path.Combine(path, DialoguePackageLayout.ManifestFileName)
        : path;

    private static void ValidateInstalledPackage(string packageDirectory) =>
        DialoguePackageLayout.ValidateExactFileSet(packageDirectory, DialoguePackageStorage.InstalledZstd);

    private static DialoguePackageManifest ReadManifest(string manifestPath)
    {
        var manifest = DialoguePackageJson.ReadPackageManifestFile(manifestPath);
        if (manifest.FormatVersion != DialoguePackageManifest.ExpectedFormatVersion)
            throw new InvalidOperationException($"Unsupported installed dialogue package format version: {manifest.FormatVersion}");

        return manifest;
    }
}
