namespace Grimoire.Integrations;

public interface IMetadataProvider {
    Task<MetadataResult?> FindMangaAsync(string title);
}
