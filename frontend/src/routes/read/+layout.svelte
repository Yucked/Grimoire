<script lang="ts">
	import '../../app.css'
	let { children } = $props()
</script>

<!-- Full-width layout for reader — no navbar, no max-width -->
{@render children()}
