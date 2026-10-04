export type MangaStatus = 'OnGoing' | 'Completed' | 'Hiatus' | 'Cancelled'
export type MangaType   = 'Manga' | 'Manhwa' | 'Manhua' | 'Novel'

export interface MangaObject {
	title:        string
	sourceId:     string
	sourceUrl:    string
	authors:      string[]
	artists:      string[]
	aliases:      string[]
	genres:       string[]
	summary:      string
	cover:        string       // original URL from source
	coverPath:    string       // MinIO path: "{sourceId}/{mangaId}/cover.ext"
	status:       MangaStatus
	type:         MangaType
	ratings:      number
	updatedAt:    string
	releasedOn:   string
	chapters:     ChapterObject[]
	id:           string       // "{sourceId}/{base64(title)}"
}

export interface ChapterObject {
	number:       string
	title:        string
	sourceUrl:    string
	releasedOn:   string
	isDownloaded: boolean
	pages:        string[]   // resolved image URLs (MinIO paths if downloaded, source URLs otherwise)
}

export interface SourceObject {
	name:       string
	url:        string
	favicon:    string
	updatedOn:  string
	isDisabled: boolean
	id:         string
}

export interface UserObject {
	username:  string
	library:   Record<string, number>   // {"sourceId/mangaId": chapterProgress}
	createdAt: string
	id:        string
}

export interface ApiResponse<T> {
	statusCode: number
	data:       T | null
}

export interface HealthStatus {
	ravendb: string
	minio:   string
}
