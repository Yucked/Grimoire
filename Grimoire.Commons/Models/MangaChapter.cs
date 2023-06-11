namespace Grimoire.Common.Models; 

public readonly record struct MangaChapter(
    string Name,
    string Url,
    DateTimeOffset ReleasedOn);