using System.Net;
using AngleSharp.Dom;

namespace Grimoire.Sources;

public static class Misc {
    public static string CleanPath(this string str) {
        return WebUtility.UrlDecode(str)
            .Replace(' ', '_');
    }

    public static string[] Slice(this string str, char seperator) {
        return string.IsNullOrWhiteSpace(str)
            ? new[] { str }
            : str.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] Slice(this string str, char[] seperator) {
        return string.IsNullOrWhiteSpace(str)
            ? new[] { str }
            : str.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string Join(this IEnumerable<string> strs) {
        return string.Join(" ", strs);
    }

    public static Task<T[]> AwaitAsync<T>(this IEnumerable<Task<T>> results) {
        return Task.WhenAll(results);
    }

    public static T As<T>(this object element) {
        return (T)element;
    }

    public static string[] Split(this INode node, char slice) {
        return node == null || string.IsNullOrWhiteSpace(node.TextContent)
            ? default
            : node.TextContent.Split(slice);
    }
}