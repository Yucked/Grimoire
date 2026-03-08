using System.Text;

namespace Grimoire.Objects;

public readonly record struct UserObject(
    string Username,
    DateTime CreatedAt,
    IDictionary<string, float> Library) {
    public string Id
        => Convert
            .ToBase64String(Encoding.UTF8.GetBytes(Username))
            .ToLowerInvariant();
}