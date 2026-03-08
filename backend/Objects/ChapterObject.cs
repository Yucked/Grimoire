namespace Grimoire.Objects;

/// <summary>
/// 
/// </summary>
/// <param name="Title"></param>
/// <param name="Number"></param>
/// <param name="ReleasedOn"></param>
/// <param name="IsDownloaded"></param>
/// <param name="SourceUrl"></param>
/// <param name="Pages"></param>
public readonly record struct ChapterObject(
    string Title,
    string Number,
    DateOnly ReleasedOn,
    bool IsDownloaded,
    string SourceUrl,
    Dictionary<int, PageObject> Pages);