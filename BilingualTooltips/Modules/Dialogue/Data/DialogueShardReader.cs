using System.IO;
using BilingualTooltips.Modules.Dialogue.Package;
using ZstdSharp;

namespace BilingualTooltips.Modules.Dialogue.Data;

internal static class DialogueShardReader
{
    public static byte[] ReadInstalledPackageFile(string packageDirectory, string relativePath)
    {
        var path = DialoguePackageLayout.ResolvePackageFile(packageDirectory, relativePath);
        using var input = File.OpenRead(path);
        using var zstd = new DecompressionStream(input, bufferSize: 0, checkEndOfStream: true, leaveOpen: false);
        using var output = new MemoryStream();
        CopyToMemoryWithLimit(zstd, output, relativePath);
        return output.ToArray();
    }

    private static void CopyToMemoryWithLimit(Stream source, MemoryStream destination, string relativePath)
    {
        var buffer = new byte[1024 * 128];
        long totalBytes = 0;
        while (true)
        {
            var read = source.Read(buffer, 0, buffer.Length);
            if (read == 0) break;

            totalBytes += read;
            if (totalBytes > DialoguePackageLayout.MaxPackageShardBytes)
                throw new InvalidOperationException($"Dialogue package shard exceeds the hard limit of {DialoguePackageLayout.MaxPackageShardBytes:N0} bytes: {relativePath}");

            destination.Write(buffer, 0, read);
        }
    }
}
