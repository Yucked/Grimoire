<script lang="ts">
	import { addToLibrary, removeFromLibrary, refreshManga, coverUrl, toUrlSafe } from '$lib/api'
	import ChapterList from '$lib/components/ChapterList.svelte'
	import LazyImage from '$lib/components/LazyImage.svelte'
	import { ChevronLeft, Star, RefreshCw } from 'lucide-svelte'
	import type { MangaObject, UserObject } from '$lib/api/types'
	import { toast } from '$lib/stores.svelte'

	let { data }: {
		data: { manga: MangaObject; user: UserObject | null; progress: number | null; sourceId: string; mangaId: string }
	} = $props()

	let inLibrary      = $state(false)
	let progress       = $state<number | null>(null)
	let libLoading     = $state(false)
	let refreshLoading = $state(false)
	let refreshed      = $state(false)
	let summaryOpen    = $state(false)

	$effect(() => {
		inLibrary = data.progress !== null
		progress  = data.progress
	})

	const manga     = $derived(data.manga)
	const sourceId  = $derived(data.sourceId)
	const mangaId   = $derived(data.mangaId)
	const totalChapters = $derived(manga?.chapters?.length ?? 0)
	const progressPct   = $derived(totalChapters > 0 ? Math.min(100, ((progress ?? 0) / totalChapters) * 100) : 0)

	const GENRE_COLORS = [
		['rgba(201,123,42,.12)','#c97b2a'],  ['rgba(56,189,248,.12)','#38bdf8'],
		['rgba(139,92,246,.12)','#a78bfa'],  ['rgba(52,211,153,.12)','#34d399'],
		['rgba(251,146,60,.12)','#fb923c'],  ['rgba(236,72,153,.12)','#f472b6'],
	]
	function genreColor(g: string) {
		return GENRE_COLORS[[...g].reduce((a, c) => a + c.charCodeAt(0), 0) % GENRE_COLORS.length]
	}

	async function handleRefresh() {
		if (refreshLoading) return
		refreshLoading = true; refreshed = false
		try {
			await refreshManga(sourceId, mangaId)
			refreshed = true
			toast('Chapters updated', 'success')
			setTimeout(() => refreshed = false, 2000)
		} catch (e) {
			toast(e instanceof Error ? e.message : 'Refresh failed', 'error')
		} finally { refreshLoading = false }
	}

	async function toggleLibrary() {
		if (libLoading) return
		libLoading = true
		try {
			if (inLibrary) {
				await removeFromLibrary(sourceId, mangaId)
				inLibrary = false; progress = null
				toast('Removed from library', 'info')
			} else {
				await addToLibrary(sourceId, mangaId)
				inLibrary = true; progress = 0
				toast('Added to library', 'success')
			}
		} catch (e) {
			toast(e instanceof Error ? e.message : 'Action failed', 'error')
		} finally { libLoading = false }
	}
</script>

<svelte:head><title>{manga?.title ?? 'Manga'} — Grimoire</title></svelte:head>

