using BilingualTooltips.Modules.Dialogue.Data;

namespace BilingualTooltips.Modules.Dialogue;

public sealed class DialogueShardRequirement : IEquatable<DialogueShardRequirement>
{
    public static DialogueShardRequirement Empty { get; } = new(new HashSet<BttLanguage>(), new HashSet<BttLanguage>());

    public IReadOnlySet<BttLanguage> TemplateShards { get; }
    public IReadOnlySet<BttLanguage> MatchShards { get; }
    public bool RequiresStore => TemplateShards.Count > 0 || MatchShards.Count > 0;

    private DialogueShardRequirement(
        IReadOnlySet<BttLanguage> templateShards,
        IReadOnlySet<BttLanguage> matchShards)
    {
        TemplateShards = templateShards;
        MatchShards = matchShards;
    }

    public static DialogueShardRequirement ForTalk(BttLanguage inputLanguage, BttLanguage targetLanguage)
    {
        inputLanguage = inputLanguage.ResolveInput();
        return new DialogueShardRequirement(
            ReleaseShardSet(targetLanguage),
            ReleaseShardSet(inputLanguage));
    }

    public static DialogueShardRequirement ForLookupHistory() =>
        new(
            BttLanguageExtensions.ReleaseLanguages.ToHashSet(),
            new HashSet<BttLanguage>());

    public static DialogueShardRequirement Merge(IEnumerable<DialogueShardRequirement> requirements)
    {
        var templateShards = new HashSet<BttLanguage>();
        var matchShards = new HashSet<BttLanguage>();

        foreach (var requirement in requirements)
        {
            templateShards.UnionWith(requirement.TemplateShards);
            matchShards.UnionWith(requirement.MatchShards);
        }

        return new DialogueShardRequirement(templateShards, matchShards);
    }

    public bool IsSatisfiedBy(DialogueStore store) => store.HasPreparedShards(this);

    public bool Equals(DialogueShardRequirement? other) =>
        other != null
        && TemplateShards.SetEquals(other.TemplateShards)
        && MatchShards.SetEquals(other.MatchShards);

    public override bool Equals(object? obj) => obj is DialogueShardRequirement other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var language in TemplateShards.Order())
        {
            hash.Add(language);
        }

        hash.Add(-1);
        foreach (var language in MatchShards.Order())
        {
            hash.Add(language);
        }

        return hash.ToHashCode();
    }

    private static HashSet<BttLanguage> ReleaseShardSet(BttLanguage language) =>
        language.IsReleaseSupported() ? [language] : new HashSet<BttLanguage>();
}
