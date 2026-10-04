<script lang="ts">
	import { getHealth } from '$lib/api'
	import { avatar, GRADIENTS, EMOJIS, saveAvatar } from '$lib/stores.svelte'
	import type { HealthStatus } from '$lib/api/types'

	type Section = 'appearance' | 'downloads' | 'storage' | 'backup' | 'network' | 'advanced'

	let health     = $state<HealthStatus | null>(null)
	let pickerOpen = $state(false)
	let active     = $state<Section>('appearance')

	$effect(() => {
		getHealth().then(h => { health = h }).catch(() => {})
	})

	const grad = $derived(GRADIENTS[avatar.gradientIdx] ?? GRADIENTS[0])

	function setGradient(i: number) { avatar.gradientIdx = i; saveAvatar() }
	function setEmoji(e: string)    { avatar.emoji = e;        saveAvatar() }

	function statusColor(s: string) {
		const v = s?.toLowerCase()
		if (v === 'healthy' || v === 'running' || v === 'ok') return '#4ade80'
		if (v === 'degraded') return '#fbbf24'
		return '#f87171'
	}

	const NAV = [
		{ section: 'App',      items: [
			{ id: 'appearance' as Section, label: 'Appearance' },
		]},
		{ section: 'Data',     items: [
			{ id: 'downloads'  as Section, label: 'Downloads' },
			{ id: 'storage'    as Section, label: 'Storage' },
			{ id: 'backup'     as Section, label: 'Backup & Restore' },
		]},
		{ section: 'Advanced', items: [
			{ id: 'network'    as Section, label: 'Network' },
			{ id: 'advanced'   as Section, label: 'Advanced' },
		]},
	]
</script>

<svelte:head><title>Settings — Grimoire</title></svelte:head>

<!-- Topbar -->
<div class="sticky top-0 z-20 flex items-center px-5 py-3.5 border-b"
     style="background:rgba(12,11,9,.95); backdrop-filter:blur(8px); border-color:var(--color-border)">
	<h1 style="font-family:var(--font-serif); font-size:18px; font-weight:500; color:var(--color-text); letter-spacing:0.01em">
		Settings
	</h1>
</div>

