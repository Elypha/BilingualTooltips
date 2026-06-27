using System.Buffers.Binary;

namespace BilingualTooltips.Modules.Dialogue.Data;

internal static class DialogueBinaryPrimitives
{
    private const int ShardMagicBytes = 16;
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static void ValidateShardHeader(byte[] data, string expectedMagic, int expectedHeaderSize)
    {
        if (data.Length < expectedHeaderSize)
            throw new InvalidOperationException("Dialogue binary shard is too small.");

        var magic = Encoding.ASCII.GetString(data, 0, ShardMagicBytes).TrimEnd('\0');
        if (!string.Equals(magic, expectedMagic, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unexpected dialogue binary shard magic: {magic}");

        var schemaVersion = ReadU16(data, ShardMagicBytes);
        if (schemaVersion != 1)
            throw new InvalidOperationException($"Unsupported dialogue binary shard schema version: {schemaVersion}");

        var headerSize = ReadU16(data, ShardMagicBytes + 2);
        if (headerSize != expectedHeaderSize)
            throw new InvalidOperationException($"Unsupported dialogue binary shard header size: {headerSize}");
    }

    public static string[] ReadStringPool(byte[] data, int count, int offsetsOffset, int bytesOffset, int bytesLength)
    {
        ValidateRange(data, bytesOffset, bytesLength, "dialogue string pool bytes");
        ValidateRange(data, offsetsOffset, checked((count + 1) * 8), "dialogue string pool offsets");

        var offsets = new int[count + 1];
        for (var index = 0; index <= count; index++)
        {
            var value = ReadU64(data, offsetsOffset + index * 8);
            if (value > int.MaxValue)
                throw new InvalidOperationException("Dialogue string pool offset is too large.");
            offsets[index] = (int)value;
            if (index > 0 && offsets[index] < offsets[index - 1])
                throw new InvalidOperationException("Dialogue string pool offsets are not ordered.");
        }

        if (offsets[^1] != bytesLength)
            throw new InvalidOperationException("Dialogue string pool offset table does not match the string byte length.");

        var output = new string[count];
        for (var index = 0; index < count; index++)
        {
            var start = offsets[index];
            var end = offsets[index + 1];
            output[index] = StrictUtf8.GetString(data, bytesOffset + start, end - start);
        }

        return output;
    }

    public static string ReadStringById(IReadOnlyList<string> strings, int stringId, string name)
    {
        if ((uint)stringId >= (uint)strings.Count)
            throw new InvalidOperationException($"Dialogue {name} string id is out of range.");

        return strings[stringId];
    }

    public static int CheckedOffset(byte[] data, ulong value)
    {
        if (value > (ulong)data.Length)
            throw new InvalidOperationException("Dialogue binary shard offset escapes the file.");

        return (int)value;
    }

    public static void ValidateRange(byte[] data, int offset, int length, string name)
    {
        if ((uint)offset > data.Length || (uint)length > data.Length - offset)
            throw new InvalidOperationException($"{name} escapes the dialogue binary shard.");
    }

    public static ushort ReadU16(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));

    public static int ReadU32(byte[] data, int offset) =>
        checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4)));

    public static ulong ReadU64(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset, 8));
}
