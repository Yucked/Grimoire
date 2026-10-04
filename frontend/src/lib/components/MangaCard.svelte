<script lang="ts">
	import { coverUrl, mangaHref, readerHref, parseLibraryKey } from '$lib/api'
	import LazyImage from './LazyImage.svelte'
	import type { MangaObject } from '$lib/api/types'

	let {
		manga,
		progress = null
	}: {
		manga:     MangaObject
		progress?: number | null
	} = $props()

	const mangaUrlId    = $derived(parseLibraryKey(manga.id).mangaId)
	const detailHref    = $derived(mangaHref(manga.sourceId, mangaUrlId))
	const totalChapters = $derived(manga.chapters?.length ?? 0)
	const unreadCount   = $derived(Math.max(0, totalChapters - Math.floor(progress ?? 0)))
	const progressPct   = $derived(totalChapters > 0 ? Math.min(100, ((progress ?? 0) / totalChapters) * 100) : 0)

	const nextHref = $derived.by(() => {
		if (!manga.chapters?.length) return null
		const sorted = [...manga.chapters].sort((a, b) => parseFloat(a.number) - parseFloat(b.number))
		const next   = progress != null
			? (sorted.find(c => parseFloat(c.number) > Math.floor(progress!)) ?? sorted[sorted.length - 1])
			: sorted[0]
		if (!next) return null
		return readerHref(manga.sourceId, mangaUrlId, next.number)
	})
</script>

<div class="group relative" style="animation: card-in 0.3s ease-out both">
	<!-- Main link (whole card) -->
	<a href={detailHref}
	   class="block relative rounded-[10px] overflow-hidden cursor-pointer border transition-all duration-200"
	   style="aspect-ratio:2/3; background:var(--color-ink3); border-color:var(--color-border);
	          group-hover:border-color:var(--color-amber-dim)">

		<!-- Cover or placeholder -->
		<LazyImage src={coverUrl(manga)} alt={manga.title} class="w-full h-full" />

		<!-- Scrim -->
		<div class="absolute inset-0 pointer-events-none"
		     style="background: linear-gradient(to top, rgba(10,9,7,.96) 0%, rgba(10,9,7,.4) 40%, transparent 65%)">
		</div>

		<!-- Info -->
		<div class="absolute bottom-0 left-0 right-0 p-2.5">
			<p class="text-[11.5px] font-medium leading-[1.35] mb-[3px] line-clamp-2"
			   style="color: #e8e2d8">{manga.title}</p>
			<div class="flex items-center gap-[5px]" style="font-size:10px; color:var(--color-text-dim)">
				<span style="color:var(--color-amber); font-size:10px">●</span>
				{progress != null && totalChapters > 0
					? `Ch. ${Math.floor(progress)} / ${totalChapters}`
					: totalChapters > 0 ? `${totalChapters} chapters` : manga.status}
			</div>
		</div>

		<!-- Unread badge -->
		{#if unreadCount > 0}
			<div class="absolute top-2 right-2 text-[10px] font-medium px-[7px] py-[2px] rounded-full text-white"
			     style="background: rgba(201,123,42,.92); backdrop-filter:blur(4px)">
				{unreadCount} new
			</div>
		{/if}

		<!-- Progress bar -->
		{#if progress != null && totalChapters > 0}
			<div class="absolute bottom-0 left-0 right-0 h-[2px]" style="background:var(--color-border)">
				<div class="h-full transition-all duration-300" style="width:{progressPct}%; background:var(--color-amber)"></div>
			</div>
		{/if}
	</a>

	<!-- Hover overlay (pointer-events-none so card link still works for clicks outside buttons) -->
	<div class="absolute inset-0 rounded-[10px] flex flex-col items-center justify-center gap-2
	            opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none"
	     style="background:rgba(12,11,9,.82); backdrop-filter:blur(2px)">
		{#if nextHref}
			<a href={nextHref}    class="overlay-btn pointer-events-auto">
				{progress ? 'Continue reading' : 'Start reading'}
			</a>
		{/if}
		<a href={detailHref}  class="overlay-btn secondary pointer-events-auto">View details</a>
	</div>

	<!-- Lift + border glow handled in group-hover via CSS -->
</div>

<style>
	div.group:hover > a {
		transform: translateY(-3px);
		border-color: var(--color-amber-dim);
		box-shadow: 0 8px 32px rgba(0,0,0,.6);
	}
	div.group > a {
		transition: transform 0.2s, border-color 0.2s, box-shadow 0.2s;
	}
</style>
