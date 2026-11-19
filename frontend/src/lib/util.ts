import type { Concept, Recipe, ThemeDef } from '../types'

/** Applies a theme by writing its colours to CSS custom properties (--c-bg, --c-accent, ...). */
export function applyTheme(theme: ThemeDef | undefined) {
  if (!theme) return
  const root = document.documentElement
  root.style.removeProperty('--c-onAccent') // optional colour; don't leak between themes
  for (const [key, value] of Object.entries(theme.colors)) root.style.setProperty(`--c-${key}`, value)
  root.dataset.theme = theme.dark ? 'dark' : 'light'
  root.style.colorScheme = theme.dark ? 'dark' : 'light'
}

export const randomSeed = () => String(100000 + Math.floor(Math.random() * 900000))

export const dailySeed = (d = new Date()) =>
  `daily-${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`

export function defaultRecipe(): Recipe {
  return {
    seed: randomSeed(), chaos: 0.35, scope: 1, silliness: 0.5, pins: {}, keep: {}, rerolls: {},
    exclusions: [], titleOverride: null, featureCreep: false,
    creepKinds: ['system', 'loop', 'content', 'whyNot'], creep: [],
  }
}

export const cloneRecipe = (r: Recipe): Recipe => JSON.parse(JSON.stringify(r)) as Recipe

/** Fills fields that older saved recipes might lack. */
export function normalizeRecipe(r: Partial<Recipe> | null | undefined): Recipe {
  return { ...defaultRecipe(), ...(r ?? {}), keep: r?.keep ?? {}, pins: r?.pins ?? {}, rerolls: r?.rerolls ?? {}, exclusions: r?.exclusions ?? [], creep: r?.creep ?? [] }
}

export async function copyText(text: string) {
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    // Fallback for contexts without clipboard permission.
    const ta = document.createElement('textarea')
    ta.value = text
    document.body.appendChild(ta)
    ta.select()
    document.execCommand('copy')
    ta.remove()
  }
}

export function download(fileName: string, mime: string, content: string) {
  const url = URL.createObjectURL(new Blob([content], { type: `${mime};charset=utf-8` }))
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export const pct = (v: number) => `${Math.round(v * 100)}%`

export function timeAgo(iso: string) {
  const s = (Date.now() - new Date(iso).getTime()) / 1000
  if (s < 60) return 'just now'
  if (s < 3600) return `${Math.floor(s / 60)} min ago`
  if (s < 86400) return `${Math.floor(s / 3600)} h ago`
  return new Date(iso).toLocaleDateString()
}

/** True when the concept was made from exactly the packs currently enabled. */
export function sameLibrary(concept: Concept, signature: { id: string; version: string }[]) {
  const a = concept.library.map((p) => `${p.id}@${p.version}`).sort().join(',')
  const b = signature.map((p) => `${p.id}@${p.version}`).sort().join(',')
  return a === b
}

/** Small seeded PRNG for decorative things (cover art). Not used for generation. */
export function mulberry(seedText: string) {
  let h = 1779033703 ^ seedText.length
  for (let i = 0; i < seedText.length; i++) {
    h = Math.imul(h ^ seedText.charCodeAt(i), 3432918353)
    h = (h << 13) | (h >>> 19)
  }
  let a = h >>> 0
  return () => {
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}
