using System.Text;

namespace Grimoire.Objects;

public readonly record struct SourceObject(
    string Name,
    string Url,
    string Favicon,
    DateTime UpdatedOn,
    bool IsDisabled) {
    public string Id
        => Convert
            .ToBase64String(Encoding.UTF8.GetBytes(Name))
            .ToLowerInvariant();
}