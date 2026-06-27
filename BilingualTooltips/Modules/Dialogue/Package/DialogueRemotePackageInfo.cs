namespace BilingualTooltips.Modules.Dialogue.Package;

public sealed record DialogueRemotePackageInfo(
    int BuildNumber,
    string GameVersion,
    string BuiltAt,
    int EntryCount,
    string PackagePath,
    string PackageSha256);
