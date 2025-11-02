using Grimoire.Objects;
using Raven.Client.Documents.Indexes;

namespace Grimoire.Handlers;

public class MangaSearchIndex : AbstractIndexCreationTask<MangaObject> {
    public MangaSearchIndex() {
        Map = mangas => from manga in mangas
                        select new {
                            manga.Title,
                            manga.Summary,
                            manga.Aliases,
                            manga.SourceId,
                            manga.Genres,
                            manga.Authors
                        };
        Index(x => x.Title, FieldIndexing.Search);
        Index(x => x.Summary, FieldIndexing.Search);
        Index(x => x.Aliases, FieldIndexing.Search);
        Index(x => x.Genres, FieldIndexing.Search);
        Index(x => x.Authors, FieldIndexing.Search);

        Store(x => x.Title, FieldStorage.Yes);
        Store(x => x.Summary, FieldStorage.Yes);
    }
}