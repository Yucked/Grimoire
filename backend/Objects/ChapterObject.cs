using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grimoire.Objects;

/// <summary>
/// 
/// </summary>
/// <param name="Title"></param>
/// <param name="Number"></param>
/// <param name="ReleasedOn"></param>
/// <param name="IsDownloaded"></param>
/// <param name="LastPageRead"></param>
/// <param name="Pages"></param>
public record ChapterObject(
    string Title,
    int Number,
    DateOnly ReleasedOn,
    bool IsDownloaded,
    int LastPageRead,
    Dictionary<int, Page> Pages);