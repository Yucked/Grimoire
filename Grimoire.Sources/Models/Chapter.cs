namespace Grimoire.Sources.Models;

public sealed record Chapter {
    public string Name { get; set; }
    public string Url { get; set; }
    public DateOnly ReleasedOn { get; set; }
    public IReadOnlyDictionary<int, string> Pages { get; set; }
}