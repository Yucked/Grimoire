namespace Grimoire.Objects;

public readonly record struct ChapterObject(
    string Title,
    string Number,
    DateOnly ReleasedOn,
    string SourceUrl,
    string[] Pages) {
    public string[] Pages { get; init; } = Pages ?? [];
    public bool IsDownloaded => Pages is { Length: > 0 };
}
