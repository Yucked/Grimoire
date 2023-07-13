namespace Grimoire.Commons.Models;

/// <summary>
/// 
/// </summary>
public record Manga {
    /// <summary>
    /// 
    /// </summary>
    public string Id
        => Name.GetIdFromName();

    /// <summary>
    /// 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Cover { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string SourceId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsInLibrary { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset LastFetch { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> Metonyms { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> Genre { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IList<Chapter> Chapters { get; set; }
}