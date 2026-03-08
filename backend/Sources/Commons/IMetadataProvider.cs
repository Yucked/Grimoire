using Grimoire.Sources.MetadataProviders;

namespace Grimoire.Sources.Commons;

public interface IMetadataProvider {
    Task<MetadataResult?> FindMangaAsync(string title);
}