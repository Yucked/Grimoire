using System.Text.Json;
using Grimoire.Objects;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources.MetadataProviders;

public sealed class MyAnimeListProvider(
    HttpClient httpClient,
    ILogger<MyAnimeListProvider> logger) : IMetadataProvider {
    public async Task<MetadataResult?> FindMangaAsync(string title) {
        try {
            const string fields = "id,title,alternative_titles,start_date,mean,status,genres,authors{first_name,last_name}";
            using var response = await httpClient.GetAsync(
                $"manga?q={Uri.EscapeDataString(title)}&limit=5&fields={fields}");
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            JsonElement? matched = null;
            foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray()) {
                var node = item.GetProperty("node");
                if (TitleMatches(node, title)) {
                    matched = node;
                    break;
                }
            }

            if (matched is null) return null;

            var attrs = matched.Value;
            var id = attrs.GetProperty("id").GetInt32().ToString();

            var authors = new List<string>();
            var artists = new List<string>();
            if (attrs.TryGetProperty("authors", out var authorsEl)) {
                foreach (var entry in authorsEl.EnumerateArray()) {
                    var node = entry.GetProperty("node");
                    var first = node.TryGetProperty("first_name", out var fn) ? fn.GetString() ?? "" : "";
                    var last  = node.TryGetProperty("last_name",  out var ln) ? ln.GetString() ?? "" : "";
                    var name  = $"{first} {last}".Trim();
                    var role  = entry.TryGetProperty("role", out var r) ? r.GetString() : null;
                    if (role == "art") artists.Add(name);
                    else               authors.Add(name);
                }
            }

            var genres = new List<string>();
            if (attrs.TryGetProperty("genres", out var genresEl))
                foreach (var g in genresEl.EnumerateArray()) {
                    var name = g.GetProperty("name").GetString();
                    if (name is not null) genres.Add(name);
                }

            var aliases = new List<string>();
            if (attrs.TryGetProperty("alternative_titles", out var altTitles)) {
                if (altTitles.TryGetProperty("synonyms", out var synonyms))
                    foreach (var s in synonyms.EnumerateArray()) {
                        var v = s.GetString();
                        if (v is not null) aliases.Add(v);
                    }
                foreach (var lang in new[] { "en", "ja" }) {
                    if (altTitles.TryGetProperty(lang, out var t)) {
                        var v = t.GetString();
                        if (!string.IsNullOrEmpty(v)) aliases.Add(v);
                    }
                }
            }

            var status = ParseStatus(
                attrs.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null);

            var releasedOn = default(DateOnly);
            if (attrs.TryGetProperty("start_date", out var startDate)
                && startDate.ValueKind == JsonValueKind.String)
                DateOnly.TryParse(startDate.GetString(), out releasedOn);

            var ratings = 0f;
            if (attrs.TryGetProperty("mean", out var mean) && mean.ValueKind == JsonValueKind.Number)
                ratings = (float)mean.GetDouble();

            return new MetadataResult(id, authors, artists, genres, aliases, status, releasedOn, ratings);
        }
        catch (Exception ex) {
            logger.LogError(ex, "MyAnimeList lookup failed for {Title}", title);
            return null;
        }
    }

    private static bool TitleMatches(JsonElement node, string search) {
        var lower = search.ToLowerInvariant();
        if (node.GetProperty("title").GetString()?.ToLowerInvariant().Contains(lower) is true) return true;
        if (!node.TryGetProperty("alternative_titles", out var alt)) return false;
        foreach (var lang in new[] { "en", "ja" })
            if (alt.TryGetProperty(lang, out var t) && t.GetString()?.ToLowerInvariant().Contains(lower) is true)
                return true;
        if (alt.TryGetProperty("synonyms", out var synonyms))
            foreach (var s in synonyms.EnumerateArray())
                if (s.GetString()?.ToLowerInvariant().Contains(lower) is true)
                    return true;
        return false;
    }

    private static MangaStatus ParseStatus(string? status) => status switch {
        "currently_publishing" => MangaStatus.OnGoing,
        "finished"             => MangaStatus.Completed,
        "on_hiatus"            => MangaStatus.Hiatus,
        "discontinued"         => MangaStatus.Cancelled,
        _                      => MangaStatus.OnGoing
    };
}
