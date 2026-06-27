using System.IO;

namespace BilingualTooltips.Modules.Dialogue.Package;

internal enum DialoguePackageStorage
{
    ArchiveRaw,
    InstalledZstd,
}

internal readonly record struct DialoguePackageShardFile(string Role, string? Language, string RelativePath);

internal static class DialoguePackageLayout
{
    public const string ManifestFileName = "manifest.json";
    public const string EntriesRole = "entries";
    public const string TemplateRole = "template";
    public const string MatchRole = "match";
    public const int InstalledCompressionLevel = 1;
    public const long MaxVersionManifestBytes = 1L * 1024 * 1024;
    public const long MaxPackageManifestBytes = 1L * 1024 * 1024;
    public const long MaxDownloadedPackageBytes = 1024L * 1024 * 1024;
    public const long MaxExtractedPackageBytes = 2L * 1024 * 1024 * 1024;
    public const long MaxPackageShardBytes = 512L * 1024 * 1024;
    public const int MaxArchiveEntryCount = 256;

    public static string PackageArchivePath(string gameVersion, int buildNumber) => $"packages/{gameVersion}-build-{buildNumber}.tar.zst";

    public static string FormatBytes(long bytes) => $"{bytes:N0} bytes";

    public static IEnumerable<string> ExpectedFiles(DialoguePackageStorage storage)
    {
        yield return ManifestFileName;
        foreach (var shard in ExpectedShards(storage))
        {
            yield return shard.RelativePath;
        }
    }

    public static IEnumerable<DialoguePackageShardFile> ExpectedShards(DialoguePackageStorage storage)
    {
        var suffix = storage == DialoguePackageStorage.InstalledZstd ? ".zst" : "";
        yield return new DialoguePackageShardFile(EntriesRole, null, $"dialogue/entries.bttbin{suffix}");

        foreach (var packageLanguage in BttLanguageExtensions.PackageLanguages)
        {
            var language = packageLanguage.ToPackageCode();
            if (string.IsNullOrWhiteSpace(language))
                throw new InvalidOperationException($"Package language has no package code: {packageLanguage}");

            yield return new DialoguePackageShardFile(
                TemplateRole,
                language,
                $"dialogue/template/{language}.bttbin{suffix}");
            yield return new DialoguePackageShardFile(
                MatchRole,
                language,
                $"dialogue/match/{language}.bttbin{suffix}");
        }
    }

    public static string RequiredShardPath(
        DialoguePackageStorage storage,
        string role,
        string? language = null)
    {
        foreach (var shard in ExpectedShards(storage))
        {
            if (string.Equals(shard.Role, role, StringComparison.Ordinal) && string.Equals(shard.Language, language, StringComparison.Ordinal))
                return shard.RelativePath;
        }

        throw new InvalidOperationException($"Dialogue package is missing required shard: {role}/{language}.");
    }

    public static string ResolvePackageFile(string packageDirectory, string relativePath)
    {
        DialogueManifestValidation.ValidateRelativePath(relativePath, "Dialogue package file path");

        var root = Path.GetFullPath(packageDirectory);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Dialogue package file path escapes the package directory.");

        return fullPath;
    }

    public static void ValidateExactFileSet(string packageDirectory, DialoguePackageStorage storage)
    {
        var expectedPaths = ExpectedFiles(storage).ToHashSet(StringComparer.Ordinal);
        var actualPaths = Directory.EnumerateFiles(packageDirectory, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(packageDirectory, file).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var relativePath in expectedPaths)
        {
            if (actualPaths.Contains(relativePath)) continue;
            throw new FileNotFoundException("Dialogue package file was not found.", ResolvePackageFile(packageDirectory, relativePath));
        }

        foreach (var relativePath in actualPaths)
        {
            if (expectedPaths.Contains(relativePath)) continue;
            throw new InvalidOperationException($"Dialogue package contains undeclared file: {relativePath}");
        }
    }
}
