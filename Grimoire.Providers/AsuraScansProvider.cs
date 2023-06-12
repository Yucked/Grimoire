using AngleSharp.Html.Dom;
using Grimoire.Providers.Interfaces;
using Grimoire.Providers.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Providers;

public class AsuraScansProvider : IGrimoireProvider {
    public string Name
        => "Asura Scans";

    public string BaseUrl
        => "https://www.asurascans.com";

    public string Icon
        => "wp-content/uploads/2021/03/Group_1.png";

    private readonly HttpClient _httpClient;
    private readonly ILogger<AsuraScansProvider> _logger;

    public AsuraScansProvider(HttpClient httpClient, ILogger<AsuraScansProvider> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await _httpClient.ParseAsync($"{BaseUrl}/?s=");
        var lastPage = (document.GetElementsByClassName("page-numbers").Skip(4).FirstOrDefault() as IHtmlAnchorElement)
            .Href[^4..^3];

        var results = await Task.WhenAll(Enumerable
            .Range(1, int.Parse(lastPage))
            .Select(PaginateAsync));

        var populate = results
            .SelectMany(x => x)
            .AsParallel()
            .Select(async manga => {
                _logger.LogDebug("Getting additional information for {manga}", manga.Name);
                using var doc = await _httpClient.ParseAsync(manga.Url);

                // TODO: Cleanup text, lots of text breaks. Mentonyms match against Genre, getting duplicated.
                var info = doc.QuerySelector("div.infox");
                manga.Author = info.Children[3].Children[1].Children[1].TextContent;
                manga.Summary = info.QuerySelector("div.entry-content").TextContent;
                manga.Genre = info.QuerySelector("span.mgen").TextContent.Split(' ');
                manga.Metonyms = info.QuerySelector("div.wd-full > span").TextContent.Split(',');
                manga.LastFetch = DateTimeOffset.Now;
                manga.Chapters = doc.GetElementsByClassName("eph-num")
                    .Select(x => {
                        var anchor = x.Children[0] as IHtmlAnchorElement;
                        return new MangaChapter {
                            Name = anchor.Children[0].TextContent,
                            Url = anchor.Href,
                            ReleasedOn = DateOnly.Parse(anchor.Children[1].TextContent)
                        };
                    })
                    .ToArray();
                return manga;
            });

        return await Task.WhenAll(populate);
    }

    public async Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        using var document = await _httpClient.ParseAsync($"{BaseUrl}/page/{page}/?s");
        var titles = document.GetElementsByClassName("bsx");
        _logger.LogDebug("Parsing page #{page} with {titlesCount} titles", page, titles.Length);

        return titles
            .AsParallel()
            .Select(x => {
                var info = x.Children[0] as IHtmlAnchorElement;
                return new Manga {
                    Name = info.Title,
                    Url = info.Href!,
                    Cover = (info
                                .GetElementsByClassName("ts-post-image wp-post-image attachment-medium size-medium")
                                .First()
                            as IHtmlImageElement)
                        .Source,
                    LastFetch = DateTimeOffset.Now,
                    Provider = GetType().Name[..^8]
                };
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<MangaChapter>> FetchChaptersAsync(Manga manga) {
        using var document = await _httpClient.ParseAsync(manga.Url);
        return document.GetElementsByClassName("eph-num")
            .Select(x => {
                var anchor = x.Children[0] as IHtmlAnchorElement;
                return new MangaChapter {
                    Name = anchor.Children[0].TextContent,
                    Url = anchor.Href,
                    ReleasedOn = DateOnly.Parse(anchor.Children[1].TextContent)
                };
            })
            .ToArray();
    }
}