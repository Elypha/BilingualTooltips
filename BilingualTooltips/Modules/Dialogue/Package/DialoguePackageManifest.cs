using System.Text.Json;

namespace BilingualTooltips.Modules.Dialogue.Package;

internal sealed class DialoguePackageManifest
{
    public const string ExpectedFormat = "btt-package";
    public const int ExpectedFormatVersion = 1;

    public string Format { get; set; } = "";
    public int FormatVersion { get; set; }
    public int BuildNumber { get; set; }
    public string GameVersion { get; set; } = "";
    public int EntryCount { get; set; }
    public string BuiltAt { get; set; } = "";
    public DialoguePackageComponentsManifest Components { get; set; } = null!;

    public static DialoguePackageManifest Parse(string json, JsonSerializerOptions options)
    {
        var manifest = JsonSerializer.Deserialize<DialoguePackageManifest>(json, options)
            ?? throw new InvalidOperationException("Dialogue package manifest could not be parsed.");
        manifest.Validate();
        return manifest;
    }

    private void Validate()
    {
        DialogueManifestValidation.ValidateHeader(Format, ExpectedFormat);
        if (FormatVersion != ExpectedFormatVersion)
            throw new InvalidOperationException($"Unsupported dialogue package format version: {FormatVersion}");
        if (BuildNumber <= 0)
            throw new InvalidOperationException("Dialogue package build number must be a positive integer.");
        DialogueManifestValidation.ValidateGameVersion(GameVersion);
        if (EntryCount <= 0)
            throw new InvalidOperationException("Dialogue package entry count must be positive.");
        DialogueManifestValidation.ValidateBuiltAt(BuiltAt);
        if (Components == null)
            throw new InvalidOperationException("Dialogue package component list is missing.");
        Components.Validate();
    }
}

internal sealed class DialoguePackageComponentsManifest
{
    public DialoguePackageDialogueComponentManifest Dialogue { get; set; } = null!;

    public void Validate()
    {
        if (Dialogue == null)
            throw new InvalidOperationException("Dialogue package dialogue component is missing.");
        Dialogue.Validate();
    }
}

internal sealed class DialoguePackageDialogueComponentManifest
{
    public void Validate()
    {
    }
}
