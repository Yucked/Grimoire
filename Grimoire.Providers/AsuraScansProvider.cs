using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
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

        var lastPage = (document.GetElementsByClassName("page-numbers")
                .LastOrDefault()
            as IHtmlAnchorElement).Href[^4..^3];

        var results = await Task.WhenAll(Enumerable
            .Range(1, int.Parse(lastPage))
            .Select(PaginateAsync));

        return results
            .SelectMany(x => x)
            .ToArray();
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
                    Name = info.TextContent,
                    Url = info?.Href!,
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
        throw new NotImplementedException();
    }
}