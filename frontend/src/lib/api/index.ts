import type {ApiResponse, ChapterObject, HealthStatus, MangaObject, SourceObject, UserObject} from './types'
import {routes} from './routes'

const BASE = import.meta.env.PUBLIC_API_URL ?? ''
const MINIO = import.meta.env.PUBLIC_MINIO_URL ?? 'http://localhost:9000'
const USER = import.meta.env.PUBLIC_GRIMOIRE_USER ?? 'Grimoire'

// ── URL helpers ───────────────────────────────────────────────────────────────

/** Standard base64 → URL-safe (replaces + / =) */
export const toUrlSafe = (b64: string) =>
    b64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '~')

/** URL-safe base64 → standard base64 */
export const fromUrlSafe = (s: string) =>
    s.replace(/-/g, '+').replace(/_/g, '/').replace(/~/g, '=')

/** UTF-8 string → lowercase hex (matches C# GetIdFromName / Convert.ToHexStringLower) */
const toHexId = (name: string) =>
    Array.from(new TextEncoder().encode(name))
        .map(b => b.toString(16).padStart(2, '0'))
        .join('')

/** Parse composite "sourceId/mangaId" key → { sourceId, mangaId } */
export const parseLibraryKey = (key: string) => {
    const idx = key.indexOf('/')
    return {sourceId: key.slice(0, idx), mangaId: key.slice(idx + 1)}
}

/** Cover image URL — MinIO if stored, fallback to original source URL */
export const coverUrl = (m: MangaObject): string =>
    m.coverPath ? `${MINIO}/${m.coverPath}` : m.cover

/** Chapter page URL — MinIO path if downloaded (no scheme), source URL otherwise */
export const pageUrl = (path: string): string =>
    path.startsWith('http') ? path : `${MINIO}/${path}`

/** Manga detail page href */
export const mangaHref = (sourceId: string, mangaId: string): string =>
    `/manga/${toUrlSafe(sourceId)}/${toUrlSafe(mangaId)}`

/** Reader page href */
export const readerHref = (sourceId: string, mangaId: string, chapterNumber: string): string =>
    `/read/${toUrlSafe(sourceId)}/${toUrlSafe(mangaId)}/${encodeURIComponent(chapterNumber)}`

// ── Fetch client ──────────────────────────────────────────────────────────────

async function api<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(`${BASE}${path}`, {
        headers: {'Content-Type': 'application/json'},
        ...init
    })
    const body: ApiResponse<T> = await res.json()
    // Throw only when the response is an error AND carries no data (e.g. 404, 400).
    // Allow non-ok responses that still carry data (e.g. 503 health with status info),
    // and allow null data for void responses (add/remove library, update progress).
    if (!res.ok && body.data == null) {
        throw new Error(`${res.status} ${res.statusText} — ${path}`)
    }
    return body.data as T
}

// ── Endpoints ─────────────────────────────────────────────────────────────────

export const ping = () => api<unknown>(routes.general.ping())
export const getHealth = () => api<HealthStatus>(routes.general.health())

export const getSources = () => api<SourceObject[]>(routes.source.list())
export const getSource = (id: string) => api<SourceObject>(routes.source.get(id))
export const toggleSource = (id: string) =>
    api<{ isDisabled: boolean }>(routes.source.toggle(id), {method: 'PATCH'})

export const getMangas = (sourceId: string, page = 0, pageSize = 24) =>
    api<MangaObject[]>(routes.manga.list(sourceId, page, pageSize))
export const getManga = (sourceId: string, mangaId: string) =>
    api<MangaObject>(routes.manga.get(sourceId, mangaId))
export const getChapter = (sourceId: string, mangaId: string, chapterId: string) =>
    api<ChapterObject>(routes.manga.chapter(sourceId, mangaId, chapterId))
export const refreshManga = (sourceId: string, mangaId: string) =>
    api<MangaObject>(routes.manga.refresh(sourceId, mangaId), {method: 'POST'})

const userId = toHexId(USER)
export const getLibrary = () => api<UserObject>(routes.library.get(userId))
export const addToLibrary = (sourceId: string, mangaId: string) =>
    api<void>(routes.library.entry(userId, sourceId, mangaId), {method: 'POST'})
export const removeFromLibrary = (sourceId: string, mangaId: string) =>
    api<void>(routes.library.entry(userId, sourceId, mangaId), {method: 'DELETE'})
export const updateProgress = (sourceId: string, mangaId: string, chapter: number) =>
    api<void>(routes.library.progress(userId, sourceId, mangaId, chapter), {method: 'PATCH'})
export const refreshLibrary = () =>
    api<void>(routes.library.refresh(userId), {method: 'POST'})

// Re-export types for convenience
export type {MangaObject, ChapterObject, SourceObject, UserObject, HealthStatus} from './types'
