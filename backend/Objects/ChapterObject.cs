namespace Grimoire.Objects;

public readonly record struct ChapterObject(
    string Title,
    string Number,
    DateOnly ReleasedOn,
    bool IsDownloaded,
    string SourceUrl,
    string[] Pages) {
    public string[] Pages { get; init; } = Pages ?? [];
}
