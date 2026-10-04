import { fromUrlSafe, getMangas, getSource } from '$lib/api'

export const load = async ({ params }: { params: Record<string, string> }) => {
	const sourceId = fromUrlSafe(params.sourceId)
	const [source, mangas] = await Promise.all([
		getSource(sourceId).catch(() => null),
		getMangas(sourceId, 0, 24)
	])
	return { source, mangas: mangas ?? [], sourceId }
}
