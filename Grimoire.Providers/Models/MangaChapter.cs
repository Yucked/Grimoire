namespace Grimoire.Providers.Models; 

public readonly record struct MangaChapter(
    string Name,
    string Url,
    DateOnly ReleasedOn);