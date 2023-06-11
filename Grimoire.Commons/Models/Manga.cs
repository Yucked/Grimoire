namespace Grimoire.Common.Models;

public sealed record Manga {
    public string Name { get; set; }
    public string Url { get; set; }
    public string Cover { get; set; }
    public string Summary { get; set; }
    public string Author { get; set; }
    public DateTimeOffset LastFetch { get; set; }
    public IReadOnlyList<string> Metonyms { get; set; }
    public IReadOnlyList<string> Genre { get; set; }
    public IReadOnlyList<MangaChapter> Chapters { get; set; }
}