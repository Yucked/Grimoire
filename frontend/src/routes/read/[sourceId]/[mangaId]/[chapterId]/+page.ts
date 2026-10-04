import { getManga, getChapter, fromUrlSafe } from '$lib/api'

export const load = async ({ params }: { params: Record<string, string> }) => {
	const sourceId  = fromUrlSafe(params.sourceId)
	const mangaId   = fromUrlSafe(params.mangaId)
	const chapterId = decodeURIComponent(params.chapterId)

	// Fetch manga (for chapter navigation) and chapter (for pages) in parallel
	const [manga, chapter] = await Promise.all([
		getManga(sourceId, mangaId),
		getChapter(sourceId, mangaId, chapterId)
	])

	const sortedChapters = [...(manga?.chapters ?? [])].sort(
		(a, b) => parseFloat(a.number) - parseFloat(b.number)
	)

	const currentIdx  = sortedChapters.findIndex(c => c.number === chapterId)
	const prevChapter = currentIdx > 0 ? sortedChapters[currentIdx - 1] : null
	const nextChapter = currentIdx < sortedChapters.length - 1 ? sortedChapters[currentIdx + 1] : null

	return { manga, chapter, sourceId, mangaId, chapterId, prevChapter, nextChapter }
}
