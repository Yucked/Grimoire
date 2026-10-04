<script lang="ts">
	import { getSources, toggleSource } from '$lib/api'
	import { Search } from 'lucide-svelte'
	import type { SourceObject } from '$lib/api/types'

	let sources  = $state<SourceObject[]>([])
	let loading  = $state(true)
	let error    = $state<string | null>(null)
	let search   = $state('')
	let tab      = $state<'all' | 'enabled'>('all')
	let toggling = $state<Set<string>>(new Set())

	$effect(() => {
		getSources()
			.then(s => { sources = s })
			.catch(e => { error = e.message })
			.finally(() => { loading = false })
	})

	const filtered = $derived(
		sources.filter(s => {
			if (tab === 'enabled' && s.isDisabled) return false
			if (search && !s.name.toLowerCase().includes(search.toLowerCase())) return false
			return true
		})
	)

	const enabledCount = $derived(sources.filter(s => !s.isDisabled).length)

	async function toggle(src: SourceObject) {
		if (toggling.has(src.id)) return
		toggling = new Set([...toggling, src.id])
		try {
			const r = await toggleSource(src.id)
			sources = sources.map(s => s.id === src.id ? { ...s, isDisabled: r.isDisabled } : s)
		} finally {
			toggling = new Set([...toggling].filter(id => id !== src.id))
		}
	}

	// Deterministic icon color from source name
	const ICON_PALETTES = [
		{ bg: '#1f3a5a', fg: '#60a0e0' },
		{ bg: '#3a1010', fg: '#e06060' },
		{ bg: '#1a2a1a', fg: '#60c080' },
		{ bg: '#2a1a3a', fg: '#a060e0' },
		{ bg: '#3a2010', fg: '#e08030' },
		{ bg: '#1a1a3a', fg: '#6080e0' },
		{ bg: '#2a2a10', fg: '#c0c040' },
		{ bg: '#3a1a2a', fg: '#e070b0' },
	]
	function iconPalette(name: string) {
		return ICON_PALETTES[[...name].reduce((a, c) => a + c.charCodeAt(0), 0) % ICON_PALETTES.length]
	}
	function iconLabel(name: string) {
		const words = name.trim().split(/\s+/)
		if (words.length >= 2) return (words[0][0] + words[1][0]).toUpperCase()
		return name.slice(0, 2).toUpperCase()
	}
</script>

<svelte:head><title>Sources — Grimoire</title></svelte:head>

<!-- Topbar -->
<div class="sticky top-0 z-20 flex items-center gap-3 px-5 py-3.5 border-b"
     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
	<h1 class="mr-auto" style="font-family:var(--font-serif); font-size:18px; font-weight:500; color:var(--color-text); letter-spacing:0.01em">
		Sources
	</h1>
	<!-- Search -->
	<div class="flex items-center gap-2 px-3 py-1.5 rounded-lg border transition-colors w-full sm:w-[200px]"
	     style="background:var(--color-ink3); border-color:var(--color-border)"
	     onfocusin={(e) => (e.currentTarget.style.borderColor = 'var(--color-amber-dim)')}
	     onfocusout={(e) => (e.currentTarget.style.borderColor = 'var(--color-border)')}>
		<Search size={13} color="var(--color-text-muted)" class="shrink-0" />
		<input bind:value={search} placeholder="Search sources…"
		       class="bg-transparent border-none outline-none w-full text-[12px]"
		       style="color:var(--color-text); font-family:var(--font-sans)"
		       style:color="var(--color-text)" />
	</div>
</div>

