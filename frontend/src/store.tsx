import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { api } from './api'
import type { BootstrapInfo, Concept, OutputMode, Recipe, VaultEntry } from './types'
import { applyTheme, cloneRecipe, copyText, dailySeed, defaultRecipe, download, normalizeRecipe, randomSeed } from './lib/util'
import { noorCreepSlots } from './lib/noor'

/**
 * All generator state and actions.
 *
 * Every action builds a new Recipe from the current concept's recipe and asks the host to
 * generate it, so the result is always reproducible from the recipe alone:
 *   - generateTrouble: new seed, keeps locks (pins)
 *   - rerollSlot:      bumps one field's reroll counter, "keeps" every other field
 *   - rerollUnlocked:  bumps every unlocked field (same seed)
 *   - lock / pick:     pins an item
 *   - creep actions:   edit recipe.creep slots
 */

export type Toast = { id: number; text: string; kind: 'info' | 'success' | 'error' }
export type Page = 'generator' | 'vault' | 'packs' | 'stats' | 'settings'

interface Studio {
  boot: BootstrapInfo
  refreshBoot: (b?: BootstrapInfo) => Promise<void>
  page: Page
  setPage: (p: Page) => void
  concept: Concept | null
  controls: Recipe
  busy: boolean
  outputMode: OutputMode
  setOutputMode: (m: OutputMode) => void
  setTheme: (id: string) => void
  noorMode: boolean
  setNoorMode: (on: boolean) => void
  /** Increments on every new idea in Noor Mode; drives the heart burst. */
  heartBurst: number
  vault: VaultEntry[]
  refreshVault: () => Promise<void>
  linkedEntryId: string | null
  isSaved: boolean
  notes: string
  setNotes: (n: string) => void
  toasts: Toast[]
  toast: (text: string, kind?: Toast['kind']) => void
  canBack: boolean
  canForward: boolean
  /** Ideas generated recently (persisted across restarts), newest last. */
  history: Concept[]
  historyIndex: number
  goToHistory: (index: number) => void
  /** What the last edit changed, for the brief highlight. `stamp` re-triggers the animation. */
  changes: Changes
  /** Sliders/scope changed since the current idea was made (they apply to the next one). */
  pendingSliders: boolean
  /** Slot under the pointer, in the pitch or the ingredient list; both sides highlight it. */
  hoverSlot: string | null
  setHoverSlot: (slot: string | null) => void
  a: Actions
}

export interface Changes { stamp: number; slots: Set<string>; title: boolean; pitch: boolean; creep: Set<number> }

interface Actions {
  generateTrouble: () => void
  rerollUnlocked: () => void
  rerollSlot: (slot: string) => void
  toggleLock: (slot: string) => void
  pickItem: (slot: string, itemId: string) => void
  excludeItem: (itemId: string) => void
  clearExclusions: () => void
  reword: () => void
  rerollTitle: () => void
  toggleTitleLock: () => void
  renameTitle: (title: string) => void
  setSeed: (seed: string) => void
  daily: () => void
  setSliders: (patch: Partial<Pick<Recipe, 'chaos' | 'scope' | 'silliness'>>) => void
  applySliders: () => void
  toggleCreep: () => void
  setCreepKinds: (kinds: Recipe['creepKinds']) => void
  creep: (slot: number, action: 'accept' | 'reject' | 'reroll' | 'undo') => void
  anotherThing: () => void
  back: () => void
  forward: () => void
  loadEntry: (entry: VaultEntry) => void
  save: () => Promise<void>
  updateSaved: () => Promise<void>
  copyMarkdown: () => Promise<void>
  exportAs: (format: 'markdown' | 'text' | 'json') => Promise<void>
  shareCode: () => Promise<void>
}

const Ctx = createContext<Studio | null>(null)
export const useStudio = () => {
  const s = useContext(Ctx)
  if (!s) throw new Error('useStudio outside provider')
  return s
}

const bump = (r: Recipe, key: string) => { r.rerolls[key] = (r.rerolls[key] ?? 0) + 1 }

