using System.Collections.Concurrent;
using System.Text;

namespace Grimoire.Objects;

public readonly record struct UserObject(
    string Username,
    DateOnly CreatedAt,
    ConcurrentDictionary<string, float> Library) {
    public string Id
        => Username.GetIdFromName();
}