<!-- Tabs -->
<div class="flex border-b px-5" style="border-color:var(--color-border)">
	{#each [['all','All'] as const, ['enabled','Enabled'] as const] as [key, label]}
		<button onclick={() => tab = key}
		        class="px-4 py-2.5 text-[12px] tracking-[0.02em] border-b-2 transition-all"
		        style="margin-bottom:-1px;
		               color:{tab === key ? 'var(--color-amber)' : 'var(--color-text-dim)'};
		               border-bottom-color:{tab === key ? 'var(--color-amber)' : 'transparent'}">
			{label}
			{#if key === 'all' && sources.length > 0}
				<span class="ml-1 px-1.5 py-px rounded-full text-[10px]"
				      style="background:var(--color-amber-dim); color:var(--color-amber)">{sources.length}</span>
			{:else if key === 'enabled' && enabledCount > 0}
				<span class="ml-1 px-1.5 py-px rounded-full text-[10px]"
				      style="background:var(--color-amber-dim); color:var(--color-amber)">{enabledCount}</span>
			{/if}
		</button>
	{/each}
</div>

<!-- Grid -->
<div class="p-4 pb-24 sm:pb-5 overflow-y-auto">
	{#if loading}
		<div class="flex justify-center py-20">
			<div class="w-6 h-6 rounded-full border border-t-transparent animate-spin"
			     style="border-color:var(--color-border); border-top-color:var(--color-amber)"></div>
		</div>
	{:else if error}
		<p class="text-center py-12 text-sm" style="color:var(--color-text-dim)">{error}</p>
	{:else if filtered.length === 0}
		<p class="text-center py-12 text-sm" style="color:var(--color-text-muted)">
			{search ? 'No sources match your search.' : 'No sources configured.'}
		</p>
	{:else}
		<div class="grid gap-3" style="grid-template-columns:repeat(auto-fill,minmax(280px,1fr))">
			{#each filtered as src, i (src.id)}
				{@const palette = iconPalette(src.name)}
				{@const enabled = !src.isDisabled}
				<a href={enabled ? `/sources/${src.id}` : undefined}
				   class="source-card flex gap-3 items-start rounded-[10px] border p-3.5 transition-colors"
				   style="background:var(--color-ink3);
				          border-color:{enabled ? 'rgba(42,122,74,.4)' : 'var(--color-border)'};
				          animation:card-in 0.3s ease-out both; animation-delay:{Math.min(i*30,400)}ms;
				          cursor:{enabled ? 'pointer' : 'default'}"
				   onmouseenter={(e) => { const el = e.currentTarget as HTMLElement; el.style.borderColor = enabled ? 'rgba(42,122,74,.65)' : 'var(--color-ink4)' }}
				   onmouseleave={(e) => { const el = e.currentTarget as HTMLElement; el.style.borderColor = enabled ? 'rgba(42,122,74,.4)' : 'var(--color-border)' }}>
					<!-- Icon -->
					<div class="relative w-10 h-10 rounded-lg flex items-center justify-center text-sm font-semibold shrink-0"
					     style="background:{palette.bg}; color:{palette.fg}">
						{iconLabel(src.name)}
						{#if src.favicon}
							<img src={src.favicon} alt="" class="absolute inset-0 w-full h-full object-contain rounded-lg p-2"
							     onerror={(e) => { (e.currentTarget as HTMLImageElement).style.display='none' }} />
						{/if}
					</div>

					<!-- Body -->
					<div class="flex-1 min-w-0">
						<p class="text-[13px] font-medium mb-0.5" style="color:var(--color-text)">{src.name}</p>
						{#if src.url}
							<p class="text-[11px] mb-1.5 truncate" style="color:var(--color-text-muted)">{src.url.replace(/^https?:\/\//, '')}</p>
						{/if}
						<div class="flex flex-wrap gap-1">
							<span class="text-[10px] px-1.5 py-px rounded-full"
							      style="background:rgba(42,74,122,.3); color:#70a0e0">EN</span>
							{#if src.isDisabled}
								<span class="text-[10px] px-1.5 py-px rounded-full"
								      style="background:var(--color-ink4); color:var(--color-text-dim)">Disabled</span>
							{/if}
						</div>
					</div>

					<!-- Toggle (stop propagation so it doesn't navigate) -->
					<div class="shrink-0">
						<button onclick={(e) => { e.preventDefault(); toggle(src) }} disabled={toggling.has(src.id)}
						        aria-label="{enabled ? 'Disable' : 'Enable'} {src.name}"
						        class="relative inline-flex h-5 w-[34px] items-center rounded-full transition-colors disabled:opacity-50"
						        style="background:{enabled ? 'var(--color-green, #2a7a4a)' : 'var(--color-ink4)'}; border:1px solid {enabled ? 'var(--color-green, #2a7a4a)' : 'var(--color-border)'}">
							<span class="inline-block h-3.5 w-3.5 rounded-full bg-white shadow transition-transform"
							      style="transform:translateX({enabled ? '16px' : '2px'})"></span>
						</button>
					</div>
				</a>
			{/each}
		</div>
	{/if}
</div>

<style>
	input::placeholder { color: var(--color-text-muted); }
</style>
