using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources;

public sealed partial class OmegaScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<OmegaScansSource> logger) : IGrimoireSource {
    public string Name
        => "Omega Scans";

    public string Url
        => "https://api.omegascans.org";

    public string Icon
        => "https://omegascans.org/icon.png";

    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex ChapterNumberRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex RelativeDateRegex();

    private readonly ConcurrentDictionary<string, (string Title, string Cover, string Genre)> _apiCache = new();

    public async Task<IReadOnlyList<MangaObject>> GetMangasAsync() {
        using var jsonDocument = await scrapingHandler.GetJsonDocumentAsync(
            $"{Url}/query?visibility=Public&series_type=All&perPage=100");

        if (!jsonDocument.RootElement.TryGetProperty("data", out var dataElement)) {
            logger.LogError("Unable to fetch JSON data from source!");
            throw new Exception("Missing 'data' property in Omega Scans API response.");
        }

        var tasks = dataElement
            .EnumerateArray()
            .Select(x => {
                var slug = x.GetProperty("series_slug").GetString()!;
                _apiCache.TryAdd(slug, (
                    x.GetProperty("title").GetString()!,
                    x.GetProperty("thumbnail").GetString()!,
                    x.GetProperty("series_type").GetString()!));
                return GetMangaAsync($"https://omegascans.org/series/{slug}");
            });

        var mangas = await Task.WhenAll(tasks);
        _apiCache.Clear();
        return mangas;
    }

    public async Task<MangaObject> GetMangaAsync(string url) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(url);
        var cached = _apiCache[url.Split('/')[^1]];

        var authorElement = document
            .QuerySelectorAll("div.flex > p")
            .FirstOrDefault(x => x.TextContent.Contains("Author:"));
        var authorText = authorElement?.Children[^1].TextContent;
        IList<string> authors = string.IsNullOrEmpty(authorText) ? [] : [authorText];

        var summary = document.QuerySelector("div.bg-gray-800 > p")?.TextContent
                      ?? document.QuerySelectorAll("div.col-span-12 > div.bg-gray-800")
                          .Select(x => x.Text())
                          .Join();

        var aliases = document
            .QuerySelector("div.col-span-12 > p.text-center")
            ?.TextContent
            .Slice('|') ?? [];

        var chapters = document
            .QuerySelectorAll("ul.grid > a.text-gray-50")
            .Select(x => {
                var chapterText = x.QuerySelector("div.flex > span.m-0")!.TextContent;
                return new ChapterObject {
                    Title = chapterText.Clean(),
                    Number = ChapterNumberRegex().Match(chapterText).Value,
                    SourceUrl = x.As<IHtmlAnchorElement>().Href,
                    ReleasedOn = ChangeToDate(x.QuerySelector("div.flex > span.block")!.TextContent)
                };
            })
            .ToList();

        var coverPath = string.Empty;
        try {
            coverPath = await scrapingHandler.SaveCoverAsync(cached.Cover, Name.GetIdFromName(), cached.Title);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to download cover for {}", cached.Title);
        }

        var mangaObject = new MangaObject {
            Title = cached.Title,
            SourceId = Name.GetIdFromName(),
            SourceUrl = url,
            Summary = summary,
            Cover = cached.Cover,
            CoverPath = coverPath,
            Authors = authors,
            Genres = [cached.Genre],
            Aliases = aliases,
            Chapters = chapters,
            UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        foreach (var provider in metadataProviders)
            try {
                var enrichment = await provider.FindMangaAsync(cached.Title);
                if (enrichment is null) continue;
                mangaObject = mangaObject.WithMetadata(enrichment);
                break;
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Metadata enrichment failed for {title} via {name}",
                    cached.Title,
                    provider.GetType().Name);
            }

        return mangaObject;
    }

    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter, string sourceId, string mangaId) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(chapter.SourceUrl);
        var imageUrls = document
            .QuerySelectorAll("p.flex > img")
            .Select(x => x.GetAttribute("src"))
            .Where(x => x is not null)
            .ToList();

        var pages = new Dictionary<int, PageObject>();
        for (var i = 0; i < imageUrls.Count; i++)
            pages.Add(i, new PageObject(false, string.Empty, imageUrls[i]!));

        document.Close();
        return chapter with { Pages = pages };
    }

    private static DateOnly ChangeToDate(string str) {
        try {
            return DateOnly.Parse(str);
        }
        catch {
            var match = RelativeDateRegex().Match(str);
            var number = match.Success ? int.Parse(match.Value) : 0;
            var span = str switch {
                _ when str.Contains("minutes") => TimeSpan.FromMinutes(number),
                _ when str.Contains("hours")   => TimeSpan.FromHours(number),
                _ when str.Contains("days")    => TimeSpan.FromDays(number),
                _                              => TimeSpan.Zero
            };
            return DateOnly.FromDateTime(DateTime.UtcNow.Subtract(span));
        }
    }
}