export function StudioProvider({ initial, children }: { initial: BootstrapInfo; children: ReactNode }) {
  const [boot, setBoot] = useState(initial)
  const [page, setPage] = useState<Page>('generator')
  const [concept, setConcept] = useState<Concept | null>(null)
  const [controls, setControls] = useState<Recipe>(() => normalizeRecipe(initial.settings.lastRecipe ?? defaultRecipe()))
  const [busy, setBusy] = useState(false)
  const [outputMode, setOutputModeState] = useState<OutputMode>(initial.settings.outputMode ?? 'pitch')
  const [vault, setVault] = useState<VaultEntry[]>([])
  const [linkedEntryId, setLinkedEntryId] = useState<string | null>(null)
  const [savedSnapshot, setSavedSnapshot] = useState<string | null>(null)
  const [notes, setNotesState] = useState('')
  const [toasts, setToasts] = useState<Toast[]>([])
  const [history, setHistory] = useState<{ list: Concept[]; index: number }>(() => {
    const list = loadHistory()
    return { list, index: list.length - 1 }
  })
  const [changes, setChanges] = useState<Changes>({ stamp: 0, slots: new Set(), title: false, pitch: false, creep: new Set() })
  const [heartBurst, setHeartBurst] = useState(0)
  const [hoverSlot, setHoverSlot] = useState<string | null>(null)
  const noorRef = useRef(initial.settings.noorMode); noorRef.current = boot.settings.noorMode

  // Refs so async actions always see the latest values.
  const conceptRef = useRef(concept); conceptRef.current = concept
  const controlsRef = useRef(controls); controlsRef.current = controls
  const linkedRef = useRef(linkedEntryId); linkedRef.current = linkedEntryId
  const notesRef = useRef(notes); notesRef.current = notes

  const toast = useCallback((text: string, kind: Toast['kind'] = 'info') => {
    const id = Date.now() + Math.random()
    setToasts((t) => [...t.slice(-3), { id, text, kind }])
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), kind === 'error' ? 6000 : 2600)
  }, [])

  const refreshVault = useCallback(async () => { setVault(await api.vaultList()) }, [])
  const refreshBoot = useCallback(async (b?: BootstrapInfo) => { setBoot(b ?? (await api.bootstrap())) }, [])

  useEffect(() => { applyTheme(boot.themes.find((t) => t.id === boot.settings.theme) ?? boot.themes[0]) }, [boot])
  useEffect(() => { document.documentElement.dataset.motion = boot.settings.reduceMotion ? 'reduce' : 'full' }, [boot.settings.reduceMotion])
  useEffect(() => { void refreshVault() }, [refreshVault])
  useEffect(() => { if (boot.vaultNotice) toast(boot.vaultNotice, 'error') }, [boot.vaultNotice, toast])

  // Keep recent ideas across restarts, so a skipped gem is never lost.
  useEffect(() => { saveHistory(history.list) }, [history.list])

  // Watch the editable library: edits made in any text editor show up within a few seconds.
  useEffect(() => {
    const timer = window.setInterval(async () => {
      if (document.hidden) return
      try {
        const r = await api.checkLibrary()
        if (!r.changed || !r.bootstrap) return
        setBoot(r.bootstrap)
        if (r.broken.length) toast(`Pack problem after edit: ${r.broken[0]}`, 'error')
        else toast('Library changed on disk and was reloaded. Changes apply to your next idea.', 'success')
      } catch { /* host busy or gone; try again next tick */ }
    }, 2500)
    return () => window.clearInterval(timer)
  }, [toast])

  /** Generates a recipe and makes it current. `keepLink` = this is an edit of the same idea. */
  const run = useCallback(async (recipe: Recipe, keepLink = false) => {
    setBusy(true)
    try {
      const c = await api.generate(recipe)
      const prev = conceptRef.current
      if (keepLink && prev) setChanges(diffConcepts(prev, c))
      setConcept(c)
      setControls(normalizeRecipe(c.recipe))
      setHistory((h) => {
        // Always append (never truncate "forward" ideas): the history doubles as "Just made".
        const list = [...h.list, c].slice(-50)
        return { list, index: list.length - 1 }
      })
      if (!keepLink) { setLinkedEntryId(null); setSavedSnapshot(null); setNotesState('') }
      if (noorRef.current && !keepLink) setHeartBurst((n) => n + 1)
    } catch (e) {
      toast((e as Error).message, 'error')
    } finally {
      setBusy(false)
    }
  }, [toast])

  // First concept on startup: restore the last recipe so the app reopens where you left it.
  const started = useRef(false)
  useEffect(() => {
    if (started.current) return // StrictMode runs effects twice in dev
    started.current = true
    void run(controlsRef.current)
  }, [run])

  /** Current concept's recipe merged with the live controls (sliders, locks, exclusions, creep). */
  const base = useCallback((): Recipe => {
    const c = controlsRef.current
    const r = cloneRecipe(conceptRef.current?.recipe ?? c)
    return { ...r, chaos: c.chaos, scope: c.scope, silliness: c.silliness, pins: { ...c.pins }, exclusions: [...c.exclusions], featureCreep: c.featureCreep || noorRef.current, creepKinds: [...c.creepKinds] }
  }, [])

  /** Creep slots for a brand-new idea: empty lets the host pick its default; Noor Mode adds one more. */
  const freshCreep = (r: Recipe) =>
    noorRef.current ? Array.from({ length: noorCreepSlots(r.scope) }, () => ({ nonce: 0, status: 'proposed' as const })) : []

  /** Hold every unlocked field of the current concept steady. */
  const keepAll = (r: Recipe, except?: string) => {
    r.keep = {}
    for (const p of conceptRef.current?.picks ?? []) if (!r.pins[p.slot] && p.slot !== except) r.keep[p.slot] = p.itemId
    return r
  }

  const a = useMemo<Actions>(() => ({
    generateTrouble: () => {
      const r = base()
      r.seed = randomSeed(); r.rerolls = {}; r.keep = {}; r.creep = freshCreep(r)
      void run(r)
    },
    rerollUnlocked: () => {
      const r = base()
      r.keep = {}
      for (const p of conceptRef.current?.picks ?? []) if (!r.pins[p.slot]) bump(r, p.slot)
      if (!r.titleOverride) bump(r, 'title')
      bump(r, 'wording')
      void run(r)
    },
    rerollSlot: (slot) => {
      const r = keepAll(base(), slot)
      delete r.pins[slot]
      bump(r, slot)
      void run(r, true)
    },
    toggleLock: (slot) => {
      const r = keepAll(base())
      const pick = conceptRef.current?.picks.find((p) => p.slot === slot)
      if (r.pins[slot]) delete r.pins[slot]
      else if (pick) r.pins[slot] = pick.itemId
      void run(keepAll(r), true)
    },
    pickItem: (slot, itemId) => {
      // Choosing an item sets it without locking it; the padlock is what locks.
      const r = keepAll(base(), slot)
      const wasLocked = !!r.pins[slot]
      delete r.pins[slot]
      if (wasLocked) r.pins[slot] = itemId
      else r.keep[slot] = itemId
      void run(r, true)
    },
    excludeItem: (itemId) => {
      const r = base()
      if (!r.exclusions.includes(itemId)) r.exclusions.push(itemId)
      const hit = conceptRef.current?.picks.find((p) => p.itemId === itemId)
      keepAll(r, hit?.slot)
      if (hit) { delete r.pins[hit.slot]; bump(r, hit.slot) }
      void run(r, true)
      toast('Banished. It will not trouble you again.')
    },
    clearExclusions: () => { const r = keepAll(base()); r.exclusions = []; void run(r, true) },
    reword: () => { const r = keepAll(base()); bump(r, 'wording'); void run(r, true) },
    rerollTitle: () => { const r = keepAll(base()); r.titleOverride = null; bump(r, 'title'); void run(r, true) },
    toggleTitleLock: () => {
      const r = keepAll(base())
      r.titleOverride = r.titleOverride ? null : conceptRef.current?.title ?? null
      void run(r, true)
    },
    renameTitle: (title) => { const r = keepAll(base()); r.titleOverride = title.trim() || null; void run(r, true) },
    setSeed: async (seed) => {
      const text = seed.trim()
      if (/^bgg1-/i.test(text)) {
        try {
          const payload = await api.decodeRecipe(text)
          const missing = payload.packs.filter((id) => !boot.librarySignature.some((p) => p.id === id))
          if (missing.length) toast(`This recipe used packs you don't have enabled: ${missing.join(', ')}. Results may differ.`, 'error')
          void run(normalizeRecipe(payload.recipe))
        } catch (e) { toast((e as Error).message, 'error') }
        return
      }
      const r = base()
      r.seed = text || randomSeed(); r.rerolls = {}; r.keep = {}; r.creep = freshCreep(r)
      void run(r)
    },
    daily: () => {
      const r = base()
      r.seed = dailySeed(); r.rerolls = {}; r.keep = {}; r.pins = {}; r.creep = freshCreep(r)
      void run(r)
      toast("Today's bad idea. Same for everyone with the same packs.")
    },
    setSliders: (patch) => {
      // Sliders set up the NEXT idea; they don't silently replace the current one.
      setControls((c) => ({ ...c, ...patch }))
      controlsRef.current = { ...controlsRef.current, ...patch }
    },
    applySliders: () => {
      const r = base()
      r.keep = {}
      void run(r, true)
    },
    toggleCreep: () => {
      if (noorRef.current) { toast("Nice try. Noor Mode is on, and Feature Creep is not optional. 💜"); return }
      const on = !controlsRef.current.featureCreep
      controlsRef.current = { ...controlsRef.current, featureCreep: on }
      const r = keepAll(base())
      void run(r, true)
      toast(on ? 'Feature Creep Mode ON. Just one more system...' : 'Feature Creep Mode off. Your producer thanks you.')
    },
    setCreepKinds: (kinds) => {
      controlsRef.current = { ...controlsRef.current, creepKinds: kinds }
      const r = keepAll(base())
      void run(r, true)
    },
    creep: (slot, action) => {
      const r = keepAll(base())
      const s = r.creep[slot]
      if (!s) return
      if (action === 'accept') s.status = 'accepted'
      else if (action === 'reject') s.status = 'rejected'
      else if (action === 'undo') s.status = 'proposed'
      else { s.nonce += 1; s.status = 'proposed' }
      void run(r, true)
    },
    anotherThing: () => {
      controlsRef.current = { ...controlsRef.current, featureCreep: true }
      const r = keepAll(base())
      if (r.creep.length === 0) r.creep = []
      r.creep.push({ nonce: 0, status: 'proposed' })
      void run(r, true)
    },
    back: () => setHistory((h) => {
      if (h.index <= 0) return h
      const c = h.list[h.index - 1]
      setConcept(c); setControls(normalizeRecipe(c.recipe))
      return { ...h, index: h.index - 1 }
    }),
    forward: () => setHistory((h) => {
      if (h.index >= h.list.length - 1) return h
      const c = h.list[h.index + 1]
      setConcept(c); setControls(normalizeRecipe(c.recipe))
      return { ...h, index: h.index + 1 }
    }),
    loadEntry: (entry) => {
      // Opens the saved snapshot itself (not a regeneration), so it is exactly what was saved.
      const c = { ...entry.concept, title: entry.title }
      setConcept(c)
      setControls(normalizeRecipe(c.recipe))
      setLinkedEntryId(entry.id)
      setSavedSnapshot(JSON.stringify(c))
      setNotesState(entry.notes)
      setHistory((h) => { const list = [...h.list, c].slice(-50); return { list, index: list.length - 1 } })
      setPage('generator')
    },
    save: async () => {
      const c = conceptRef.current
      if (!c) return
      const entry = await api.vaultSave(c, notesRef.current)
      setLinkedEntryId(entry.id)
      setSavedSnapshot(JSON.stringify(c))
      await refreshVault()
      toast(`"${c.title}" is safe in the Vault.`, 'success')
    },
    updateSaved: async () => {
      const c = conceptRef.current
      const id = linkedRef.current
      if (!c || !id) return
      await api.vaultUpdate(id, { concept: c, title: c.title, notes: notesRef.current })
      setSavedSnapshot(JSON.stringify(c))
      await refreshVault()
      toast('Saved idea updated.', 'success')
    },
    copyMarkdown: async () => {
      const c = conceptRef.current
      if (!c) return
      const r = await api.exportConcept(c, 'markdown')
      const noor = noorRef.current ? '\n💜 Feature creep approved by Noor.\n' : ''
      await copyText((notesRef.current ? `${r.content}\n## Notes\n\n${notesRef.current}\n` : r.content) + noor)
      toast('Markdown copied. Paste responsibly.', 'success')
    },
    exportAs: async (format) => {
      const c = conceptRef.current
      if (!c) return
      const id = linkedRef.current
      const r = id ? await api.exportEntry(id, format) : await api.exportConcept(c, format)
      download(r.fileName, r.mime, r.content)
    },
    shareCode: async () => {
      const c = conceptRef.current
      if (!c) return
      const { code } = await api.encodeRecipe(c.recipe)
      await copyText(code)
      toast('Recipe code copied. Paste it into the seed box to reproduce this idea exactly.', 'success')
    },
  }), [base, run, toast, refreshVault, boot.librarySignature])

  const setNotes = useCallback((n: string) => {
    setNotesState(n)
    const id = linkedRef.current
    if (id) {
      window.clearTimeout((setNotes as unknown as { t?: number }).t)
      ;(setNotes as unknown as { t?: number }).t = window.setTimeout(() => {
        void api.vaultUpdate(id, { notes: n }).then(refreshVault)
      }, 600)
    }
  }, [refreshVault])

  const setOutputMode = useCallback((m: OutputMode) => {
    setOutputModeState(m)
    void api.updateSettings({ outputMode: m })
  }, [])

  const setTheme = useCallback((id: string) => {
    void api.updateSettings({ theme: id }).then((settings) => setBoot((b) => ({ ...b, settings })))
  }, [])

  const setNoorMode = useCallback((on: boolean) => {
    void api.updateSettings({ noorMode: on }).then((settings) => {
      noorRef.current = on
      setBoot((b) => ({ ...b, settings }))
      if (on) {
        toast('💜 Noor Mode activated. Sage has been notified. Sage is sighing.', 'success')
        setHeartBurst((n) => n + 1)
        // Creep on, plus Noor's extra proposal, applied to the current idea.
        const r = base()
        r.featureCreep = true
        while (r.creep.length < noorCreepSlots(r.scope)) r.creep.push({ nonce: 0, status: 'proposed' })
        controlsRef.current = { ...controlsRef.current, featureCreep: true }
        void run(keepAll(r), true)
      } else {
        toast('Noor Mode off. She left a heart on your desk anyway. 💜')
      }
    })
  }, [base, run, toast])

  const goToHistory = useCallback((index: number) => {
    setHistory((h) => {
      const c = h.list[index]
      if (!c) return h
      setConcept(c); setControls(normalizeRecipe(c.recipe))
      setLinkedEntryId(null); setSavedSnapshot(null); setNotesState('')
      return { ...h, index }
    })
  }, [])

  const pendingSliders = !!concept && (Math.abs(concept.recipe.chaos - controls.chaos) > 0.001
    || concept.recipe.scope !== controls.scope || Math.abs(concept.recipe.silliness - controls.silliness) > 0.001)

  const isSaved = !!linkedEntryId && !!concept && savedSnapshot === JSON.stringify(concept)

  const value: Studio = {
    boot, refreshBoot, page, setPage, concept, controls, busy, outputMode, setOutputMode, setTheme,
    noorMode: boot.settings.noorMode, setNoorMode, heartBurst,
    vault, refreshVault, linkedEntryId, isSaved, notes, setNotes, toasts, toast,
    canBack: history.index > 0, canForward: history.index < history.list.length - 1, a,
    history: history.list, historyIndex: history.index, goToHistory, changes, pendingSliders, hoverSlot, setHoverSlot,
  }
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>
}

