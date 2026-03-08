namespace Grimoire.Objects;

public record MangaObject {
    public string Title { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string CoverPath { get; set; } = string.Empty;
    public IList<string> Authors { get; set; } = [];
    public IList<string> Artists { get; set; } = [];
    public IList<string> Aliases { get; set; } = [];
    public IList<string> Genres { get; set; } = [];
    public MangaStatus Status { get; set; }
    public MangaType Type { get; set; }
    public float Ratings { get; set; }
    public DateOnly UpdatedAt { get; set; }
    public DateOnly ReleasedOn { get; set; }
    public IList<ChapterObject> Chapters { get; set; } = [];

    public string Id => $"{SourceId}/{Title.GetIdFromName()}";
}