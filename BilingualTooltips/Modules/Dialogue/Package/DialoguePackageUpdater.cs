using System.Formats.Tar;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using ZstdSharp;

namespace BilingualTooltips.Modules.Dialogue.Package;

public sealed class DialoguePackageUpdater
{
    private static readonly HttpClient HttpClient = new();

    public DialoguePackageUpdateProgress Progress { get; } = new();

    // public operations
    // --------------------------------
    public async Task<DialogueRemotePackageInfo> FetchRemotePackageInfoAsync(
        string versionManifestUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(versionManifestUrl))
            throw new InvalidOperationException("No package version manifest URL is configured.");

        var manifestJson = await DownloadTextAsync(
            new Uri(versionManifestUrl, UriKind.Absolute),
            DialoguePackageLayout.MaxVersionManifestBytes,
            "dialogue package version manifest",
            cancellationToken);
        var manifest = DialoguePackageVersionManifest.Parse(manifestJson, DialoguePackageJson.Options);
        return ToRemoteInfo(manifest);
    }

    public void CleanStagingPackage()
    {
        if (Directory.Exists(DialoguePaths.UserStagingPackageDirectory))
            Directory.Delete(DialoguePaths.UserStagingPackageDirectory, true);
    }

    public async Task<string> DownloadToActivePackageAsync(
        string versionManifestUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(versionManifestUrl))
            throw new InvalidOperationException("No package version manifest URL is configured.");

        Progress.Reset("Downloading version manifest");
        var manifestUri = new Uri(versionManifestUrl, UriKind.Absolute);
        var stagingDirectory = DialoguePaths.UserStagingPackageDirectory;
        PrepareEmptyDirectory(stagingDirectory);

        var manifestJson = await DownloadTextAsync(
            manifestUri,
            DialoguePackageLayout.MaxVersionManifestBytes,
            "dialogue package version manifest",
            cancellationToken);
        var versionManifest = DialoguePackageVersionManifest.Parse(manifestJson, DialoguePackageJson.Options);

        Progress.BuildNumber = versionManifest.BuildNumber;
        Progress.GameVersion = versionManifest.GameVersion;
        Progress.EntryCount = versionManifest.EntryCount;
        Progress.Status = "Downloading package";

        var packageName = ValidatePackageRelativePath(versionManifest.Package.Path);
        var packageUri = new Uri(manifestUri, packageName.Replace('\\', '/'));
        var packagePath = Path.Combine(stagingDirectory, "__download.tar.zst");
        await DownloadFileAsync(packageUri, packagePath, Progress, cancellationToken);

        Progress.Status = "Verifying package";
        VerifyFileHash(packagePath, versionManifest.Package.Sha256);

        Progress.Status = "Extracting package";
        ExtractArchive(packagePath, stagingDirectory, cancellationToken);
        File.Delete(packagePath);

        Progress.Status = "Preparing package";
        var packageManifestPath = Path.Combine(stagingDirectory, DialoguePackageLayout.ManifestFileName);
        var packageManifest = ReadPackageManifest(packageManifestPath);
        VerifyVersionMatchesPackage(versionManifest, packageManifest);
        DialoguePackageLayout.ValidateExactFileSet(stagingDirectory, DialoguePackageStorage.ArchiveRaw);
        ConvertPackageToInstalledStorage(stagingDirectory);
        DialoguePackageLayout.ValidateExactFileSet(stagingDirectory, DialoguePackageStorage.InstalledZstd);

        Progress.Status = "Activating package";
        ActivateStagedPackage(stagingDirectory);
        Progress.Status = "Installed";
        return $"Installed dialogue package with {packageManifest.EntryCount:N0} entries.";
    }

    // download helpers
    // --------------------------------
    private static DialogueRemotePackageInfo ToRemoteInfo(DialoguePackageVersionManifest manifest) =>
        new(
            manifest.BuildNumber,
            manifest.GameVersion,
            manifest.BuiltAt,
            manifest.EntryCount,
            manifest.Package.Path,
            manifest.Package.Sha256);

    private static async Task DownloadFileAsync(
        Uri uri,
        string path,
        DialoguePackageUpdateProgress progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        ValidateContentLength(
            response,
            DialoguePackageLayout.MaxDownloadedPackageBytes,
            "dialogue package download");
        progress.TotalBytes = response.Content.Headers.ContentLength;

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(path);
        await CopyWithProgressAsync(
            responseStream,
            fileStream,
            progress,
            DialoguePackageLayout.MaxDownloadedPackageBytes,
            cancellationToken);
    }

    private static async Task<string> DownloadTextAsync(
        Uri uri,
        long maxBytes,
        string name,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        ValidateContentLength(response, maxBytes, name);

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        await CopyWithLimitAsync(responseStream, output, maxBytes, name, cancellationToken);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static async Task CopyWithProgressAsync(
        Stream source,
        Stream destination,
        DialoguePackageUpdateProgress progress,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 128];
        long totalBytes = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;

            totalBytes += read;
            if (totalBytes > maxBytes)
                throw new InvalidOperationException($"Dialogue package download exceeds the hard limit of {DialoguePackageLayout.FormatBytes(maxBytes)}.");

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            progress.BytesDownloaded = totalBytes;
        }
    }

    private static async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        string name,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 32];
        long totalBytes = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;

            totalBytes += read;
            if (totalBytes > maxBytes)
                throw new InvalidOperationException($"{name} exceeds the hard limit of {DialoguePackageLayout.FormatBytes(maxBytes)}.");

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    // archive extraction
    // --------------------------------
    private static void ExtractArchive(string archivePath, string stagingDirectory, CancellationToken cancellationToken)
    {
        using var archive = File.OpenRead(archivePath);
        using var zstd = new DecompressionStream(archive, bufferSize: 0, checkEndOfStream: true, leaveOpen: false);
        using var tarReader = new TarReader(zstd, leaveOpen: false);
        var expectedPaths = DialoguePackageLayout.ExpectedFiles(DialoguePackageStorage.ArchiveRaw)
            .ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalExtractedBytes = 0;
        var totalEntries = 0;

        while (tarReader.GetNextEntry() is { } entry)
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalEntries++;
            if (totalEntries > DialoguePackageLayout.MaxArchiveEntryCount)
                throw new InvalidOperationException($"Dialogue package archive contains more than {DialoguePackageLayout.MaxArchiveEntryCount} entries.");

            if (entry.EntryType is TarEntryType.Directory) continue;
            if (entry.EntryType is not TarEntryType.RegularFile and not TarEntryType.V7RegularFile)
                throw new InvalidOperationException($"Unsupported dialogue package tar entry type: {entry.EntryType}");

            var relativePath = ValidatePackageRelativePath(entry.Name.Replace('\\', '/'));
            if (string.Equals(relativePath, "__download.tar.zst", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dialogue package archive contains reserved staging file name.");
            if (!expectedPaths.Contains(relativePath))
                throw new InvalidOperationException($"Dialogue package archive contains undeclared file: {relativePath}");

            if (!seen.Add(relativePath))
                throw new InvalidOperationException($"Dialogue package archive contains duplicate file: {relativePath}");

            var targetPath = DialoguePackageLayout.ResolvePackageFile(stagingDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? stagingDirectory);
            using var target = File.Create(targetPath);
            if (entry.DataStream == null)
                throw new InvalidOperationException($"Dialogue package tar entry has no data stream: {relativePath}");

            CopyEntryData(entry.DataStream, target, relativePath, ref totalExtractedBytes, cancellationToken);
        }
    }

    private static void CopyEntryData(
        Stream source,
        Stream destination,
        string relativePath,
        ref long totalExtractedBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 128];
        long fileBytes = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = source.Read(buffer, 0, buffer.Length);
            if (read == 0) break;

            fileBytes += read;
            if (fileBytes > DialoguePackageLayout.MaxPackageShardBytes)
                throw new InvalidOperationException($"Dialogue package file exceeds the hard limit of {DialoguePackageLayout.FormatBytes(DialoguePackageLayout.MaxPackageShardBytes)}: {relativePath}");

            totalExtractedBytes += read;
            if (totalExtractedBytes > DialoguePackageLayout.MaxExtractedPackageBytes)
                throw new InvalidOperationException($"Dialogue package extraction exceeds the hard limit of {DialoguePackageLayout.FormatBytes(DialoguePackageLayout.MaxExtractedPackageBytes)}.");

            destination.Write(buffer, 0, read);
        }
    }

    // package validation and conversion
    // --------------------------------
    private static DialoguePackageManifest ReadPackageManifest(string manifestPath) =>
        DialoguePackageJson.ReadPackageManifestFile(manifestPath);

    private static void VerifyVersionMatchesPackage(
        DialoguePackageVersionManifest versionManifest,
        DialoguePackageManifest packageManifest)
    {
        if (packageManifest.BuildNumber != versionManifest.BuildNumber)
            throw new InvalidOperationException("Dialogue package build number does not match latest.json.");
        if (!string.Equals(packageManifest.GameVersion, versionManifest.GameVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Dialogue package game version does not match latest.json.");
        if (packageManifest.EntryCount != versionManifest.EntryCount)
            throw new InvalidOperationException("Dialogue package entry count does not match latest.json.");
        if (!string.Equals(packageManifest.BuiltAt, versionManifest.BuiltAt, StringComparison.Ordinal))
            throw new InvalidOperationException("Dialogue package build timestamp does not match latest.json.");
    }

    private static void ConvertPackageToInstalledStorage(string directory)
    {
        using var compressor = new Compressor(DialoguePackageLayout.InstalledCompressionLevel);
        foreach (var shard in DialoguePackageLayout.ExpectedShards(DialoguePackageStorage.ArchiveRaw))
        {
            var rawPath = DialoguePackageLayout.ResolvePackageFile(directory, shard.RelativePath);
            var rawBytes = File.ReadAllBytes(rawPath);
            var compressedBytes = compressor.Wrap(rawBytes).ToArray();
            var compressedRelativePath = shard.RelativePath + ".zst";
            var compressedPath = DialoguePackageLayout.ResolvePackageFile(directory, compressedRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(compressedPath) ?? directory);
            File.WriteAllBytes(compressedPath, compressedBytes);
            File.Delete(rawPath);
        }
    }

    private static void VerifyFileHash(string path, string expectedSha256)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Dialogue package file was not found.", path);

        var actualHash = Sha256File(path);
        if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Dialogue package file hash mismatch for {path}. Expected {expectedSha256}, got {actualHash}.");
    }

    private static void ValidateContentLength(HttpResponseMessage response, long maxBytes, string name)
    {
        if (response.Content.Headers.ContentLength is { } contentLength && contentLength > maxBytes)
            throw new InvalidOperationException($"{name} declares {DialoguePackageLayout.FormatBytes(contentLength)}, exceeding the hard limit of {DialoguePackageLayout.FormatBytes(maxBytes)}.");
    }

    // filesystem helpers
    // --------------------------------
    private static void PrepareEmptyDirectory(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);

        Directory.CreateDirectory(directory);
    }

    private static string ValidatePackageRelativePath(string value)
    {
        DialogueManifestValidation.ValidateRelativePath(value, "Dialogue package path");
        return value.Replace('\\', '/');
    }

    private static void ActivateStagedPackage(string stagingDirectory)
    {
        var activeDirectory = DialoguePaths.UserActivePackageDirectory;
        var previousDirectory = DialoguePaths.UserPreviousPackageDirectory;

        if (Directory.Exists(previousDirectory)) Directory.Delete(previousDirectory, true);

        var movedActive = false;
        if (Directory.Exists(activeDirectory))
        {
            Directory.Move(activeDirectory, previousDirectory);
            movedActive = true;
        }

        try
        {
            Directory.Move(stagingDirectory, activeDirectory);
            if (Directory.Exists(previousDirectory)) Directory.Delete(previousDirectory, true);
        }
        catch
        {
            if (movedActive && Directory.Exists(previousDirectory) && !Directory.Exists(activeDirectory))
            {
                Directory.Move(previousDirectory, activeDirectory);
            }

            throw;
        }
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
