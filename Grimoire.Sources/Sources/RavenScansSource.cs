using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

// "img.ts-main-image"
public class RavenScansSource : ArenaScansSource, IGrimoireSource {
    public new string Name
        => "Raven Scans";

    public new string Url
        => "https://ravenscans.com";

    public new string Icon
        => "https://i0.wp.com/ravenscans.com/wp-content/uploads/2022/12/cropped-33.png";

    public RavenScansSource(ILogger<RavenScansSource> logger, HtmlParser htmlParser)
        : base(logger, htmlParser) { }
}