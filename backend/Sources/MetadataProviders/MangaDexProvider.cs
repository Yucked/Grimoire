using System.Text.Json;
using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources.MetadataProviders;

public sealed class MangaDexProvider(
    HttpClient httpClient,
    ScrapingHandler scrapingHandler,
    ILogger<MangaDexProvider> logger) : IMetadataProvider {
    private const string URL = "https://api.mangadex.org";

    public async Task<MetadataResult?> FindMangaAsync(string mangaName) {
        try {
            var responseMessage = await httpClient.GetAsync(
                $"{URL}/manga?title={Uri.EscapeDataString(mangaName)}&limit=5&includes[]=author&includes[]=artist");
            responseMessage.EnsureSuccessStatusCode();

            await using var searchStream = await responseMessage.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(searchStream);
            var root = document.RootElement.GetProperty("data");

            var matched = root
                .EnumerateArray()
                .FirstOrDefault(y => {
                    var title = y.GetProperty("attributes")
                        .GetProperty("title")
                        .GetProperty("ja-ro")
                        .GetString()!;
                    var titleId = title.GetIdFromName();
                    var mangaId = mangaName.GetIdFromName();
                    var similarity = mangaName.Similarity(title);

                    return titleId == mangaId ||
                           similarity > 85.0;
                });

            if (string.IsNullOrEmpty(matched.GetRawText())) {
                return null;
            }

            var metadata = new MetadataResult();
            if (matched.TryGetProperty("relationships", out var relationships)) {
                foreach (var elm in relationships.EnumerateArray()) {
                    var type = elm.GetProperty("type").GetString()!;
                    var name = elm
                        .GetProperty("attributes")
                        .GetProperty("name")
                        .GetString()!;

                    switch (type) {
                        case "author":
                            metadata.Authors.Add(name);
                            break;

                        case "artist":
                            metadata.Artists.Add(name);
                            break;
                    }
                }
            }

            if (matched.TryGetProperty("tags", out var tags)) {
                var genres = tags
                    .EnumerateArray()
                    .Select(x => x.GetProperty("attributes")
                        .GetProperty("name")
                        .GetProperty("en")
                        .GetString()!);

                metadata.Genres.AddRange(genres);
            }

            if (matched.TryGetProperty("altTitles", out var altTitles)) {
                metadata.Aliases.AddRange(altTitles
                    .EnumerateObject()
                    .Select(x => x.Value.GetString()!));
            }

            if (matched.TryGetProperty("status", out var status)) {
                metadata.Status = status.GetString() switch {
                    "ongoing"   => MangaStatus.OnGoing,
                    "completed" => MangaStatus.Completed,
                    "hiatus"    => MangaStatus.Hiatus,
                    "cancelled" => MangaStatus.Cancelled,
                    _           => MangaStatus.OnGoing
                };
            }

            if (matched.TryGetProperty("createdAt", out var createdAt)) {
                metadata.ReleasedOn = DateOnly.Parse(createdAt.GetString()!);
            }

            var mangaId = matched.GetProperty("id").GetString()!;
            var doc = await scrapingHandler.GetJsonDocumentAsync($"{URL}/statistics/manga/{mangaId}");
            metadata.Ratings = (float)doc.RootElement
                .GetProperty("statistics")
                .GetProperty(mangaId)
                .GetProperty("rating")
                .GetProperty("average")
                .GetDouble();

            return metadata;
        }
        catch (Exception ex) {
            logger.LogError(ex, "MangaDex lookup failed for {mangaName}", mangaName);
            return null;
        }
    }
}