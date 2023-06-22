using System.Text.RegularExpressions;

namespace Grimoire.Sources;

public static partial class Misc {
    [GeneratedRegex("\\r\\n?|\\n")]
    private static partial Regex CleanRegex();

    public static string Clean(this string str) {
        return string.IsNullOrWhiteSpace(str) ? str : CleanRegex().Replace(str, string.Empty);
    }
}