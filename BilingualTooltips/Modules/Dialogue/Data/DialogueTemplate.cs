using System.Buffers.Binary;
using BilingualTooltips.Configuration;

namespace BilingualTooltips.Modules.Dialogue.Data;

public enum BttDialogueGender
{
    Unspecified,
    Male,
    Female,
}

public static class BttDialogueGenderExtensions
{
    public static string DisplayName(this BttDialogueGender gender) => gender switch
    {
        BttDialogueGender.Unspecified => "Package default",
        BttDialogueGender.Male => "Male",
        BttDialogueGender.Female => "Female",
        _ => gender.ToString(),
    };
}

public sealed class DialogueTemplate
{
    private const byte TextOp = 1;
    private const byte PlayerNameOp = 2;
    private const byte GenderOp = 3;

    private static readonly DialogueTemplatePart[] NoParts = [];
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static DialogueTemplate Empty { get; } = new(NoParts);

    private readonly IReadOnlyList<DialogueTemplatePart> _parts;

    private DialogueTemplate(IReadOnlyList<DialogueTemplatePart> parts)
    {
        _parts = parts;
    }

    public bool IsEmpty => _parts.Count == 0;

    internal static DialogueTemplate Read(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return Empty;

        var offset = 0;
        var template = ReadTemplate(data, ref offset);
        if (offset != data.Length)
            throw new InvalidOperationException("Dialogue template has trailing bytes.");

        return template;
    }

    internal string Render(DialogueRenderProfile profile)
    {
        if (_parts.Count == 0) return "";

        var builder = new StringBuilder();
        AppendTo(builder, profile);
        return builder.ToString();
    }

    private void AppendTo(StringBuilder builder, DialogueRenderProfile profile)
    {
        foreach (var part in _parts)
        {
            part.AppendTo(builder, profile);
        }
    }

    private static DialogueTemplate ReadTemplate(ReadOnlySpan<byte> data, ref int offset)
    {
        var count = ReadU32(data, ref offset, "template part count");
        if (count == 0) return Empty;

        var parts = new DialogueTemplatePart[count];
        for (var index = 0; index < count; index++)
        {
            var op = ReadByte(data, ref offset, "template op");
            parts[index] = op switch
            {
                TextOp => new TextPart(ReadString(data, ref offset)),
                PlayerNameOp => PlayerNamePart.Instance,
                GenderOp => ReadGenderPart(data, ref offset),
                _ => throw new InvalidOperationException($"Unsupported dialogue template op: {op}."),
            };
        }

        return new DialogueTemplate(parts);
    }

    private static GenderPart ReadGenderPart(ReadOnlySpan<byte> data, ref int offset)
    {
        var female = ReadNestedTemplate(data, ref offset, "female gender branch");
        var male = ReadNestedTemplate(data, ref offset, "male gender branch");
        return new GenderPart(female, male);
    }

    private static DialogueTemplate ReadNestedTemplate(ReadOnlySpan<byte> data, ref int offset, string name)
    {
        var length = ReadU32(data, ref offset, $"{name} length");
        Require(data, offset, length, name);
        var nestedOffset = 0;
        var template = length == 0
            ? Empty
            : ReadTemplate(data.Slice(offset, length), ref nestedOffset);
        if (nestedOffset != length)
            throw new InvalidOperationException($"Dialogue template {name} has trailing bytes.");

        offset += length;
        return template;
    }

    private static string ReadString(ReadOnlySpan<byte> data, ref int offset)
    {
        var length = ReadU32(data, ref offset, "text length");
        Require(data, offset, length, "template text");
        var value = StrictUtf8.GetString(data.Slice(offset, length));
        offset += length;
        return value;
    }

    private static byte ReadByte(ReadOnlySpan<byte> data, ref int offset, string name)
    {
        Require(data, offset, 1, name);
        return data[offset++];
    }

    private static int ReadU32(ReadOnlySpan<byte> data, ref int offset, string name)
    {
        Require(data, offset, 4, name);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        if (value > int.MaxValue)
            throw new InvalidOperationException($"Dialogue template {name} is too large.");

        offset += 4;
        return (int)value;
    }

    private static void Require(ReadOnlySpan<byte> data, int offset, int length, string name)
    {
        if ((uint)offset > data.Length || (uint)length > data.Length - offset)
            throw new InvalidOperationException($"Dialogue template {name} escapes the payload.");
    }

    private abstract class DialogueTemplatePart
    {
        public abstract void AppendTo(StringBuilder builder, DialogueRenderProfile profile);
    }

    private sealed class TextPart(string value) : DialogueTemplatePart
    {
        public override void AppendTo(StringBuilder builder, DialogueRenderProfile profile)
            => builder.Append(value);
    }

    private sealed class PlayerNamePart : DialogueTemplatePart
    {
        public static PlayerNamePart Instance { get; } = new();

        public override void AppendTo(StringBuilder builder, DialogueRenderProfile profile)
            => builder.Append(profile.PlayerName);
    }

    private sealed class GenderPart(DialogueTemplate female, DialogueTemplate male) : DialogueTemplatePart
    {
        public override void AppendTo(StringBuilder builder, DialogueRenderProfile profile)
            => (profile.Gender == BttDialogueGender.Male ? male : female).AppendTo(builder, profile);
    }
}

internal readonly record struct DialogueRenderProfile(
    string FirstName,
    string LastName,
    BttDialogueGender Gender)
{
    public string FullName
    {
        get
        {
            var firstName = FirstName.Trim();
            var lastName = LastName.Trim();
            return (firstName, lastName) switch
            {
                ({ Length: > 0 }, { Length: > 0 }) => $"{firstName} {lastName}",
                ({ Length: > 0 }, _) => firstName,
                (_, { Length: > 0 }) => lastName,
                _ => "",
            };
        }
    }

    public string PlayerName
    {
        get
        {
            var fullName = FullName;
            return string.IsNullOrWhiteSpace(fullName) ? "{object:1}" : fullName;
        }
    }

    public static DialogueRenderProfile FromConfig(BilingualTooltipsConfig config) => new(
        config.TalkDialogueRenderProfileFirstName,
        config.TalkDialogueRenderProfileLastName,
        config.TalkDialogueRenderProfileGender);
}
