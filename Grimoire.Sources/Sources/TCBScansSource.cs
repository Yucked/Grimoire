using AngleSharp.Html.Dom;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public sealed class TCBScansSource : IGrimoireSource {
    public string Name
        => "TCB Scans";

    public string BaseUrl
        => "https://tcbscans.com";

    public string Icon
        => $"{BaseUrl}/files/apple-touch-icon.png";

    private readonly HttpClient _httpClient;
    private readonly ILogger<TCBScansSource> _logger;

    public TCBScansSource(HttpClient httpClient, ILogger<TCBScansSource> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await _httpClient.ParseAsync($"{BaseUrl}/projects");
        var items = document.GetElementsByClassName("flex flex-col items-center md:flex-row md:items-start");
        var result = items.AsParallel()
            .Select(async x => {
                var anchor = x
                    .GetElementsByClassName("relative h-24 w-24 sm:mb-0 mb-3")
                    .FirstOrDefault()
                    .Children[0] as IHtmlAnchorElement;

                var img = anchor.Children[0] as IHtmlImageElement;
                using var doc = await _httpClient.ParseAsync($"{BaseUrl}{anchor.PathName}");
                _logger.LogInformation("Getting additional information for {manga}", img.Attributes[1].Value);

                return new Manga {
                    Name = img.AlternativeText,
                    Url = $"{BaseUrl}{anchor.PathName}",
                    Cover = img.Source,
                    LastFetch = DateTimeOffset.Now,
                    SourceName = GetType().Name[..^8],
                    Summary = doc.GetElementsByClassName("leading-6 my-3")
                        .FirstOrDefault()
                        .TextContent,
                    Chapters = doc.GetElementsByClassName("block border border-border bg-card mb-3 p-3 rounded")
                        .Select(c => new Chapter {
                            Name = c.TextContent,
                            Url = $"{BaseUrl}{(c as IHtmlAnchorElement).PathName}"
                        })
                        .ToArray()
                };
            });
        return await Task.WhenAll(result);
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotImplementedException("Source doesn't have pagination.");
    }

    public async Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        using var document = await _httpClient.ParseAsync(manga.Url);
        _logger.LogDebug("Fetching chapters for {name}", manga.Name);
        return document.GetElementsByClassName("block border border-border bg-card mb-3 p-3 rounded")
            .Select(x => new Chapter {
                Name = x.TextContent,
                Url = $"{BaseUrl}{(x as IHtmlAnchorElement).PathName}"
            })
            .ToArray();
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await _httpClient.ParseAsync(chapter.Url);
        chapter.Pages = document
            .QuerySelectorAll("img.fixed-ratio-content")
            .Select((x, index) => new {
                Key = index, Value = (x as IHtmlImageElement).Source
            })
            .ToDictionary(x => x.Key, x => x.Value);
        return chapter;
    }
}