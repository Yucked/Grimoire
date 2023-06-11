namespace Grimoire.Commons.Models; 

public readonly record struct MangaChapter(
    string Name,
    string Url,
    DateTimeOffset ReleasedOn);