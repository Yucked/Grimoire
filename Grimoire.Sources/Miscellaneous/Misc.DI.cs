namespace Grimoire.Sources.Miscellaneous;

public static partial class Misc {
    public static Task<T[]> AwaitAsync<T>(this IEnumerable<Task<T>> results) {
        return Task.WhenAll(results);
    }
}