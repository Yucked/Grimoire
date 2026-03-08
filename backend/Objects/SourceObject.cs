namespace Grimoire.Objects;

public readonly record struct SourceObject(
    string Name,
    string Url,
    string Favicon,
    DateTime UpdatedOn,
    bool IsDisabled) {
    public string Id
        => Name.GetIdFromName();
}