<script lang="ts">
	import { getLibrary, getManga, refreshLibrary, parseLibraryKey, mangaHref, coverUrl } from '$lib/api'
	import MangaCard from '$lib/components/MangaCard.svelte'
	import { search, toast } from '$lib/stores.svelte'
	import { BookOpen, CheckSquare, Star, RefreshCw, LayoutGrid, List } from 'lucide-svelte'
	import type { MangaObject, UserObject } from '$lib/api/types'

	let user       = $state<UserObject | null>(null)
	let mangas     = $state<MangaObject[]>([])
	let loading    = $state(true)
	let error      = $state<string | null>(null)
	let refreshing = $state(false)
	let listView   = $state(false)
	let sortBy     = $state('updated')

	$effect(() => {
		getLibrary()
			.then(async u => {
				user = u
				const keys = Object.keys(u.library)
				if (!keys.length) { loading = false; return }
				const results = await Promise.allSettled(
					keys.map(key => { const { sourceId, mangaId } = parseLibraryKey(key); return getManga(sourceId, mangaId) })
				)
				mangas = results.flatMap(r => r.status === 'fulfilled' ? [r.value] : [])
			})
			.catch(e => { error = e.message })
			.finally(() => { loading = false })
	})

	async function handleRefresh() {
		if (refreshing) return
		refreshing = true
		try {
			await refreshLibrary()
			toast('Library refreshed', 'success')
		} catch (e) {
			toast(e instanceof Error ? e.message : 'Refresh failed', 'error')
		} finally { refreshing = false }
	}

	function getProgress(manga: MangaObject) {
		return user?.library[manga.id] ?? null
	}

	function hasUnread(manga: MangaObject) {
		const prog = getProgress(manga)
		return (manga.chapters?.length ?? 0) > Math.floor(prog ?? 0)
	}

	const totalRead = $derived(user ? Object.values(user.library).reduce((s, n) => s + (n ?? 0), 0) : 0)
	const completed = $derived(mangas.filter(m => m.status === 'Completed').length)

	const filtered = $derived.by(() => {
		let list = mangas
		if (search.query) {
			const q = search.query.toLowerCase()
			list = list.filter(m => m.title.toLowerCase().includes(q))
		}
		if (sortBy === 'updated') list = [...list].sort((a, b) => (b.updatedAt ?? '').localeCompare(a.updatedAt ?? ''))
		if (sortBy === 'az')      list = [...list].sort((a, b) => a.title.localeCompare(b.title))
		if (sortBy === 'rating')  list = [...list].sort((a, b) => b.ratings - a.ratings)
		return list
	})

	const withUnread  = $derived(filtered.filter(hasUnread))
	const upToDate    = $derived(filtered.filter(m => !hasUnread(m)))
</script>

<svelte:head><title>Library — Grimoire</title></svelte:head>

