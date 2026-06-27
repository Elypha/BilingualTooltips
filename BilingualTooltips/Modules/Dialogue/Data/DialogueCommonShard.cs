using static BilingualTooltips.Modules.Dialogue.Data.DialogueBinaryPrimitives;

namespace BilingualTooltips.Modules.Dialogue.Data;

internal sealed class DialogueCommonShard
{
    private readonly Lock _entryIdIndexLock = new();
    private Dictionary<string, int>? _entryIdsByKey;

    public int EntryCount => Keys.Length;
    public string[] Keys { get; }
    public byte[] LanguagePresenceMasks { get; }

    private DialogueCommonShard(string[] keys, byte[] languagePresenceMasks)
    {
        Keys = keys;
        LanguagePresenceMasks = languagePresenceMasks;
    }

    public static DialogueCommonShard Load(byte[] data, int expectedEntryCount)
    {
        ValidateShardHeader(data, "BTT-PKG-ENTRIES", 72);
        var entryCount = ReadU32(data, 24);
        if (entryCount != expectedEntryCount)
            throw new InvalidOperationException($"Entry shard count mismatch. Expected {expectedEntryCount}, got {entryCount}.");

        var stringCount = ReadU32(data, 28);
        var rowSize = ReadU32(data, 32);
        if (rowSize != 17)
            throw new InvalidOperationException($"Unsupported entry shard row size: {rowSize}");

        var entriesOffset = CheckedOffset(data, ReadU64(data, 40));
        var stringOffsetsOffset = CheckedOffset(data, ReadU64(data, 48));
        var stringBytesOffset = CheckedOffset(data, ReadU64(data, 56));
        var stringBytesLength = CheckedOffset(data, ReadU64(data, 64));
        ValidateRange(data, entriesOffset, checked(entryCount * 17), "entry shard rows");
        ValidateRange(data, stringOffsetsOffset, checked((stringCount + 1) * 8), "entry shard string offsets");
        var strings = ReadStringPool(data, stringCount, stringOffsetsOffset, stringBytesOffset, stringBytesLength);

        var keys = new string[entryCount];
        var masks = new byte[entryCount];
        for (var index = 0; index < entryCount; index++)
        {
            var rowOffset = entriesOffset + index * 17;
            var keyId = ReadU32(data, rowOffset);
            keys[index] = ReadStringById(strings, keyId, "entry key");
            masks[index] = data[rowOffset + 16];
        }

        return new DialogueCommonShard(keys, masks);
    }

    public bool HasLanguage(int entryId, BttLanguage language)
    {
        var languageIndex = Array.IndexOf(BttLanguageExtensions.PackageLanguages, language);
        return languageIndex >= 0 && (LanguagePresenceMasks[entryId] & (1 << languageIndex)) != 0;
    }

    public bool TryGetEntryId(string key, out int entryId)
    {
        var index = _entryIdsByKey;
        if (index == null)
        {
            lock (_entryIdIndexLock)
            {
                index = _entryIdsByKey ??= BuildEntryIdIndex(Keys);
            }
        }

        return index.TryGetValue(key, out entryId);
    }

    private static Dictionary<string, int> BuildEntryIdIndex(string[] keys)
    {
        var index = new Dictionary<string, int>(keys.Length, StringComparer.Ordinal);
        for (var entryId = 0; entryId < keys.Length; entryId++)
        {
            index[keys[entryId]] = entryId;
        }

        return index;
    }
}