const HISTORY_KEY = 'bgg.history.v1'

function loadHistory(): Concept[] {
  try {
    const raw = localStorage.getItem(HISTORY_KEY)
    const list = raw ? (JSON.parse(raw) as Concept[]) : []
    return Array.isArray(list) ? list.filter((c) => c && c.recipe && c.title) : []
  } catch { return [] }
}

function saveHistory(list: Concept[]) {
  try { localStorage.setItem(HISTORY_KEY, JSON.stringify(list.slice(-50))) } catch { /* storage full or blocked: history is a convenience */ }
}

/** Which parts of a concept changed between two versions of the same idea. */
function diffConcepts(prev: Concept, next: Concept): Changes {
  const before = new Map(prev.picks.map((p) => [p.slot, p.itemId]))
  const prevCreep = new Map(prev.creep.map((c) => [c.slot, c.itemId]))
  return {
    stamp: Date.now(),
    slots: new Set(next.picks.filter((p) => before.get(p.slot) !== p.itemId).map((p) => p.slot)),
    title: prev.title !== next.title,
    pitch: prev.pitch !== next.pitch || prev.hook !== next.hook,
    creep: new Set(next.creep.filter((c) => prevCreep.get(c.slot) !== c.itemId).map((c) => c.slot)),
  }
}
