using System.Text;

namespace Grimoire.Objects;

/// <summary>
/// 
/// </summary>
/// <param name="Authors"></param>
/// <param name="Artists"></param>
/// <param name="Title"></param>
/// <param name="Aliases"></param>
/// <param name="Summary"></param>
/// <param name="Genres"></param>
/// <param name="Status"></param>
/// <param name="Cover"></param>
/// <param name="CoverPath"></param>
/// <param name="SourceUrl"></param>
/// <param name="Ratings"></param>
/// <param name="UpdatedAt"></param>
/// <param name="ReleasedOn"></param>
/// <param name="Chapters"></param>
/// <param name="SourceId"></param>
public readonly record struct MangaObject(
    IList<string> Authors,
    IList<string> Artists,
    string Title,
    IList<string> Aliases,
    string Summary,
    IList<string> Genres,
    MangaStatus Status,
    string Cover,
    string CoverPath,
    string SourceUrl,
    float Ratings,
    DateOnly UpdatedAt,
    DateOnly ReleasedOn,
    IList<ChapterObject> Chapters,
    MangaType Type,
    string SourceId) {
    public string Id
        => $"{SourceId}/{Convert.ToBase64String(Encoding.UTF8.GetBytes(Title)).ToLowerInvariant()}";
}