<script lang="ts">
	import { readerHref } from '$lib/api'
	import { Download, Check } from 'lucide-svelte'
	import type { ChapterObject } from '$lib/api/types'

	let {
		chapters,
		sourceId,
		mangaId,
		lastChapterRead = 0
	}: {
		chapters:         ChapterObject[]
		sourceId:         string
		mangaId:          string
		lastChapterRead?: number
	} = $props()

	const sorted = $derived([...chapters].sort((a, b) => parseFloat(b.number) - parseFloat(a.number)))

	function relativeDate(iso: string): string {
		const d = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000)
		if (d === 0) return 'Today'
		if (d === 1) return 'Yesterday'
		if (d < 7)   return `${d}d ago`
		if (d < 30)  return `${Math.floor(d / 7)}w ago`
		if (d < 365) return `${Math.floor(d / 30)}mo ago`
		return `${Math.floor(d / 365)}y ago`
	}
</script>

<div>
	{#each sorted as ch, i}
		{@const isRead = parseFloat(ch.number) <= lastChapterRead}
		<a href={readerHref(sourceId, mangaId, ch.number)}
		   style="animation:row-in 0.22s ease-out both; animation-delay:{Math.min(i*18,400)}ms;
		          border-bottom:1px solid var(--color-border)"
		   class="group flex items-center gap-3 px-4 py-3 transition-colors last:border-b-0"
		   onmouseenter={(e)=>(e.currentTarget.style.background='var(--color-ink4)')}
		   onmouseleave={(e)=>(e.currentTarget.style.background='')}>

			<!-- Read indicator -->
			<div class="w-1 h-6 rounded-full shrink-0 transition-colors"
			     style="background:{isRead ? 'var(--color-border)' : 'var(--color-amber-dim)'}"></div>

			<div class="flex-1 min-w-0">
				<span class="text-sm" style="color:{isRead ? 'var(--color-text-muted)' : 'var(--color-text)'}; font-weight:400">
					Chapter {ch.number}
					{#if ch.title && ch.title !== ch.number}
						<span style="color:var(--color-text-dim); font-weight:300"> — {ch.title}</span>
					{/if}
				</span>
				{#if ch.releasedOn}
					<p class="text-[11px] mt-0.5" style="color:var(--color-text-muted)" title={new Date(ch.releasedOn).toLocaleDateString(undefined, { year:'numeric', month:'short', day:'numeric' })}>
						{relativeDate(ch.releasedOn)}
					</p>
				{/if}
			</div>

			{#if ch.isDownloaded}
				<Download size={11} color="var(--color-amber)" class="shrink-0" />
			{/if}
			{#if isRead}
				<Check size={11} color="var(--color-text-muted)" class="shrink-0" />
			{/if}
		</a>
	{/each}
</div>
