import { useEffect, useState } from 'react'
import { Ban, ChevronDown, Copy, Dices, Lock, LockOpen, RefreshCw, Search, Shuffle, Sun, Wand2 } from 'lucide-react'
import { api } from '../api'
import { useStudio } from '../store'
import type { ConceptPick, CreepKind, OptionDto } from '../types'
import { iconFor } from './icons'
import { Logo } from './Logo'
import { Popover, Segmented, Slider, Toggle } from './ui'
import { copyText } from '../lib/util'

/** Fields shown up front, as in the mockup. Everything else lives under "More ingredients". */
const MAIN_FIELDS = ['genre', 'setting', 'mechanic', 'mood', 'visual', 'twist']
const MORE_FIELDS = ['subgenre', 'era', 'protagonist', 'perspective', 'objective', 'loop', 'secondary', 'progression', 'threat', 'resource', 'multiplayer']

export function GeneratorPanel() {
  const { concept, controls, boot, a } = useStudio()
  const [showMore, setShowMore] = useState(false)
  const picks = concept?.picks ?? []
  const category = (key: string) => boot.categories.find((c) => c.key === key)

  const rows = (fields: string[]) => fields.flatMap((field) =>
    picks.filter((p) => p.field === field).map((p, i, all) => (
      <IngredientRow key={p.slot} pick={p}
        label={field === 'secondary' ? `Support ${all.length > 1 ? i + 1 : ''}` : field === 'mood' ? 'Tone' : field === 'visual' ? 'Visuals' : category(p.category)?.displayName ?? field}
        icon={category(p.category)?.icon ?? 'sparkles'} />
    )))

  return (
    <aside className="panel left-panel" aria-label="Generation controls">
      <div className="panel-head">
        <div>
          <h2><Dices size={20} /> Generate a Game Idea</h2>
          <p className="muted small">Pick some ingredients. The worse the combo, the better. ✨</p>
        </div>
        <button className="icon-btn" title="Bad idea of the day" onClick={a.daily}><Sun size={18} /></button>
      </div>

      <div className="ingredients">{rows(MAIN_FIELDS)}</div>
      <button className="more-toggle" onClick={() => setShowMore((s) => !s)} aria-expanded={showMore}>
        <ChevronDown size={16} className={showMore ? 'rot' : ''} /> {showMore ? 'Fewer ingredients' : `More ingredients (${picks.filter((p) => MORE_FIELDS.includes(p.field)).length})`}
      </button>
      {showMore && <div className="ingredients">{rows(MORE_FIELDS)}</div>}
      {controls.exclusions.length > 0 && (
        <button className="link small" onClick={a.clearExclusions}>Un-banish {controls.exclusions.length} excluded item{controls.exclusions.length > 1 ? 's' : ''}</button>
      )}

      <div className="divider" />

      <div className="control-grid">
        <label className="control-label"><span className="ico">🌀</span> Chaos</label>
        <Slider ariaLabel="Chaos" value={controls.chaos} showValue onCommit={(v) => a.setSliders({ chaos: v })} />

        <label className="control-label"><span className="ico">📦</span> Scope</label>
        <Segmented ariaLabel="Scope" value={controls.scope} onChange={(v) => a.setSliders({ scope: v })}
          options={['Tiny', 'Small', 'Medium', 'Large', 'Absurd'].map((label, i) => ({ value: i, label, title: boot.scopeNames[i] }))} />

        <label className="control-label"><span className="ico">🤡</span> Serious ↔ Silly</label>
        <Slider ariaLabel="Serious to silly" value={controls.silliness} leftLabel="Serious" rightLabel="Silly" onCommit={(v) => a.setSliders({ silliness: v })} />

        <label className="control-label"><span className="ico">🎲</span> Seed</label>
        <SeedField />
      </div>

      <FeatureCreepCard />
    </aside>
  )
}

function SeedField() {
  const { controls, a, toast } = useStudio()
  const [text, setText] = useState(controls.seed)
  useEffect(() => setText(controls.seed), [controls.seed])
  return (
    <div className="seed-field">
      <input value={text} onChange={(e) => setText(e.target.value)} aria-label="Seed"
        placeholder="Any number, word or BGG1- code" spellCheck={false}
        onKeyDown={(e) => { if (e.key === 'Enter') a.setSeed(text) }} />
      <button className="icon-btn" title="Copy seed" onClick={() => { void copyText(controls.seed); toast('Seed copied.') }}><Copy size={16} /></button>
      <button className="icon-btn" title="Replay this seed (Enter)" onClick={() => a.setSeed(text)}><Shuffle size={16} /></button>
    </div>
  )
}

