using AngleSharp;
using AngleSharp.Dom;

namespace Grimoire.Sources.Miscellaneous;

public static partial class Misc {
    private static readonly IBrowsingContext Context = BrowsingContext.New(
        Configuration.Default
            .WithDefaultLoader()
            .WithDefaultCookies()
            .WithLocaleBasedEncoding()
    );

    public static T As<T>(this IElement element) {
        return (T)element;
    }

    public static T As<T>(this INode node) {
        return (T)node;
    }

    public static string[] Split(this INode node, char slice) {
        return node == null || string.IsNullOrWhiteSpace(node.TextContent)
            ? default
            : node.TextContent.Split(slice);
    }

    public static Task<IDocument> ParseAsync(string url) {
        return Context.OpenAsync(url);
    }
}