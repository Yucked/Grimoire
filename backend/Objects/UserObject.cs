using System.Collections.Concurrent;

namespace Grimoire.Objects;

public readonly record struct UserObject(
    string Username,
    DateOnly CreatedAt,
    ConcurrentDictionary<string, float> Library) {
    public string Id
        => Username.GetIdFromName();
}