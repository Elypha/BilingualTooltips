namespace BilingualTooltips.Modules.Dialogue;

internal static class DialogueFontGlyphRanges
{
    public static readonly ushort[] Current =
    [
        0x0020, 0x007E, // Basic Latin, printable subset.
        0x00A0, 0x00FF, // Latin-1 Supplement, printable subset.
        0x0100, 0x017F, // Latin Extended-A.
        0x0370, 0x03FF, // Greek and Coptic.
        0x0400, 0x04FF, // Cyrillic.
        0x2000, 0x206F, // General Punctuation.
        0x20A0, 0x20CF, // Currency Symbols.
        0x2100, 0x214F, // Letterlike Symbols.
        0x2150, 0x218F, // Number Forms.
        0x2190, 0x21FF, // Arrows.
        0x2200, 0x22FF, // Mathematical Operators.
        0x2500, 0x257F, // Box Drawing.
        0x25A0, 0x25FF, // Geometric Shapes.
        0x2600, 0x26FF, // Miscellaneous Symbols.
        0x3000, 0x303F, // CJK Symbols and Punctuation.
        0x3040, 0x309F, // Hiragana.
        0x30A0, 0x30FF, // Katakana.
        0x3130, 0x318F, // Hangul Compatibility Jamo.
        0x31F0, 0x31FF, // Katakana Phonetic Extensions.
        0x4E00, 0x9FFF, // CJK Unified Ideographs.
        0xAC00, 0xD7AF, // Hangul Syllables.
        0xFF00, 0xFFEF, // Halfwidth and Fullwidth Forms.
        0,
    ];
}
