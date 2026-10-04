<script lang="ts">
	import { getMangas } from '$lib/api'
	import MangaCard from '$lib/components/MangaCard.svelte'
	import { ExternalLink, Search } from 'lucide-svelte'
	import type { MangaObject, MangaStatus, SourceObject } from '$lib/api/types'
	import { toast } from '$lib/stores.svelte'

	let { data }: { data: { source: SourceObject | null; mangas: MangaObject[]; sourceId: string } } = $props()

	let mangas      = $state<MangaObject[]>([])
	let page        = $state(0)
	let loading     = $state(false)
	let hasMore     = $state(false)
	let searchQuery = $state('')
	let statusFilter = $state<MangaStatus | 'All'>('All')

	const STATUSES: Array<MangaStatus | 'All'> = ['All', 'OnGoing', 'Completed', 'Hiatus', 'Cancelled']

	$effect(() => {
		mangas  = data.mangas
		hasMore = data.mangas.length === 24
		page    = 0
	})

	const filtered = $derived(mangas.filter(m => {
		if (statusFilter !== 'All' && m.status !== statusFilter) return false
		if (searchQuery && !m.title.toLowerCase().includes(searchQuery.toLowerCase())) return false
		return true
	}))

	async function loadMore() {
		if (loading) return
		loading = true; page++
		try {
			const next = await getMangas(data.sourceId, page, 24)
			mangas = [...mangas, ...(next ?? [])]
			hasMore = (next?.length ?? 0) === 24
		} catch (e) {
			toast(e instanceof Error ? e.message : 'Failed to load more', 'error')
		} finally { loading = false }
	}
</script>

<svelte:head><title>{data.source?.name ?? 'Source'} — Grimoire</title></svelte:head>

<!-- Topbar -->
<div class="sticky top-0 z-20 flex items-center gap-3 px-5 py-3.5 border-b"
     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
	{#if data.source?.favicon}
		<img src={data.source.favicon} alt={data.source.name}
		     class="w-6 h-6 rounded object-contain" style="background:rgba(255,255,255,.05)" />
	{/if}
	<h1 style="font-family:var(--font-serif); font-size:18px; font-weight:500; color:var(--color-text); letter-spacing:0.01em">
		{data.source?.name ?? data.sourceId}
	</h1>
	{#if data.source?.url}
		<a href={data.source.url} target="_blank" rel="noopener noreferrer"
		   class="flex items-center gap-1 text-xs transition-colors ml-1"
		   style="color:var(--color-amber)">
			<ExternalLink size={11} />
		</a>
	{/if}
</div>

<!-- Filter bar -->
<div class="sticky top-[53px] z-10 flex items-center gap-2 px-4 py-2 border-b"
     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
	<span class="mr-auto text-[11px] uppercase tracking-[0.05em] hidden sm:block" style="color:var(--color-text-muted)">
		{filtered.length} titles
	</span>
	<div class="flex items-center gap-1.5 px-2.5 py-[5px] rounded-[7px] border transition-colors"
	     style="background:var(--color-ink); border-color:var(--color-border)"
	     onfocusin={(e) => (e.currentTarget.style.borderColor='var(--color-amber-dim)')}
	     onfocusout={(e) => (e.currentTarget.style.borderColor='var(--color-border)')}>
		<Search size={12} color="var(--color-text-muted)" />
		<input bind:value={searchQuery} placeholder="Search…"
		       class="bg-transparent border-none outline-none text-xs w-28 sm:w-36"
		       style="color:var(--color-text); font-family:var(--font-sans)" />
	</div>
	<div class="relative">
		<select bind:value={statusFilter} class="tselect pr-6">
			{#each STATUSES as s}
				<option value={s}>{s === 'OnGoing' ? 'Ongoing' : s}</option>
			{/each}
		</select>
		<span class="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none text-[10px]"
		      style="color:var(--color-text-muted)">▾</span>
	</div>
</div>

<div class="p-5 pb-24 sm:pb-5">
	{#if filtered.length === 0}
		<p class="text-center py-12 text-sm" style="color:var(--color-text-dim)">
			{searchQuery || statusFilter !== 'All' ? 'No manga match your filters.' : 'No manga found for this source.'}
		</p>
	{:else}
		<div class="grid gap-3.5 mb-6" style="grid-template-columns:repeat(auto-fill,minmax(140px,1fr))">
			{#each filtered as manga, i (manga.id)}
				<div style="animation:card-in 0.3s ease-out both; animation-delay:{Math.min(i*25,500)}ms">
					<MangaCard {manga} />
				</div>
			{/each}
		</div>

		{#if hasMore}
			<div class="flex justify-center">
				<button onclick={loadMore} disabled={loading}
				        class="tbtn disabled:opacity-50">
					{loading ? 'Loading…' : 'Load more'}
				</button>
			</div>
		{/if}
	{/if}
</div>

<style>
	input::placeholder { color: var(--color-text-muted); }
</style>
