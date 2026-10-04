import { getManga, getLibrary, fromUrlSafe } from '$lib/api'

export const load = async ({ params }: { params: Record<string, string> }) => {
	const sourceId = fromUrlSafe(params.sourceId)
	const mangaId  = fromUrlSafe(params.mangaId)

	const [manga, user] = await Promise.all([
		getManga(sourceId, mangaId),
		getLibrary().catch(() => null)
	])

	const progress = user?.library[`${sourceId}/${mangaId}`] ?? null

	return { manga, user, progress, sourceId, mangaId }
}
