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
/// <param name="LastChapterRead"></param>
/// <param name="SourceUrl"></param>
/// <param name="Ratings"></param>
/// <param name="UpdatedAt"></param>
/// <param name="ReleasedOn"></param>
/// <param name="Chapters"></param>
/// <param name="Metadata"></param>
public record MangaObject(
    IList<string> Authors,
    IList<string> Artists,
    string Title,
    IList<string> Aliases,
    string Summary,
    IList<string> Genres,
    MangaStatus Status,
    string Cover,
    int LastChapterRead,
    string SourceUrl,
    float Ratings,
    DateOnly UpdatedAt,
    DateOnly ReleasedOn,
    IList<ChapterObject> Chapters,
    MetadataObject Metadata);