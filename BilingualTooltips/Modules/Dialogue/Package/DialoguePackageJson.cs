using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilingualTooltips.Modules.Dialogue.Package;

internal static class DialoguePackageJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static DialoguePackageManifest ReadPackageManifestFile(string manifestPath)
    {
        var manifestJson = ReadTextFileWithLimit(
            manifestPath,
            DialoguePackageLayout.MaxPackageManifestBytes,
            "dialogue package manifest");
        return DialoguePackageManifest.Parse(manifestJson, Options);
    }

    private static string ReadTextFileWithLimit(string path, long maxBytes, string name)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{name} was not found.", path);

        var length = new FileInfo(path).Length;
        if (length > maxBytes)
            throw new InvalidOperationException($"{name} exceeds the hard limit of {DialoguePackageLayout.FormatBytes(maxBytes)}: {path}");

        return File.ReadAllText(path);
    }
}