<!-- Two-panel layout -->
<div class="settings-layout flex flex-col sm:flex-row overflow-hidden">

	<!-- Left nav -->
	<nav class="settings-nav shrink-0 border-b sm:border-b-0 sm:border-r sm:overflow-y-auto py-2 sm:py-4 flex sm:flex-col flex-row overflow-x-auto gap-0" style="sm:width:180px; border-color:var(--color-border)">
		{#each NAV as group}
			<p class="section-label text-[10px] uppercase tracking-[0.08em] px-4 pb-1 mt-2 first:mt-0 hidden sm:block"
			   style="color:var(--color-text-muted)">{group.section}</p>
			{#each group.items as item}
				<button onclick={() => active = item.id}
				        class="nav-item shrink-0 sm:w-full text-left px-4 py-[10px] sm:py-[7px] text-[12px] border-b-2 sm:border-b-0 sm:border-l-2 transition-all whitespace-nowrap"
				        style="color:{active === item.id ? 'var(--color-amber)' : 'var(--color-text-dim)'};
				               border-color:{active === item.id ? 'var(--color-amber)' : 'transparent'};
				               background:{active === item.id ? 'rgba(107,61,16,.15)' : 'transparent'}">
					{item.label}
				</button>
			{/each}
		{/each}
	</nav>

	<!-- Right content -->
	<div class="settings-content flex-1 overflow-y-auto px-6 py-5">

		<!-- APPEARANCE -->
		{#if active === 'appearance'}
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">Profile</p>

				<div class="setting-row flex items-center gap-3 py-2.5 border-b" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Avatar</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Customise your profile icon</p>
					</div>
					<button onclick={() => pickerOpen = !pickerOpen}
					        class="relative shrink-0 group" title="Change avatar">
						<div class="w-10 h-10 rounded-xl flex items-center justify-center text-xl transition-transform group-hover:scale-105"
						     style="background:linear-gradient(135deg,{grad.from},{grad.to}); color:#fff">
							{avatar.emoji}
						</div>
						<div class="absolute -bottom-1 -right-1 w-4 h-4 rounded-full border-2 text-[9px] flex items-center justify-center"
						     style="background:var(--color-ink4); border-color:var(--color-ink3)">✏️</div>
					</button>
				</div>

				{#if pickerOpen}
					<div class="pt-4 space-y-3" style="animation:fade-up 0.2s ease-out both">
						<div>
							<p class="text-[11px] mb-2" style="color:var(--color-text-muted)">Colour</p>
							<div class="flex flex-wrap gap-2">
								{#each GRADIENTS as g, i}
									<button onclick={() => setGradient(i)} title={g.label}
									        class="w-7 h-7 rounded-lg transition-all duration-150"
									        style="background:linear-gradient(135deg,{g.from},{g.to});
									               outline:{avatar.gradientIdx === i ? '2px solid rgba(255,255,255,.7)' : '2px solid transparent'};
									               outline-offset:2px;
									               transform:{avatar.gradientIdx === i ? 'scale(1.15)' : 'scale(1)'}">
									</button>
								{/each}
							</div>
						</div>
						<div>
							<p class="text-[11px] mb-2" style="color:var(--color-text-muted)">Icon</p>
							<div class="flex flex-wrap gap-1.5">
								{#each EMOJIS as e}
									<button onclick={() => setEmoji(e)}
									        class="w-8 h-8 rounded-lg text-base flex items-center justify-center transition-all duration-150"
									        style="background:{avatar.emoji === e ? 'var(--color-amber-dim)' : 'var(--color-ink4)'};
									               outline:{avatar.emoji === e ? '1px solid rgba(201,123,42,.5)' : 'none'}">
										{e}
									</button>
								{/each}
							</div>
						</div>
					</div>
				{/if}
			</div>

		<div class="settings-group mb-7">
			<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
			   style="color:var(--color-text-muted); border-color:var(--color-border)">System Status</p>
				{#if health}
					{#each Object.entries(health) as [service, status]}
						<div class="flex items-center justify-between py-2.5 border-b last:border-b-0"
						     style="border-color:rgba(46,42,36,.5)">
							<span class="text-[13px] capitalize" style="color:var(--color-text-dim)">{service}</span>
							<div class="flex items-center gap-2">
								<div class="w-1.5 h-1.5 rounded-full" style="background:{statusColor(status)}"></div>
								<span class="text-[12px] capitalize" style="color:{statusColor(status)}">{status}</span>
							</div>
						</div>
					{/each}
				{:else}
					<p class="text-sm py-2" style="color:var(--color-text-dim)">Checking…</p>
				{/if}
			</div>

			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">About</p>
				{#each [['Backend API', import.meta.env.PUBLIC_API_URL || 'localhost:7000'], ['MinIO URL', import.meta.env.PUBLIC_MINIO_URL || 'localhost:9000']] as [label, value]}
					<div class="flex items-center justify-between gap-3 py-2.5 border-b last:border-b-0"
					     style="border-color:rgba(46,42,36,.5)">
						<span class="text-[13px]" style="color:var(--color-text-dim)">{label}</span>
						<code class="text-[11px] px-2 py-1 rounded-[5px] font-mono max-w-[200px] truncate"
						      style="background:var(--color-ink4); color:var(--color-text); border:1px solid var(--color-border)">{value}</code>
					</div>
				{/each}
			</div>
		{/if}

		<!-- DOWNLOADS -->
		{#if active === 'downloads'}
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">Storage</p>
				<div class="setting-row flex items-center gap-3 py-2.5 border-b" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Download location</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Where downloaded chapters are stored</p>
					</div>
					<select class="sel-input shrink-0">
						<option>MinIO (default)</option>
						<option>Local filesystem</option>
					</select>
				</div>
				<div class="setting-row flex items-center gap-3 py-2.5" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Image format</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Format for downloaded pages</p>
					</div>
					<select class="sel-input shrink-0">
						<option>Original</option>
						<option>WebP</option>
						<option>JPEG (smaller)</option>
					</select>
				</div>
			</div>
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">Behaviour</p>
				<div class="setting-row flex items-center gap-3 py-2.5 border-b" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Concurrent downloads</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Max simultaneous chapter downloads</p>
					</div>
					<input class="num-input shrink-0" type="number" value="3" min="1" max="10" />
				</div>
				<div class="setting-row flex items-center gap-3 py-2.5">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Download ahead</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Pre-download the next N chapters while reading</p>
					</div>
					<input class="num-input shrink-0" type="number" value="2" min="0" max="10" />
				</div>
			</div>
		{/if}

		<!-- STORAGE -->
		{#if active === 'storage'}
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">MinIO</p>
				<div class="setting-row flex items-center gap-3 py-2.5 border-b" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Endpoint</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Your MinIO server URL</p>
					</div>
					<input class="sel-input shrink-0" style="width:180px" type="text"
					       value={import.meta.env.PUBLIC_MINIO_URL || 'localhost:9000'} />
				</div>
			</div>
		{/if}

		<!-- BACKUP -->
		{#if active === 'backup'}
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">Backup</p>
				<div class="setting-row flex items-center gap-3 py-2.5 border-b" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Export library backup</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Saves your library, reading progress, and settings</p>
					</div>
					<button class="sel-input shrink-0" style="color:var(--color-amber); border-color:var(--color-amber-dim); cursor:pointer">Export</button>
				</div>
				<div class="setting-row flex items-center gap-3 py-2.5">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Import backup</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Restore from a previously exported file</p>
					</div>
					<button class="sel-input shrink-0" style="cursor:pointer">Import</button>
				</div>
			</div>
		{/if}

		<!-- NETWORK -->
		{#if active === 'network'}
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">Requests</p>
				<div class="setting-row flex items-center gap-3 py-2.5 border-b" style="border-color:rgba(46,42,36,.5)">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">Request timeout</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Seconds before a request is considered failed</p>
					</div>
					<input class="num-input shrink-0" type="number" value="30" />
				</div>
				<div class="setting-row flex items-center gap-3 py-2.5">
					<div class="flex-1">
						<p class="text-[13px] mb-0.5" style="color:var(--color-text)">User agent</p>
						<p class="text-[11px] leading-relaxed" style="color:var(--color-text-muted)">Override the HTTP user agent string</p>
					</div>
					<select class="sel-input shrink-0">
						<option>Default</option>
						<option>Custom</option>
					</select>
				</div>
			</div>
		{/if}

		<!-- ADVANCED -->
		{#if active === 'advanced'}
			<div class="settings-group mb-7">
				<p class="group-title text-[11px] uppercase tracking-[0.08em] pb-2 mb-3 border-b"
				   style="color:var(--color-text-muted); border-color:var(--color-border)">Danger Zone</p>
				<div class="rounded-[10px] p-4" style="background:rgba(122,42,42,.1); border:1px solid rgba(122,42,42,.3)">
					<p class="text-[12px] font-medium mb-1" style="color:#c44040">Clear all data</p>
					<p class="text-[11px] leading-relaxed mb-3" style="color:var(--color-text-muted)">
						Permanently removes your library, reading progress, and all downloaded chapters. This cannot be undone.
					</p>
					<button class="danger-btn px-3.5 py-1.5 rounded-[7px] text-[12px] transition-colors"
					        style="border:1px solid rgba(196,64,64,.4); background:rgba(196,64,64,.1); color:#c44040; cursor:pointer; font-family:var(--font-sans)"
					        onmouseenter={(e)=>((e.currentTarget as HTMLElement).style.background='rgba(196,64,64,.2)')}
					        onmouseleave={(e)=>((e.currentTarget as HTMLElement).style.background='rgba(196,64,64,.1)')}>
						Clear all data
					</button>
				</div>
			</div>
		{/if}

	</div>
</div>

<style>
	.settings-layout { flex: 1; overflow: hidden; }
	@media (min-width: 640px) {
		.settings-layout { height: calc(100vh - 53px); }
		.settings-nav { width: 180px; }
	}
	.nav-item:hover {
		color: var(--color-text) !important;
		background: var(--color-ink3) !important;
	}
	.sel-input {
		appearance: none;
		background: var(--color-ink3);
		border: 1px solid var(--color-border);
		color: var(--color-text);
		padding: 5px 10px;
		border-radius: 7px;
		font-size: 12px;
		font-family: var(--font-sans);
	}
	select.sel-input {
		padding-right: 28px;
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6'%3E%3Cpath d='M0 0l5 6 5-6z' fill='%234a4640'/%3E%3C/svg%3E");
		background-repeat: no-repeat;
		background-position: right 8px center;
		background-color: var(--color-ink3);
		cursor: pointer;
	}
	.num-input {
		background: var(--color-ink3);
		border: 1px solid var(--color-border);
		color: var(--color-text);
		padding: 5px 10px;
		border-radius: 7px;
		font-size: 12px;
		width: 70px;
		font-family: var(--font-sans);
	}
</style>
