using System.Text.Json;

namespace BilingualTooltips.Modules.Dialogue.Package;

internal sealed class DialoguePackageVersionManifest
{
    public const string ExpectedFormat = "btt-version";
    public const int ExpectedFormatVersion = 1;

    public string Format { get; set; } = "";
    public int FormatVersion { get; set; }
    public int BuildNumber { get; set; }
    public string GameVersion { get; set; } = "";
    public int EntryCount { get; set; }
    public string BuiltAt { get; set; } = "";
    public DialoguePackageFileManifest Package { get; set; } = null!;

    public static DialoguePackageVersionManifest Parse(string json, JsonSerializerOptions options)
    {
        var manifest = JsonSerializer.Deserialize<DialoguePackageVersionManifest>(json, options)
            ?? throw new InvalidOperationException("Dialogue package version manifest could not be parsed.");
        manifest.Validate();
        return manifest;
    }

    private void Validate()
    {
        DialogueManifestValidation.ValidateHeader(Format, ExpectedFormat);
        if (FormatVersion != ExpectedFormatVersion)
            throw new InvalidOperationException($"Unsupported dialogue package version format version: {FormatVersion}");

        if (BuildNumber <= 0)
            throw new InvalidOperationException("Dialogue package build number must be a positive integer.");
        DialogueManifestValidation.ValidateGameVersion(GameVersion);
        if (EntryCount <= 0)
            throw new InvalidOperationException("Dialogue package entry count must be positive.");
        DialogueManifestValidation.ValidateBuiltAt(BuiltAt);
        if (Package == null)
            throw new InvalidOperationException("Dialogue package archive identity is missing.");
        Package.Validate();

        var expectedPath = DialoguePackageLayout.PackageArchivePath(GameVersion, BuildNumber);
        if (!string.Equals(Package.Path, expectedPath, StringComparison.Ordinal))
            throw new InvalidOperationException($"Dialogue package archive path must be {expectedPath}, got {Package.Path}.");
    }
}

internal sealed class DialoguePackageFileManifest
{
    public string Path { get; set; } = "";
    public string Sha256 { get; set; } = "";

    public void Validate()
    {
        DialogueManifestValidation.ValidateRelativePath(Path, "Dialogue package archive path");
        DialogueManifestValidation.ValidateSha256(Sha256, "Dialogue package archive hash");
    }
}
