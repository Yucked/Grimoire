using System.Text;
using System.Text.Json.Serialization;

namespace Grimoire.Objects;

public readonly record struct SourceObject(
    string Name,
    string Url,
    string Favicon,
    DateTime UpdatedOn,
    bool IsDisabled) {
    public string Id
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(Name));
    [JsonIgnore]
    public string RavenPath
        => $"sources/{Id}";
}