{#if manga}
	<!-- Topbar with back -->
	<div class="sticky top-0 z-20 flex items-center gap-3 px-5 py-3 border-b"
	     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
		<a href="/sources/{toUrlSafe(sourceId)}"
		   class="flex items-center gap-1.5 text-xs transition-colors"
		   style="color:var(--color-text-dim)">
			<ChevronLeft size={14} />
			Back
		</a>
		<span style="color:var(--color-border)">·</span>
		<span class="text-sm truncate" style="color:var(--color-text-dim); font-family:var(--font-serif); font-style:italic">
			{manga.title}
		</span>
	</div>

	<div class="p-5 sm:p-6 pb-24 sm:pb-6">

		<!-- Hero -->
		<div class="relative overflow-hidden rounded-[12px] mb-6">
			<!-- Blurred bg -->
			<div class="absolute inset-0 scale-110 origin-center pointer-events-none"
			     style="background:url('{coverUrl(manga)}') center/cover no-repeat; filter:blur(50px) saturate(1.3); opacity:0.13"></div>
			<div class="absolute inset-0 pointer-events-none"
			     style="background:linear-gradient(to right, rgba(12,11,9,.98) 0%, rgba(12,11,9,.8) 50%, rgba(12,11,9,.5) 100%)"></div>

			<div class="relative flex flex-col sm:flex-row gap-5 p-5 sm:p-7">
				<!-- Cover -->
				<div class="w-28 sm:w-36 shrink-0 mx-auto sm:mx-0">
					<div class="overflow-hidden rounded-[8px] shadow-2xl" style="aspect-ratio:2/3; border:1px solid rgba(201,123,42,.2)">
						<LazyImage src={coverUrl(manga)} alt={manga.title} class="w-full h-full" />
					</div>
				</div>

				<!-- Meta -->
				<div class="flex-1 min-w-0">
					<h1 class="mb-2 leading-tight" style="font-family:var(--font-serif); font-size:22px; font-weight:500; color:var(--color-text)">
						{manga.title}
					</h1>

					<!-- Status + type -->
					<div class="flex flex-wrap gap-2 mb-3">
						<span class="text-[11px] font-medium px-2.5 py-1 rounded-[6px] border"
						      style="background:rgba(201,123,42,.1); color:var(--color-amber); border-color:rgba(201,123,42,.25)">
							{manga.status}
						</span>
						<span class="text-[11px] px-2.5 py-1 rounded-[6px] border"
						      style="background:var(--color-ink4); color:var(--color-text-dim); border-color:var(--color-border)">
							{manga.type}
						</span>
					</div>

					<!-- Ratings / Author -->
					<div class="flex flex-wrap items-center gap-x-4 gap-y-1.5 mb-3 text-sm">
						{#if manga.ratings > 0}
							<span class="flex items-center gap-1" style="color:var(--color-text-dim)">
								<Star size={12} fill="var(--color-amber)" color="var(--color-amber)" />
								<span style="color:var(--color-text)">{manga.ratings.toFixed(1)}</span>
							</span>
						{/if}
						{#if manga.authors?.length > 0}
							<span style="color:var(--color-text-dim); font-size:12px">{manga.authors.join(', ')}</span>
						{/if}
					</div>

					<!-- Genres -->
					{#if manga.genres?.length > 0}
						<div class="flex flex-wrap gap-1.5 mb-4">
							{#each manga.genres as genre}
								{@const [bg, fg] = genreColor(genre)}
								<span class="text-[10px] font-medium px-2 py-[3px] rounded-[5px]"
								      style="background:{bg}; color:{fg}">
									{genre}
								</span>
							{/each}
						</div>
					{/if}

					<!-- Summary -->
					{#if manga.summary}
						<p class="text-[13px] leading-relaxed mb-4 {summaryOpen ? '' : 'line-clamp-3'}"
						   style="color:var(--color-text-dim)">
							{manga.summary}
						</p>
						{#if manga.summary.length > 180}
							<button onclick={() => summaryOpen = !summaryOpen}
							        class="text-xs mb-4 transition-colors"
							        style="color:var(--color-amber)">
								{summaryOpen ? 'Show less' : 'Read more'}
							</button>
						{/if}
					{/if}

					<!-- Actions -->
					<div class="flex flex-wrap items-center gap-2 sm:gap-3">
						<button onclick={toggleLibrary} disabled={libLoading}
						        class="overlay-btn {inLibrary ? 'secondary' : ''} disabled:opacity-50 flex-1 sm:flex-none justify-center">
							{libLoading ? '…' : inLibrary ? 'Remove from library' : 'Add to library'}
						</button>
						<button onclick={handleRefresh} disabled={refreshLoading}
						        class="overlay-btn secondary disabled:opacity-50"
						        title="Fetch latest chapters">
							<RefreshCw size={11} class={refreshLoading ? 'animate-spin' : ''} />
							{refreshed ? 'Updated!' : 'Refresh'}
						</button>

						{#if inLibrary && totalChapters > 0}
							<div class="flex-1 min-w-36">
								<div class="flex justify-between text-[11px] mb-1" style="color:var(--color-text-dim)">
									<span>Progress</span>
									<span style="color:var(--color-text)">{Math.floor(progress ?? 0)} / {totalChapters}</span>
								</div>
								<div class="h-[2px] rounded-full" style="background:var(--color-border)">
									<div class="h-full rounded-full transition-all duration-300"
									     style="width:{progressPct}%; background:var(--color-amber)"></div>
								</div>
							</div>
						{/if}
					</div>
				</div>
			</div>
		</div>

		<!-- Chapter list -->
		<div class="flex items-baseline gap-2.5 mb-3.5">
			<span style="font-family:var(--font-serif); font-size:13px; color:var(--color-text-dim); font-style:italic">Chapters</span>
			<span style="font-size:11px; color:var(--color-text-muted)">{totalChapters} total</span>
		</div>
		<div class="rounded-[10px] overflow-hidden border" style="border-color:var(--color-border); background:var(--color-ink3)">
			<ChapterList chapters={manga.chapters ?? []} {sourceId} {mangaId} lastChapterRead={progress ?? 0} />
		</div>
	</div>
{/if}
