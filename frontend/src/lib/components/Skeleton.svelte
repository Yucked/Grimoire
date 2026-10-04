<script lang="ts">
	let { class: cls = '' }: { class?: string } = $props()
</script>

<div class="animate-pulse bg-raised rounded {cls}"></div>
