namespace Grimoire.Objects;

public record SourceObject {
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Favicon { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
    public bool IsDisabled { get; set; }

    public string Id => Name.GetIdFromName();
}