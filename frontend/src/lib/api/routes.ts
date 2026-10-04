const enc = encodeURIComponent

export const routes = {
	general: {
		ping:   () => '/api/general',
		health: () => '/api/general/health',
	},

	source: {
		list:   () => '/api/source',
		get:    (id: string) => `/api/source/${enc(id)}`,
		toggle: (id: string) => `/api/source/${enc(id)}/toggle`,
	},

	manga: {
		list:    (sourceId: string, page: number, pageSize: number) =>
			`/api/manga/${enc(sourceId)}?page=${page}&pageSize=${pageSize}`,
		get:     (sourceId: string, mangaId: string) =>
			`/api/manga/${enc(sourceId)}/${enc(mangaId)}`,
		chapter: (sourceId: string, mangaId: string, chapterId: string) =>
			`/api/manga/${enc(sourceId)}/${enc(mangaId)}/${enc(chapterId)}`,
		refresh: (sourceId: string, mangaId: string) =>
			`/api/manga/${enc(sourceId)}/${enc(mangaId)}/refresh`,
	},

	library: {
		get:      (userId: string) =>
			`/api/library/${userId}`,
		entry:    (userId: string, sourceId: string, mangaId: string) =>
			`/api/library/${userId}/${sourceId}/${mangaId}`,
		progress: (userId: string, sourceId: string, mangaId: string, chapter: number) =>
			`/api/library/${userId}/${sourceId}/${mangaId}?lastChapterRead=${chapter}`,
		refresh:  (userId: string) =>
			`/api/library/${userId}/refresh`,
	},
} as const
