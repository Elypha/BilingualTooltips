using static BilingualTooltips.Modules.Dialogue.Data.DialogueBinaryPrimitives;

namespace BilingualTooltips.Modules.Dialogue.Data;

internal sealed class DialogueMatchShard
{
    private sealed record PatternCandidate(int EntryId, IReadOnlyList<string> Tokens);

    private readonly Dictionary<string, int[]> _exactIndex;
    private readonly List<PatternCandidate> _patterns;

    private DialogueMatchShard(Dictionary<string, int[]> exactIndex, List<PatternCandidate> patterns)
    {
        _exactIndex = exactIndex;
        _patterns = patterns;
    }

    public static DialogueMatchShard Load(byte[] data, int expectedEntryCount)
    {
        ValidateShardHeader(data, "BTT-PKG-MATCHIDX", 96);
        var entryCount = ReadU32(data, 24);
        if (entryCount != expectedEntryCount)
            throw new InvalidOperationException($"Match shard count mismatch. Expected {expectedEntryCount}, got {entryCount}.");

        var exactCount = ReadU32(data, 28);
        var exactCandidateCount = ReadU32(data, 32);
        var patternCount = ReadU32(data, 36);
        var exactRowsOffset = CheckedOffset(data, ReadU64(data, 40));
        var exactCandidatesOffset = CheckedOffset(data, ReadU64(data, 48));
        var patternRowsOffset = CheckedOffset(data, ReadU64(data, 56));
        var patternTokensOffset = CheckedOffset(data, ReadU64(data, 64));
        var patternTokenCount = ReadU32(data, 72);
        var stringCount = ReadU32(data, 76);
        var stringOffsetsOffset = CheckedOffset(data, ReadU64(data, 80));
        var stringBytesOffset = CheckedOffset(data, ReadU64(data, 88));
        ValidateRange(data, exactRowsOffset, checked(exactCount * 12), "match shard exact rows");
        ValidateRange(data, exactCandidatesOffset, checked(exactCandidateCount * 4), "match shard exact candidates");
        ValidateRange(data, patternRowsOffset, checked(patternCount * 16), "match shard pattern rows");
        ValidateRange(data, patternTokensOffset, checked(patternTokenCount * 4), "match shard pattern tokens");
        ValidateRange(data, stringOffsetsOffset, checked((stringCount + 1) * 8), "match shard string offsets");
        var strings = ReadStringPool(
            data,
            stringCount,
            stringOffsetsOffset,
            stringBytesOffset,
            data.Length - stringBytesOffset);

        var exactCandidates = ReadExactCandidates(data, exactCandidatesOffset, exactCandidateCount, expectedEntryCount);
        var exactIndex = ReadExactIndex(data, exactRowsOffset, exactCount, exactCandidates, strings);
        var patternTokens = ReadPatternTokens(data, patternTokensOffset, patternTokenCount, strings);
        var patterns = ReadPatterns(data, patternRowsOffset, patternCount, patternTokens, strings, expectedEntryCount);

        return new DialogueMatchShard(exactIndex, patterns);
    }

    public bool TryExact(string text, out int[] candidates) =>
        _exactIndex.TryGetValue(text, out candidates!);

    public IEnumerable<int> PatternCandidates(string text) =>
        from pattern in _patterns where DialogueText.OrderedTokenMatch(text, pattern.Tokens) select pattern.EntryId;

    private static int[] ReadExactCandidates(
        byte[] data,
        int exactCandidatesOffset,
        int exactCandidateCount,
        int expectedEntryCount)
    {
        var exactCandidates = new int[exactCandidateCount];
        for (var index = 0; index < exactCandidates.Length; index++)
        {
            var entryId = ReadU32(data, exactCandidatesOffset + index * 4);
            if (entryId >= expectedEntryCount)
                throw new InvalidOperationException("Match shard exact candidate entry id is out of range.");

            exactCandidates[index] = entryId;
        }

        return exactCandidates;
    }

    private static Dictionary<string, int[]> ReadExactIndex(
        byte[] data,
        int exactRowsOffset,
        int exactCount,
        int[] exactCandidates,
        string[] strings)
    {
        var exactIndex = new Dictionary<string, int[]>(StringComparer.Ordinal);
        for (var index = 0; index < exactCount; index++)
        {
            var offset = exactRowsOffset + index * 12;
            var text = ReadStringById(strings, ReadU32(data, offset), "match exact text");
            var start = ReadU32(data, offset + 4);
            var count = ReadU32(data, offset + 8);
            if (count > exactCandidates.Length || start > exactCandidates.Length - count)
                throw new InvalidOperationException("Match shard exact candidate range escapes the candidate table.");

            var candidates = new int[count];
            for (var candidateIndex = 0; candidateIndex < count; candidateIndex++)
            {
                candidates[candidateIndex] = exactCandidates[start + candidateIndex];
            }

            exactIndex[text] = candidates;
        }

        return exactIndex;
    }

    private static int[] ReadPatternTokens(
        byte[] data,
        int patternTokensOffset,
        int patternTokenCount,
        string[] strings)
    {
        var patternTokens = new int[patternTokenCount];
        for (var index = 0; index < patternTokens.Length; index++)
        {
            patternTokens[index] = ReadU32(data, patternTokensOffset + index * 4);
            ReadStringById(strings, patternTokens[index], "match pattern token");
        }

        return patternTokens;
    }

    private static List<PatternCandidate> ReadPatterns(
        byte[] data,
        int patternRowsOffset,
        int patternCount,
        int[] patternTokens,
        string[] strings,
        int expectedEntryCount)
    {
        var patterns = new List<PatternCandidate>(patternCount);
        for (var index = 0; index < patternCount; index++)
        {
            var offset = patternRowsOffset + index * 16;
            var entryId = ReadU32(data, offset);
            var tokenStart = ReadU32(data, offset + 4);
            var tokenCount = ReadU16(data, offset + 8);
            if (entryId >= expectedEntryCount)
                throw new InvalidOperationException("Match shard pattern entry id is out of range.");

            if (tokenCount > patternTokens.Length || tokenStart > patternTokens.Length - tokenCount)
                throw new InvalidOperationException("Match shard pattern token range escapes the token table.");

            var tokens = new string[tokenCount];
            for (var tokenIndex = 0; tokenIndex < tokenCount; tokenIndex++)
            {
                tokens[tokenIndex] = ReadStringById(strings, patternTokens[tokenStart + tokenIndex], "match pattern token");
            }

            patterns.Add(new PatternCandidate(entryId, tokens));
        }

        return patterns;
    }
}
