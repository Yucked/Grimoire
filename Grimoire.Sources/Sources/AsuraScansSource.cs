using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

// "img.alignnone"
public class AsuraScansSource : ArenaScansSource, IGrimoireSource {
    public new string Name
        => "Asura Scans";

    public new string Url
        => "https://www.asurascans.com";

    public new string Icon
        => $"{Url}/wp-content/uploads/2021/03/Group_1.png";

    private readonly HtmlParser _htmlParser;
    private readonly ILogger<AsuraScansSource> _logger;

    public AsuraScansSource(ILogger<AsuraScansSource> logger, HtmlParser htmlParser)
        : base(logger, htmlParser) {
        _logger = logger;
        _htmlParser = htmlParser;
    }
}