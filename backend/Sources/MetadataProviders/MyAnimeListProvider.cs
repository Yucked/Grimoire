using System.Text.Json;
using Grimoire.Objects;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources.MetadataProviders;

public sealed class MyAnimeListProvider(
    HttpClient httpClient,
    ILogger<MyAnimeListProvider> logger,
    IConfiguration configuration) : IMetadataProvider {
    private const string URL
        = "https://api.myanimelist.net/v2";

    private const string FIELDS
        = "id,title,alternative_titles,start_date,mean,status,genres,authors{first_name,last_name}";

    public async Task<MetadataResult?> FindMangaAsync(string mangaName) {
        try {
            using var requestMessage =
                RequestMessage($"{URL}/manga?q={Uri.EscapeDataString(mangaName)}&limit=5&fields={FIELDS}");
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            await using var stream = await responseMessage.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);

            var matched = document.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(x => x.GetProperty("node"))
                .FirstOrDefault(x => {
                    var name = x.GetProperty("title").GetString()!;
                    var nameId = name.GetIdFromName();
                    var titleId = mangaName.GetIdFromName();
                    var similarity = name.Similarity(mangaName);

                    return nameId == titleId ||
                           similarity > 85.0;
                });

            if (string.IsNullOrWhiteSpace(matched.GetRawText())) {
                return null;
            }

            var metadata = new MetadataResult();
            if (matched.TryGetProperty("authors", out var authElm)) {
                foreach (var elm in authElm.EnumerateArray()) {
                    var role = elm.TryGetProperty("role", out var rVal)
                        ? rVal.GetString()
                        : string.Empty;

                    var firstName = elm
                        .GetProperty("node")
                        .TryGetProperty("first_name", out var fn)
                        ? fn.GetString()
                        : string.Empty;

                    var lastName = elm
                        .GetProperty("node")
                        .TryGetProperty("first_name", out var ln)
                        ? ln.GetString()
                        : string.Empty;

                    var name = $"{firstName} {lastName}".Trim();
                    switch (role) {
                        case "Art":
                            metadata.Artists.Add(name);
                            break;
                        case "Story":
                            metadata.Authors.Add(name);
                            break;
                    }
                }
            }

            if (matched.TryGetProperty("genres", out var genElm)) {
                metadata.Genres = genElm
                    .EnumerateArray()
                    .Select(x => x.GetProperty("name").GetString())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray()!;
            }

            if (matched.TryGetProperty("alternative_titles", out var altElm)) {
                if (altElm.TryGetProperty("synonyms", out var synElm)) {
                    metadata.Aliases.AddRange(synElm
                        .EnumerateArray()
                        .Select(x => x.GetString()!));
                }

                foreach (var lang in new[] { "en", "ja" }) {
                    if (altElm.TryGetProperty(lang, out var t)
                        && !string.IsNullOrWhiteSpace(t.GetString())) {
                        metadata.Aliases.Add(t.GetString()!);
                    }
                }
            }

            metadata.Status = (matched.TryGetProperty("status", out var statusElm)
                    ? statusElm.GetString()
                    : string.Empty)
                switch {
                    "currently_publishing" => MangaStatus.OnGoing,
                    "finished"             => MangaStatus.Completed,
                    "on_hiatus"            => MangaStatus.Hiatus,
                    "discontinued"         => MangaStatus.Cancelled,
                    _                      => MangaStatus.OnGoing
                };

            if (matched.TryGetProperty("start_date", out var startDateElm) &&
                DateOnly.TryParse(startDateElm.GetString(), out var date)) {
                metadata.ReleasedOn = date;
            }

            if (matched.TryGetProperty("mean", out var ratingsElm)) {
                metadata.Ratings = (float)ratingsElm.GetDouble();
            }

            return metadata;
        }
        catch (Exception ex) {
            logger.LogError(ex, "MyAnimeList lookup failed for {mangaName}", mangaName);
            return null;
        }
    }

    private HttpRequestMessage RequestMessage(string url) {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("X-MAL-CLIENT-ID", configuration.GetValue<string>("MyAnimeList:ClientId"));
        return requestMessage;
    }
}