{#if loading}
	<div class="flex items-center justify-center h-full" style="color: var(--color-text-dim)">
		<div class="w-6 h-6 rounded-full border border-t-transparent animate-spin" style="border-color: var(--color-border); border-top-color: var(--color-amber)"></div>
	</div>

{:else if error}
	<div class="flex items-center justify-center h-full text-sm" style="color:var(--color-text-dim)">{error}</div>

{:else if !mangas.length}
	<div class="flex flex-col items-center justify-center h-full text-center px-6 pb-16">
		<BookOpen size={40} color="var(--color-amber)" class="mb-5 opacity-60" />
		<p class="font-serif text-lg mb-1" style="color:var(--color-text)">Your library is empty</p>
		<p class="text-sm mb-6" style="color:var(--color-text-dim)">Browse sources to discover manga.</p>
		<a href="/sources" class="overlay-btn">Browse sources</a>
	</div>

{:else}
	<!-- ── Topbar ──────────────────────────────────────────────────────────── -->
	<div class="sticky top-0 z-20 flex items-center gap-3 px-5 py-3.5 border-b"
	     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
		<h1 class="mr-auto tracking-[0.01em]" style="font-family:var(--font-serif); font-size:18px; font-weight:500; color:var(--color-text)">
			Library
		</h1>
		<div class="stat-pill hidden sm:flex">
			<BookOpen size={13} color="var(--color-amber)" />
			<strong>{mangas.length}</strong> series
		</div>
		<div class="stat-pill hidden sm:flex">
			<CheckSquare size={13} color="var(--color-amber)" />
			<strong>{Math.floor(totalRead)}</strong> ch read
		</div>
		<div class="stat-pill hidden sm:flex">
			<Star size={13} color="var(--color-amber)" />
			<strong>{completed}</strong> completed
		</div>
	</div>

	<!-- ── Toolbar ─────────────────────────────────────────────────────────── -->
	<div class="sticky top-[53px] z-10 flex items-center gap-2 px-4 py-2 border-b"
	     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
		<span class="mr-auto text-[11px] uppercase tracking-[0.05em] hidden sm:block" style="color:var(--color-text-muted)">
			{#if search.query}"{search.query}"{:else}All — {filtered.length}{/if}
		</span>

		<!-- Search -->
		<input type="search" bind:value={search.query} placeholder="Search…"
		       class="w-28 sm:w-36 text-xs rounded-[7px] px-2.5 py-[6px] border outline-none transition-colors"
		       style="background:var(--color-ink); border-color:var(--color-border); color:var(--color-text);
		              font-family:var(--font-sans)"
		       onfocus={(e)=>(e.currentTarget.style.borderColor='var(--color-amber-dim)')}
		       onblur={(e)=>(e.currentTarget.style.borderColor='var(--color-border)')} />

		<button onclick={handleRefresh} disabled={refreshing}
		        class="tbtn {refreshing ? 'active' : ''}" title="Refresh library">
			<RefreshCw size={12} class={refreshing ? 'animate-spin' : ''} />
			<span class="hidden sm:inline">{refreshing ? 'Refreshing…' : 'Refresh'}</span>
		</button>

		<div class="relative">
			<select bind:value={sortBy} class="tselect pr-6">
				<option value="updated">Recently updated</option>
				<option value="az">Alphabetical</option>
				<option value="rating">Top rated</option>
			</select>
			<span class="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none text-[10px]"
			      style="color:var(--color-text-muted)">▾</span>
		</div>

		<!-- View toggle -->
		<div class="flex border rounded-[7px] overflow-hidden" style="border-color:var(--color-border)">
			<button onclick={() => listView = false} aria-label="Grid view"
			        class="tbtn border-none rounded-none px-[9px] {!listView ? 'active' : ''}">
				<LayoutGrid size={12} />
			</button>
			<button onclick={() => listView = true} aria-label="List view"
			        class="tbtn border-none rounded-none px-[9px] border-l {listView ? 'active' : ''}"
			        style="border-left:1px solid var(--color-border)">
				<List size={12} />
			</button>
		</div>
	</div>

	<!-- ── Grid ────────────────────────────────────────────────────────────── -->
	<div class="p-5 pb-24 sm:pb-5">

		{#if !listView}
			<!-- Grid view -->
			{#if withUnread.length > 0}
				<div class="flex items-baseline gap-2.5 mb-3.5">
					<span style="font-family:var(--font-serif); font-size:13px; color:var(--color-text-dim); font-style:italic">Unread updates</span>
					<span style="font-size:11px; color:var(--color-text-muted)">{withUnread.length} with new chapters</span>
				</div>
				<div class="grid gap-3.5 mb-7" style="grid-template-columns: repeat(auto-fill, minmax(140px, 1fr))">
					{#each withUnread as manga, i}
						<div style="animation-delay:{i*35}ms">
							<MangaCard {manga} progress={getProgress(manga)} />
						</div>
					{/each}
				</div>
			{/if}

			{#if upToDate.length > 0}
				<div class="flex items-baseline gap-2.5 mb-3.5">
					<span style="font-family:var(--font-serif); font-size:13px; color:var(--color-text-dim); font-style:italic">Currently reading</span>
					<span style="font-size:11px; color:var(--color-text-muted)">{upToDate.length} in progress</span>
				</div>
				<div class="grid gap-3.5" style="grid-template-columns: repeat(auto-fill, minmax(140px, 1fr))">
					{#each upToDate as manga, i}
						<div style="animation-delay:{(withUnread.length + i)*30}ms">
							<MangaCard {manga} progress={getProgress(manga)} />
						</div>
					{/each}
				</div>
			{/if}

		{:else}
			<!-- List view -->
			<div class="rounded-[10px] overflow-hidden border" style="border-color:var(--color-border)">
				{#each filtered as manga, i}
					{@const prog = getProgress(manga)}
					{@const total = manga.chapters?.length ?? 0}
					{@const unread = Math.max(0, total - Math.floor(prog ?? 0))}
					{@const urlId = parseLibraryKey(manga.id).mangaId}
					<a href={mangaHref(manga.sourceId, urlId)}
					   class="flex items-center gap-3.5 px-4 py-3 border-b transition-colors hover:bg-ink3"
					   style="border-color:var(--color-border); animation:row-in 0.25s ease-out both; animation-delay:{i*20}ms">
						<div class="w-9 h-12 rounded-md overflow-hidden shrink-0" style="background:var(--color-ink4)">
							<img src={coverUrl(manga)} alt={manga.title}
							     class="w-full h-full object-cover" loading="lazy"
							     onerror={(e) => { (e.currentTarget as HTMLImageElement).style.display='none' }} />
						</div>
						<div class="flex-1 min-w-0">
							<p class="text-sm font-medium truncate" style="color:var(--color-text)">{manga.title}</p>
							<p class="text-[11px] mt-0.5" style="color:var(--color-text-dim)">
								{prog != null ? `Ch. ${Math.floor(prog)} / ${total}` : `${total} chapters`}
							</p>
						</div>
						{#if unread > 0}
							<span class="text-[10px] px-2 py-0.5 rounded-full font-medium"
							      style="background:rgba(201,123,42,.15); color:var(--color-amber)">{unread} new</span>
						{/if}
						<span class="text-[11px] shrink-0" style="color:var(--color-text-muted)">{manga.status}</span>
					</a>
				{/each}
			</div>
		{/if}

		{#if filtered.length === 0}
			<p class="text-center py-16 text-sm" style="color:var(--color-text-muted)">No manga match your search.</p>
		{/if}
	</div>
{/if}
