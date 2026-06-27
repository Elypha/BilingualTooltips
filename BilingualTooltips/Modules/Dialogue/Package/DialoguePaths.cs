using System.IO;

namespace BilingualTooltips.Modules.Dialogue.Package;

internal static class DialoguePaths
{
    public const string PackageDirectoryName = "DialoguePackage";
    public static string UserPackageRootDirectory => Path.Combine(Service.PluginInterface.GetPluginConfigDirectory(), PackageDirectoryName);
    public static string UserActivePackageDirectory => Path.Combine(UserPackageRootDirectory, "active");
    public static string UserPreviousPackageDirectory => Path.Combine(UserPackageRootDirectory, "previous");
    public static string UserStagingPackageDirectory => Path.Combine(UserPackageRootDirectory, "staging");
    public static string UserActivePackageManifestPath => Path.Combine(UserActivePackageDirectory, DialoguePackageLayout.ManifestFileName);
}
