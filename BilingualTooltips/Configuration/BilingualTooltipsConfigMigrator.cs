using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dalamud.Game.ClientState.Keys;
using Miosuke.Configuration;
using Miosuke.Messages;

namespace BilingualTooltips.Configuration;

internal static class BilingualTooltipsConfigMigrator
{
    public static void MigrateIfNeeded(string mainConfigPath)
    {
        if (!File.Exists(mainConfigPath)) return;

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(mainConfigPath)) as JsonObject
                ?? throw new InvalidDataException($"Configuration file {mainConfigPath} is not a JSON object.");

            var version = ReadInt(root, "Version") ?? 0;
            if (version >= BilingualTooltipsConfig.CurrentVersion) return;

            var config = version switch
            {
                0 => BilingualTooltipsConfigMigrationV0ToV1.Migrate(root),
                _ => throw new InvalidDataException($"Unsupported BilingualTooltips config version {version}."),
            };

            WriteConfig(mainConfigPath, config);
            Service.Log.Info($"Migrated BilingualTooltips configuration from version {version} to {config.Version}.");
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Failed to migrate BilingualTooltips configuration.");
            Notice.Error("Failed to migrate BilingualTooltips configuration. Check /xllog for details.");
        }
    }

    private static void WriteConfig(string path, BilingualTooltipsConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = $"{path}.new";
        File.WriteAllBytes(tempPath, MioConfig.ConfigIo.Serialize(config));
        File.Move(tempPath, path, true);
    }

    public static int? ReadInt(JsonObject root, string name) =>
        root[name] == null ? null : root[name]!.GetValue<int>();

    public static void CopyInt(JsonObject root, string name, Action<int> assign)
    {
        var value = ReadInt(root, name);
        if (value != null) assign(value.Value);
    }

    public static void CopyBool(JsonObject root, string name, Action<bool> assign)
    {
        if (root[name] == null) return;
        assign(root[name]!.GetValue<bool>());
    }

    public static void CopyHotkey(JsonObject root, string name, Action<VirtualKey[]> assign)
    {
        var value = root[name]?.Deserialize<VirtualKey[]>();
        if (value is { Length: > 0 }) assign(value);
    }
}
