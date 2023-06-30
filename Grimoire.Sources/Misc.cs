using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Sources.Models;

namespace Grimoire.Sources;

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

    public static IReadOnlyList<Chapter> ParseWordPressChapters(this IDocument document) {
        return document.GetElementById("chapterlist")
            .FirstChild
            .ChildNodes
            .Where(x => x is IHtmlListItemElement)
            .Select(x => {
                var element = x as IHtmlElement;
                return new Chapter {
                    Name = element.GetElementsByClassName("chapternum").FirstOrDefault().TextContent.Clean(),
                    Url = x.FindDescendant<IHtmlAnchorElement>().Href,
                    ReleasedOn = DateOnly.Parse(
                        element.GetElementsByClassName("chapterdate").FirstOrDefault().TextContent)
                };
            })
            .ToArray();
    }

    public static string[] Split(this INode node, char slice) {
        return node.TextContent.Split(slice);
    }
}