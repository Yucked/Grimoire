namespace Grimoire.Commons.Models;

/// <summary>
/// 
/// </summary>
public sealed record Chapter {
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
    public DateOnly ReleasedOn { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> Pages { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool HasBeenRead { get; set; }
}