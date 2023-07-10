using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Grimoire.Sources.Miscellaneous;

public static partial class Misc {
    public static string GetIdFromName(this string name) {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
    }

    public static string GetNameFromId(this string id) {
        return Encoding.UTF8.GetString(Convert.FromBase64String(id));
    }

    public static string CleanPath(this string str) {
        return WebUtility.UrlDecode(str)
            .Replace(' ', '_');
    }

    public static string Clean(this string str) {
        return
            string.IsNullOrWhiteSpace(str)
                ? str
                : Regex.Replace(str, """\r\n?|\n|\s{2,}""", string.Empty,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
}