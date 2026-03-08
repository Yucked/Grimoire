using System.Collections.Concurrent;

namespace Grimoire.Objects;

public record UserObject {
    public string Username { get; set; } = string.Empty;
    public DateOnly CreatedAt { get; set; }
    public ConcurrentDictionary<string, float> Library { get; set; } = new();

    public string Id => Username.GetIdFromName();
}