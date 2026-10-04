<script lang="ts">
    import {page} from '$app/state'
    import {avatar, GRADIENTS} from '$lib/stores.svelte'
    import {Library, Globe, Settings} from 'lucide-svelte'

    const grad = $derived(GRADIENTS[avatar.gradientIdx] ?? GRADIENTS[0])

    function active(href: string) {
        return href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href)
    }

    const navItems = [
        {href: '/', label: 'Library', icon: Library},
        {href: '/sources', label: 'Sources', icon: Globe},
    ]
</script>

<!-- Desktop rail -->
<nav class="hidden sm:flex flex-col items-center shrink-0 py-3.5 gap-1.5 border-r z-40"
     style="width:56px; background:var(--color-ink); border-color:var(--color-border)">

    <!-- Logo -->
    <a href="/" aria-label="Grimoire" class="flex items-center justify-center mb-2.5" style="width:32px;height:32px">
        <img src="/favicon.png" alt="Grimoire" style="width:28px;height:28px;object-fit:contain"/>
    </a>

    {#each navItems as item}
        <a href={item.href} class="rail-item {active(item.href) ? 'active' : ''}">
            <item.icon size={16} />
            <span class="tip">{item.label}</span>
        </a>
    {/each}

    <div class="flex-1"></div>

    <!-- Settings -->
    <a href="/settings" class="rail-item {active('/settings') ? 'active' : ''}">
        <Settings size={16}/>
        <span class="tip">Settings</span>
    </a>

    <!-- Avatar -->
    <a href="/settings"
       class="mt-1 w-[30px] h-[30px] rounded-full flex items-center justify-center text-sm cursor-pointer transition-transform hover:scale-110"
       style="background: linear-gradient(135deg,{grad.from},{grad.to}); color: #fff"
       title="Profile">
        {avatar.emoji}
    </a>
</nav>

<!-- Mobile bottom nav -->
<nav class="fixed bottom-0 inset-x-0 z-40 sm:hidden flex items-center justify-around h-14 border-t"
     style="background:var(--color-ink2)/95; backdrop-filter:blur(12px); border-color:var(--color-border)">
    {#each [...navItems, {href: '/settings', label: 'Settings', icon: Settings}] as item}
        <a href={item.href}
           class="flex flex-col items-center gap-1 px-4 py-1 transition-colors"
           style="color: {active(item.href) ? 'var(--color-amber)' : 'var(--color-text-dim)'}">
            <item.icon size={18} />
            <span style="font-size:10px">{item.label}</span>
        </a>
    {/each}
</nav>
