<script lang="ts">
	import { updateProgress, toUrlSafe, pageUrl, readerHref } from '$lib/api'
	import LazyImage from '$lib/components/LazyImage.svelte'
	import { ChevronLeft, ChevronRight, ArrowLeft } from 'lucide-svelte'
	import type { ChapterObject, MangaObject } from '$lib/api/types'

	let { data }: {
		data: {
			manga: MangaObject
			chapter: ChapterObject
			sourceId: string
			mangaId: string
			chapterId: string
			prevChapter: { number: string } | null
			nextChapter: { number: string } | null
		}
	} = $props()

	const chapter     = $derived(data.chapter)
	const manga       = $derived(data.manga)
	const sourceId    = $derived(data.sourceId)
	const mangaId     = $derived(data.mangaId)
	const prevChapter = $derived(data.prevChapter)
	const nextChapter = $derived(data.nextChapter)
	const prevHref    = $derived(prevChapter ? readerHref(sourceId, mangaId, prevChapter.number) : null)
	const nextHref    = $derived(nextChapter ? readerHref(sourceId, mangaId, nextChapter.number) : null)

	const pages = $derived(chapter?.pages ?? [])

	let barVisible  = $state(true)
	let lastScrollY = $state(0)
	let progressSaved = $state(false)

	function handleScroll() {
		const y = window.scrollY
		barVisible  = y < lastScrollY || y < 80
		lastScrollY = y
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'ArrowLeft'  && prevHref) { location.href = prevHref }
		if (e.key === 'ArrowRight' && nextHref) { location.href = nextHref }
	}

	// Auto-save progress when last page scrolls into view
	$effect(() => {
		if (typeof IntersectionObserver === 'undefined' || pages.length === 0) return

		// Run after DOM update
		const timer = setTimeout(() => {
			const allPages = document.querySelectorAll('.reader-page')
			const lastPage = allPages.item(allPages.length - 1)
			if (!lastPage) return

			const observer = new IntersectionObserver(entries => {
				if (entries[0]?.isIntersecting && !progressSaved) {
					progressSaved = true
					const chNum = parseFloat(data.chapterId)
					updateProgress(sourceId, mangaId, chNum).catch(console.error)
				}
			}, { threshold: 0.3 })

			observer.observe(lastPage)
			return () => observer.disconnect()
		}, 500)

		return () => clearTimeout(timer)
	})
</script>

<svelte:head>
	<title>Ch. {data.chapterId} — {manga?.title ?? 'Reader'}</title>
</svelte:head>

<svelte:window onscroll={handleScroll} onkeydown={handleKeydown} />

<!-- Sticky top bar -->
<div
	class="fixed top-0 inset-x-0 z-50 transition-transform duration-200"
	class:-translate-y-full={!barVisible}
>
	<div class="flex items-center gap-3 bg-bg/95 backdrop-blur border-b border-raised px-4 py-2.5">
		<a
			href="/manga/{toUrlSafe(sourceId)}/{toUrlSafe(mangaId)}"
			class="text-muted hover:text-white transition-colors shrink-0"
			aria-label="Back to manga"
		>
			<ArrowLeft size={18} />
		</a>

		<span class="flex-1 text-sm font-medium text-white text-center truncate">
			{manga?.title} · Chapter {data.chapterId}
		</span>

		<div class="flex items-center gap-0.5 shrink-0">
			{#if prevHref}
				<a href={prevHref} class="rounded p-1 text-muted hover:text-white hover:bg-raised transition-colors" title="Previous chapter">
					<ChevronLeft size={18} />
				</a>
			{:else}
				<span class="rounded p-1 text-muted/30"><ChevronLeft size={18} /></span>
			{/if}
			{#if nextHref}
				<a href={nextHref} class="rounded p-1 text-muted hover:text-white hover:bg-raised transition-colors" title="Next chapter">
					<ChevronRight size={18} />
				</a>
			{:else}
				<span class="rounded p-1 text-muted/30"><ChevronRight size={18} /></span>
			{/if}
		</div>
	</div>
</div>

<!-- Pages -->
<div class="bg-black min-h-screen pt-12">
	<div class="mx-auto max-w-2xl">
		{#if pages.length === 0}
			<div class="flex items-center justify-center h-64 text-muted text-sm">
				Loading pages…
			</div>
		{:else}
			{#each pages as url, i}
				<div class="reader-page w-full">
					<LazyImage
						src={pageUrl(url)}
						alt="Page {i + 1}"
						class="w-full"
					/>
				</div>
			{/each}

			<!-- Chapter end navigation -->
			<div class="flex justify-between items-center p-6 gap-4">
				{#if prevHref}
					<a href={prevHref} class="flex items-center gap-2 rounded-lg bg-surface border border-raised px-4 py-2.5 text-sm text-white hover:bg-raised transition-colors">
						<ChevronLeft size={16} /> Ch. {prevChapter?.number}
					</a>
				{:else}
					<div></div>
				{/if}
				{#if nextHref}
					<a href={nextHref} class="flex items-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-white hover:bg-accent/80 transition-colors">
						Ch. {nextChapter?.number} <ChevronRight size={16} />
					</a>
				{/if}
			</div>
		{/if}
	</div>
</div>
