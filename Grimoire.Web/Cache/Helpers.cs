using System.Text.Json;
using Grimoire.Providers.Interfaces;

namespace Grimoire.Web.Cache;

public static class Helpers {
    public static string GetId(this string str) {
        return str.Replace(' ', '_');
    }
    
    public static string GetBasePath(this string str, string id) {
        return Path.Combine(str, id);
    }

    public static string GetIconPath(this string str, string icon) {
        return Path.Combine(str, icon.Split('/')[^1]);
    }
    
    public static string GetIconPath(this string path, string manga, string icon) {
        return Path.Combine(path, icon.Split('/')[^1]);
    }

    internal static byte[] Encode<T>(this T value) {
        return JsonSerializer.SerializeToUtf8Bytes(value);
    }

    internal static T Decode<T>(this byte[] value) {
        return JsonSerializer.Deserialize<T>(value);
    }
}