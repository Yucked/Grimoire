using Grimoire.Objects;

namespace Grimoire.Integrations;

public record MetadataResult(
    string Id,
    IList<string> Authors,
    IList<string> Artists,
    IList<string> Genres,
    IList<string> Aliases,
    MangaStatus Status,
    DateOnly ReleasedOn,
    float Ratings
);
