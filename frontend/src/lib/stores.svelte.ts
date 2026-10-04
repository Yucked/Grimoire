export const search = $state({query: ''})

// ── Toasts ────────────────────────────────────────────────────────────────

export type ToastLevel = 'error' | 'success' | 'info'

interface ToastItem {
    id: number;
    message: string;
    level: ToastLevel
}

let _tid = 0
export const toasts = $state<ToastItem[]>([])

export function toast(message: string, level: ToastLevel = 'info', duration = 4000) {
    const id = ++_tid
    toasts.push({id, message, level})
    setTimeout(() => {
        const i = toasts.findIndex(t => t.id === id)
        if (i !== -1) toasts.splice(i, 1)
    }, duration)
}

// ── Avatar ────────────────────────────────────────────────────────────────────

export const GRADIENTS = [
    {label: 'Grimoire', from: '#8b5cf6', to: '#ec4899'},
    {label: 'Ocean', from: '#06b6d4', to: '#3b82f6'},
    {label: 'Sunset', from: '#f97316', to: '#ef4444'},
    {label: 'Forest', from: '#10b981', to: '#06b6d4'},
    {label: 'Gold', from: '#f59e0b', to: '#f97316'},
    {label: 'Rose', from: '#ec4899', to: '#8b5cf6'},
    {label: 'Arctic', from: '#6366f1', to: '#0ea5e9'},
    {label: 'Lime', from: '#84cc16', to: '#10b981'},
]

export const EMOJIS = ['📖', '⚔️', '🐉', '🌙', '⭐', '🔮', '🗡️', '📚', '🧙', '🌸', '🎭', '🦋']

function loadAvatar() {
    if (typeof window === 'undefined') return {gradientIdx: 0, emoji: '📖'}
    try {
        return JSON.parse(localStorage.getItem('grimoire_avatar') ?? 'null') ?? {gradientIdx: 0, emoji: '📖'}
    } catch {
        return {gradientIdx: 0, emoji: '📖'}
    }
}

export const avatar = $state(loadAvatar())

export function saveAvatar() {
    if (typeof window !== 'undefined') {
        localStorage.setItem('grimoire_avatar', JSON.stringify({gradientIdx: avatar.gradientIdx, emoji: avatar.emoji}))
        applyAccent(GRADIENTS[avatar.gradientIdx]?.from ?? GRADIENTS[0].from)
    }
}

// ── Accent colour ─────────────────────────────────────────────────────────────

function hexToHsl(hex: string): [number, number, number] {
    const r = parseInt(hex.slice(1, 3), 16) / 255
    const g = parseInt(hex.slice(3, 5), 16) / 255
    const b = parseInt(hex.slice(5, 7), 16) / 255
    const max = Math.max(r, g, b), min = Math.min(r, g, b)
    let h = 0, s = 0
    const l = (max + min) / 2
    if (max !== min) {
        const d = max - min
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min)
        switch (max) {
            case r:
                h = ((g - b) / d + (g < b ? 6 : 0)) / 6;
                break
            case g:
                h = ((b - r) / d + 2) / 6;
                break
            case b:
                h = ((r - g) / d + 4) / 6;
                break
        }
    }
    return [h * 360, s * 100, l * 100]
}

function hslToHex(h: number, s: number, l: number): string {
    l /= 100;
    s /= 100
    const a = s * Math.min(l, 1 - l)
    const f = (n: number) => {
        const k = (n + h / 30) % 12
        const c = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1)
        return Math.round(255 * c).toString(16).padStart(2, '0')
    }
    return `#${f(0)}${f(8)}${f(4)}`
}

export function applyAccent(hex: string) {
    if (typeof window === 'undefined') return
    const [h, s, l] = hexToHsl(hex)
    const light = hslToHex(h, Math.min(s + 5, 100), Math.min(l + 12, 95))
    const dim = hslToHex(h, Math.min(s + 10, 100), Math.max(l - 32, 5))
    const root = document.documentElement
    root.style.setProperty('--color-amber', hex)
    root.style.setProperty('--color-amber-l', light)
    root.style.setProperty('--color-amber-dim', dim)
}

export function loadAccent() {
    if (typeof window === 'undefined') return
    const saved = localStorage.getItem('grimoire_avatar')
    try {
        const parsed = saved ? JSON.parse(saved) : null
        const idx = parsed?.gradientIdx ?? 0
        applyAccent(GRADIENTS[idx]?.from ?? GRADIENTS[0].from)
    } catch {
        applyAccent(GRADIENTS[0].from)
    }
}
