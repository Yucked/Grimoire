using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Handlers;
using Grimoire.Helpers;
using Grimoire.Models;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public class OmegaScansSource(
    HttpHandler httpHandler,
    ILogger<OmegaScansSource> logger) : IGrimoireSource {
    public string Name
        => "Omega Scans";

    public string Url
        => "https://api.omegascans.org";

    public string Icon
        => "https://omegascans.org/icon.png";

    private readonly ConcurrentDictionary<string, (string Name, string Cover, string Genre)> _apiCache = new();

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        var stream = await httpHandler.GetStreamAsync($"{Url}/query?visibility=Public&series_type=All&perPage=100");
        using var document = await JsonDocument.ParseAsync(stream);
        if (!document.RootElement.TryGetProperty("data", out var dataElement)) {
            logger.LogError("Unable to fetch JSON data from source!");
            throw new Exception("");
        }

        var tasks = dataElement
            .EnumerateArray()
            .AsParallel()
            .Select(x => {
                var slug = x.GetProperty("series_slug").GetString()!;
                _apiCache.TryAdd(slug,
                    (x.GetProperty("title").GetString()!,
                     x.GetProperty("thumbnail").GetString()!,
                     x.GetProperty("series_type").GetString()!));

                return GetMangaAsync($"https://omegascans.org/series/{slug}");
            });

        var mangas = await Task.WhenAll(tasks);
        _apiCache.Clear();
        return mangas;
    }

    public async Task<Manga> GetMangaAsync(string url) {
        var document = await httpHandler.ParseAsync(url);
        var cached = _apiCache[url.Split('/')[^1]];
        return new Manga {
            Name = cached.Name,
            Cover = cached.Cover,
            Genre = [cached.Genre],
            LastFetch = DateTimeOffset.Now,
            Url = url,
            SourceId = nameof(OmegaScansSource).GetIdFromName(),
            Author = document
                .QuerySelectorAll("div.flex > p")
                .FirstOrDefault(x => x.TextContent.Contains("Author:"))
                ?.Children[^1]
                .TextContent!,
            Summary = document
                          .QuerySelector("div.bg-gray-800 > p")!
                          ?.TextContent ??
                      document.QuerySelectorAll("div.col-span-12 > div.bg-gray-800")
                          .Select(x => x.Text())
                          .Join(),
            Metonyms = document
                .QuerySelector("div.col-span-12 > p.text-center")!
                .TextContent
                .Split('|'),
            Chapters = document
                .QuerySelectorAll("ul.grid > a.text-gray-50")
                .Select(x => new Chapter {
                    Name = x.QuerySelector("div.flex > span.m-0")!.TextContent,
                    ReleasedOn = ChangeToDate(x.QuerySelector("div.flex > span.block")!.TextContent),
                    Url = x.As<IHtmlAnchorElement>().Href
                })
                .ToArray()
        };
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await httpHandler.ParseAsync(chapter.Url);
        chapter.Pages = document
            .QuerySelectorAll("p.flex > img")
            .Select(x => x.As<IHtmlAnchorElement>().Href)
            .ToArray();
        return chapter;
    }

    private static DateOnly ChangeToDate(string str) {
        try {
            return DateOnly.Parse(str);
        }
        catch {
            var number = 0;
            if (str.Contains("hours") || str.Contains("minutes") || str.Contains("days")) {
#pragma warning disable SYSLIB1045
                number = int.Parse(Regex.Match(str, @"\d+").Value);
#pragma warning restore SYSLIB1045
            }

            var span = str switch {
                _ when str.Contains("minutes")
                    => TimeSpan.FromMinutes(number),
                _ when str.Contains("hours")
                    => TimeSpan.FromHours(number),
                _ when str.Contains("days")
                    => TimeSpan.FromDays(number),
                _
                    => TimeSpan.Zero
            };

            return DateOnly.FromDateTime(DateTime.Now.Subtract(span));
        }
    }
}