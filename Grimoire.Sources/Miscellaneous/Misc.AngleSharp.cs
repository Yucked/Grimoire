using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Grimoire.Sources.Miscellaneous;

public static partial class Misc {
    public static IHtmlDivElement Find(this INode node, string query) {
        return (node
            .Descendents()
            .AsParallel()
            .First(x => x.TextContent.Trim() == query)
            ?.Parent as IHtmlDivElement)!;
    }

    public static T As<T>(this IElement element) {
        return (T)element;
    }

    public static T As<T>(this INode node) {
        return (T)node;
    }

    public static string[] Split(this INode node, char slice) {
        return node == null || string.IsNullOrWhiteSpace(node.TextContent)
            ? default
            : node.TextContent?.Split(slice);
    }
}