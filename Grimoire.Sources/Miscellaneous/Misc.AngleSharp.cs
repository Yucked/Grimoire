using AngleSharp.Dom;

namespace Grimoire.Sources.Miscellaneous;

public static partial class Misc {
    public static T Find<T>(this INode node, string nodeName, string query) where T : class, INode {
        return node
            .Descendents()
            .FirstOrDefault(x =>
                x.NodeName == nodeName &&
                x.TextContent.Contains(query))
            ?.Parent
            ?.FindDescendant<T>();
    }

    public static string[] Split(this INode node, char slice) {
        return node == null || string.IsNullOrWhiteSpace(node.TextContent)
            ? default
            : node.TextContent?.Split(slice);
    }
}