function IngredientRow({ pick, label, icon }: { pick: ConceptPick; label: string; icon: string }) {
  const { a, controls } = useStudio()
  const [open, setOpen] = useState(false)
  const Icon = iconFor(icon)
  const locked = !!controls.pins[pick.slot]
  return (
    <div className="ingredient">
      <span className="ingredient-label"><Icon size={17} /> {label}</span>
      <div className="ingredient-value">
        <button className={`chip pick ${locked ? 'locked' : ''}`} onClick={() => setOpen((o) => !o)} title="Choose a specific one">
          <span className="chip-text">{pick.label}</span><ChevronDown size={14} />
        </button>
        <Popover open={open} onClose={() => setOpen(false)} className="picker-pop">
          <ItemPicker category={pick.category} current={pick.itemId}
            onPick={(id) => { setOpen(false); a.pickItem(pick.slot, id) }}
            onExclude={() => { setOpen(false); a.excludeItem(pick.itemId) }} />
        </Popover>
      </div>
      <button className={`icon-btn sm ${locked ? 'active' : ''}`} title={locked ? 'Unlock' : 'Lock (keeps it on Generate Trouble)'} aria-pressed={locked} onClick={() => a.toggleLock(pick.slot)}>
        {locked ? <Lock size={15} /> : <LockOpen size={15} />}
      </button>
      <button className="icon-btn sm" title="Reroll just this" onClick={() => a.rerollSlot(pick.slot)}><RefreshCw size={15} /></button>
    </div>
  )
}

function ItemPicker({ category, current, onPick, onExclude }: { category: string; current: string; onPick: (id: string) => void; onExclude: () => void }) {
  const [query, setQuery] = useState('')
  const [options, setOptions] = useState<OptionDto[]>([])
  useEffect(() => {
    const t = setTimeout(() => { void api.options(category, query).then(setOptions) }, 120)
    return () => clearTimeout(t)
  }, [category, query])
  return (
    <div className="picker">
      <div className="picker-search"><Search size={15} /><input autoFocus value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search by name or tag…" /></div>
      <ul role="listbox">
        {options.slice(0, 200).map((o) => (
          <li key={o.id} role="option" aria-selected={o.id === current}>
            <button className={o.id === current ? 'current' : ''} onClick={() => onPick(o.id)}>
              <span>{o.label}</span>
              <span className="picker-meta">{o.packId}{o.weirdness > 0.65 ? ' · weird' : ''}</span>
            </button>
          </li>
        ))}
        {options.length === 0 && <li className="muted small pad">Nothing matches. Suspiciously sensible of you.</li>}
      </ul>
      <button className="picker-exclude" onClick={onExclude}><Ban size={14} /> Never suggest the current one again</button>
    </div>
  )
}

const CREEP_KINDS: { kind: CreepKind; label: string }[] = [
  { kind: 'system', label: '+ side systems' },
  { kind: 'loop', label: '+ extra loops' },
  { kind: 'content', label: '+ more content' },
  { kind: 'whyNot', label: '+ why not?' },
]

function FeatureCreepCard() {
  const { controls, a } = useStudio()
  const on = controls.featureCreep
  const toggleKind = (kind: CreepKind) => {
    const kinds = controls.creepKinds.includes(kind) ? controls.creepKinds.filter((k) => k !== kind) : [...controls.creepKinds, kind]
    if (kinds.length > 0) a.setCreepKinds(kinds)
  }
  return (
    <section className={`creep-card ${on ? 'on' : ''}`} aria-label="Feature Creep Mode">
      <div className="creep-card-head">
        <Logo size={46} className="creep-cat" />
        <div className="creep-title">
          <h3>Feature Creep Mode</h3>
          <p>“Just one more system...”</p>
        </div>
        <Toggle on={on} onChange={a.toggleCreep} label="Feature Creep Mode" />
      </div>
      <div className="creep-kinds">
        {CREEP_KINDS.map((k) => (
          <button key={k.kind} className={`chip small ${controls.creepKinds.includes(k.kind) ? 'active' : ''}`} aria-pressed={controls.creepKinds.includes(k.kind)} onClick={() => toggleKind(k.kind)}>{k.label}</button>
        ))}
      </div>
      {on && <button className="btn ghost small full" onClick={a.anotherThing}><Wand2 size={15} /> And another thing...</button>}
    </section>
  )
}
