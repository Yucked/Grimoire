using System.Text.Json;
using Grimoire.Providers.Interfaces;
using StackExchange.Redis;

namespace Grimoire.Db;

public static class Helpers {
    public static string GetId(this IGrimoireProvider provider) {
        return provider.Name.Replace(' ', '_');
    }

    internal static byte[] Encode<T>(this T value) {
        return JsonSerializer.SerializeToUtf8Bytes(value);
    }

    internal static T Decode<T>(this RedisValue value) {
        return JsonSerializer.Deserialize<T>(value);
    }
}