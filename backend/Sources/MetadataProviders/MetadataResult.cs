using Grimoire.Objects;

namespace Grimoire.Sources.MetadataProviders;

public record MetadataResult {
    public MetadataResult() {
        Authors = new List<string>();
        Artists = new List<string>();
        Aliases = new List<string>();
    }

    public IList<string> Authors { get; }
    public IList<string> Artists { get; }
    public IList<string> Genres { get; set; }
    public IList<string> Aliases { get; }
    public MangaStatus Status { get; set; }
    public DateOnly ReleasedOn { get; set; }
    public float Ratings { get; set; }
}