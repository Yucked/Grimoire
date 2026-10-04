<script lang="ts">
	import { toUrlSafe } from '$lib/api'
	import { ChevronRight } from 'lucide-svelte'
	import type { SourceObject } from '$lib/api/types'
	let { source }: { source: SourceObject } = $props()
</script>

<a href="/sources/{toUrlSafe(source.id)}"
   class="group flex items-center gap-3 rounded-[10px] p-4 border transition-all duration-200"
   style="background:var(--color-ink3); border-color:var(--color-border)">

	<div class="relative w-9 h-9 rounded-lg flex items-center justify-center text-sm font-semibold shrink-0"
	     style="background:var(--color-ink4); color:var(--color-text-dim)">
		{source.name[0]}
		{#if source.favicon}
			<img src={source.favicon} alt="" loading="lazy"
			     class="absolute inset-0 w-full h-full object-contain rounded-lg p-1"
			     onerror={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }} />
		{/if}
	</div>

	<div class="min-w-0 flex-1">
		<p class="text-sm font-medium truncate" style="color:var(--color-text)">{source.name}</p>
		<div class="flex items-center gap-1.5 mt-0.5">
			<div class="w-1.5 h-1.5 rounded-full" style="background:{source.isDisabled ? 'var(--color-text-muted)' : '#4ade80'}"></div>
			<p class="text-[11px]" style="color:{source.isDisabled ? 'var(--color-text-muted)' : '#4ade80'}">
				{source.isDisabled ? 'Disabled' : 'Active'}
			</p>
		</div>
	</div>

	<ChevronRight size={14} color="var(--color-text-muted)" class="shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" />
</a>

<style>
	a:hover { border-color: var(--color-amber-dim) !important; }
</style>
