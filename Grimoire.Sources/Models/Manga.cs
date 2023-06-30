using Grimoire.Sources.Miscellaneous;

namespace Grimoire.Sources.Models;

public record Manga {
    public string Id
        => Name.GetIdFromName();

    public string Name { get; set; }
    public string Url { get; set; }
    public string Cover { get; set; }
    public string Summary { get; set; }
    public string Author { get; set; }
    public string SourceName { get; set; }
    public DateTimeOffset LastFetch { get; set; }
    public IReadOnlyList<string> Metonyms { get; set; }
    public IReadOnlyList<string> Genre { get; set; }
    public IList<Chapter> Chapters { get; set; }
}