namespace BilingualTooltips.Modules.Dialogue.Package;

public sealed record DialoguePackageInfo(
    string ManifestPath,
    int? BuildNumber,
    string? GameVersion,
    string? BuiltAt,
    int? FormatVersion,
    int? EntryCount,
    string Summary);
