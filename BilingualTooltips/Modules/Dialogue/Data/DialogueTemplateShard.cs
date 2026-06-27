using static BilingualTooltips.Modules.Dialogue.Data.DialogueBinaryPrimitives;

namespace BilingualTooltips.Modules.Dialogue.Data;

internal sealed class DialogueTemplateShard
{
    private readonly byte[] _data;
    private readonly int[] _offsets;
    private readonly int _templateBytesOffset;

    public int EntryCount => _offsets.Length - 1;

    private DialogueTemplateShard(byte[] data, int[] offsets, int templateBytesOffset)
    {
        _data = data;
        _offsets = offsets;
        _templateBytesOffset = templateBytesOffset;
    }

    public static DialogueTemplateShard Load(byte[] data, int expectedEntryCount)
    {
        ValidateShardHeader(data, "BTT-PKG-TEMPLATE", 56);
        var entryCount = ReadU32(data, 24);
        if (entryCount != expectedEntryCount)
            throw new InvalidOperationException($"Template shard count mismatch. Expected {expectedEntryCount}, got {entryCount}.");

        var offsetsOffset = CheckedOffset(data, ReadU64(data, 32));
        var templateBytesOffset = CheckedOffset(data, ReadU64(data, 40));
        var templateBytesLength = CheckedOffset(data, ReadU64(data, 48));
        ValidateRange(data, offsetsOffset, checked((entryCount + 1) * 8), "template shard offsets");
        ValidateRange(data, templateBytesOffset, templateBytesLength, "template shard bytes");

        var offsets = new int[entryCount + 1];
        for (var index = 0; index <= entryCount; index++)
        {
            var value = ReadU64(data, offsetsOffset + index * 8);
            if (value > int.MaxValue)
                throw new InvalidOperationException("Template shard offset is too large.");
            offsets[index] = (int)value;
            if (index > 0 && offsets[index] < offsets[index - 1])
                throw new InvalidOperationException("Template shard offsets are not ordered.");
        }

        if (offsets[^1] != templateBytesLength)
            throw new InvalidOperationException("Template shard offset table does not match the template section length.");

        return new DialogueTemplateShard(data, offsets, templateBytesOffset);
    }

    public DialogueTemplate GetTemplate(int entryId)
    {
        if ((uint)entryId >= (uint)EntryCount)
            throw new InvalidOperationException("Template shard entry id is out of range.");

        var start = _offsets[entryId];
        var end = _offsets[entryId + 1];
        return start == end
            ? DialogueTemplate.Empty
            : DialogueTemplate.Read(_data.AsSpan(_templateBytesOffset + start, end - start));
    }
}
