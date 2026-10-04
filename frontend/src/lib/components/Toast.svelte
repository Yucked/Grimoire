<script lang="ts">
	import { toasts } from '$lib/stores.svelte'
	import { X } from 'lucide-svelte'

	const COLORS = {
		error:   { bg: 'rgba(196,64,64,.15)',  border: 'rgba(196,64,64,.4)',   text: '#f87171' },
		success: { bg: 'rgba(74,222,128,.1)',   border: 'rgba(74,222,128,.3)',  text: '#4ade80' },
		info:    { bg: 'rgba(201,123,42,.12)',  border: 'rgba(201,123,42,.35)', text: 'var(--color-amber)' },
	}

	function dismiss(id: number) {
		const i = toasts.findIndex(t => t.id === id)
		if (i !== -1) toasts.splice(i, 1)
	}
</script>

<div class="fixed bottom-4 right-4 z-[9999] flex flex-col gap-2 w-full max-w-sm pointer-events-none">
	{#each toasts as t (t.id)}
		{@const c = COLORS[t.level]}
		<div class="flex items-start gap-3 px-4 py-3 rounded-[10px] border pointer-events-auto"
		     style="background:{c.bg}; border-color:{c.border}; backdrop-filter:blur(8px); animation:fade-up 0.2s ease-out both">
			<p class="flex-1 text-[13px] leading-snug" style="color:{c.text}">{t.message}</p>
			<button onclick={() => dismiss(t.id)} class="shrink-0 opacity-60 hover:opacity-100 transition-opacity" style="color:{c.text}">
				<X size={14} />
			</button>
		</div>
	{/each}
</div>
