using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BilingualTooltips.Modules.Dialogue.Package;

internal static class DialogueManifestValidation
{
    private static readonly Regex GameVersionPattern = new(
        "^[0-9A-Za-z.-]+$",
        RegexOptions.CultureInvariant);

    public static void ValidateHeader(string format, string expectedFormat)
    {
        if (!string.Equals(format, expectedFormat, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported dialogue package format: {format}");
    }

    public static void ValidateRelativePath(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is empty.");
        if (Path.IsPathRooted(value) || Uri.TryCreate(value, UriKind.Absolute, out _))
            throw new InvalidOperationException($"{name} must be relative.");

        var normalised = value.Replace('\\', '/');
        if (normalised.Split('/').Any(static part => part is "" or "." or ".."))
            throw new InvalidOperationException($"{name} contains an unsafe path segment: {value}");
    }

    public static void ValidateSha256(string value, string name)
    {
        if (value.Length != 64
            || !value.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
            throw new InvalidOperationException($"{name} must be a SHA-256 hex string.");
    }

    public static void ValidateGameVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Dialogue package game version is missing.");
        if (!GameVersionPattern.IsMatch(value))
            throw new InvalidOperationException($"Dialogue package game version is not safe for package file names: {value}");
    }

    public static void ValidateBuiltAt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Dialogue package build timestamp is missing.");
        if (!DateTime.TryParseExact(
                value,
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _))
        {
            throw new InvalidOperationException($"Dialogue package build timestamp must be UTC ISO-8601 with milliseconds: {value}");
        }
    }
}
