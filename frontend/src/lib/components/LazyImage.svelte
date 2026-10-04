<script lang="ts">
	import Skeleton from './Skeleton.svelte'

	let {
		src,
		alt = '',
		class: cls = ''
	}: {
		src: string
		alt?: string
		class?: string
	} = $props()

	let loaded = $state(false)
</script>

<div class="relative overflow-hidden bg-raised {cls}">
	{#if !loaded}
		<Skeleton class="absolute inset-0" />
	{/if}
	<img
		{src}
		{alt}
		loading="lazy"
		decoding="async"
		class="w-full h-full object-cover transition-opacity duration-300 {loaded ? 'opacity-100' : 'opacity-0'}"
		onload={() => (loaded = true)}
		onerror={() => (loaded = true)}
	/>
</div>
