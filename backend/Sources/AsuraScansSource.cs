using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources;

public sealed partial class AsuraScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<AsuraScansSource> logger)
    : WordPressSource(scrapingHandler, metadataProviders, logger) {
    public override string Name
        => "Asura Scans";

    public override string Url
        => "https://asuracomic.net/";

    public override string Icon
        => $"{Url}/images/logo.webp";
}