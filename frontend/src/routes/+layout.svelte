<script lang="ts">
	import '../app.css'
	import Sidebar from '$lib/components/Sidebar.svelte'
	import Toast from '$lib/components/Toast.svelte'
	import { ping } from '$lib/api'
	import { loadAccent } from '$lib/stores.svelte'
	import { onNavigate } from '$app/navigation'

	$effect(() => { loadAccent() })

	let { children } = $props()

	let online = $state<boolean | null>(null)
	let retry  = $state<ReturnType<typeof setTimeout> | null>(null)

	async function check() {
		try { await ping(); online = true }
		catch { online = false; retry = setTimeout(check, 5000) }
	}

	$effect(() => {
		check()
		return () => { if (retry) clearTimeout(retry) }
	})

	onNavigate(nav => {
		if (!document.startViewTransition) return
		return new Promise(resolve => {
			document.startViewTransition(async () => { resolve(); await nav.complete })
		})
	})
</script>

{#if online !== true}
	<div class="fixed inset-0 z-[9999] flex items-center justify-center" style="background: var(--color-ink)">
		<div class="flex flex-col items-center gap-5 text-center px-6">
			<div class="relative w-14 h-14 flex items-center justify-center">
				<div class="absolute inset-0 rounded-full border border-amber/20"></div>
				<div class="absolute inset-0 rounded-full border border-t-amber animate-spin" style="border-color: transparent; border-top-color: var(--color-amber)"></div>
				<img src="/favicon.png" alt="Grimoire" style="width:20px;height:20px;object-fit:contain" />
			</div>
			<div>
				<p class="text-sm font-medium" style="color: var(--color-text)">
					{online === null ? 'Connecting to Grimoire…' : 'Backend unreachable'}
				</p>
				{#if online === false}
					<p class="mt-1 text-xs" style="color: var(--color-text-dim)">Retrying in a moment…</p>
				{/if}
			</div>
		</div>
	</div>
{:else}
	<div class="flex h-screen overflow-hidden" style="background: var(--color-ink)">
		<Sidebar />
		<main class="flex-1 overflow-y-auto" style="background: var(--color-ink)">
			{@render children()}
		</main>
	</div>
	<Toast />
{/if}
