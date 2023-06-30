using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class RavenScansSource : IGrimoireSource {
    public string Name
        => "Raven Scans";

    public string BaseUrl
        => "https://ravenscans.com";

    public string Icon
        => "https://i0.wp.com/ravenscans.com/wp-content/uploads/2022/12/cropped-33.png";

    private readonly HttpClient _httpClient;
    private readonly ILogger<RavenScansSource> _logger;

    public RavenScansSource(HttpClient httpClient, ILogger<RavenScansSource> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await _httpClient.ParseAsync($"{BaseUrl}/manga/list-mode/");
        var results = document
            .QuerySelectorAll("a.series")
            .AsParallel()
            .Select(async x => {
                var manga = new Manga {
                    Name = x.TextContent,
                    Url = (x as IHtmlAnchorElement).Href,
                    SourceName = GetType().Name[..^6],
                    LastFetch = DateTimeOffset.Now
                };

                using var doc = await _httpClient.ParseAsync(manga.Url);
                var infoDiv = doc.QuerySelector("div.bigcontent");

                manga.Cover = infoDiv.FindDescendant<IHtmlImageElement>(2).Source;
                manga.Metonyms = infoDiv.Find<IHtmlSpanElement>("B", "Alternative").Split(',');
                manga.Summary = infoDiv.Find<IHtmlParagraphElement>("H2", "Synopsis").TextContent;
                manga.Author = infoDiv.Find<IHtmlSpanElement>("B", "Author").TextContent.Clean();
                manga.Genre = infoDiv.Find<IHtmlSpanElement>("B", "Genres").Split(' ');
                manga.Chapters = doc.ParseWordPressChapters().ToArray();

                return manga;
            });

        return await Task.WhenAll(results);
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotSupportedException("Data is fetched via list mode.");
    }

    public Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        try {
            return _httpClient.ParseWordPressChaptersAsync(manga.Url);
        }
        catch (Exception exception) {
            _logger.LogError("{exception}\n{message}", exception, exception.Message);
            throw;
        }
    }

    // TODO: Requires a delay
    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        try {
            return _httpClient.ParseWordPressChapterAsync(chapter, "img.ts-main-image");
        }
        catch (Exception exception) {
            _logger.LogError("{exception}\n{message}", exception, exception.Message);
            throw;
        }
    }
}