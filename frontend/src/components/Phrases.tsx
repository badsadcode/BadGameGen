import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Ban, ChevronDown, Lock, LockOpen, RefreshCw } from 'lucide-react'
import { useStudio } from '../store'
import type { TextPart } from '../types'
import { ItemPicker } from './GeneratorPanel'

/**
 * Renders hook/pitch text where every phrase that came from an ingredient is clickable.
 * Hovering highlights the matching ingredient row (and vice versa); clicking opens PhraseMenu.
 */
export function Phrases({ parts, fallback }: { parts?: TextPart[]; fallback: string }) {
  const { hoverSlot, setHoverSlot } = useStudio()
  const [menu, setMenu] = useState<{ slot: string; rect: DOMRect } | null>(null)
  // Anchor the menu to the line that was clicked (a wrapped phrase has several line boxes).
  const openAt = (slot: string, el: HTMLElement, x?: number, y?: number) => {
    const rects = [...el.getClientRects()]
    const rect = (x !== undefined && y !== undefined && rects.find((r) => y >= r.top && y <= r.bottom && x >= r.left - 2 && x <= r.right + 2)) || rects[0] || el.getBoundingClientRect()
    setMenu({ slot, rect })
  }
  if (!parts || parts.length === 0) return <>{fallback}</>
  return (
    <>
      {parts.map((p, i) => p.slot
        ? (
          // A span (not a <button>) so long phrases wrap like normal text instead of as a block.
          <span key={i} role="button" tabIndex={0} className={`phrase ${hoverSlot === p.slot ? 'linked' : ''} ${menu?.slot === p.slot ? 'open' : ''}`}
            onMouseEnter={() => setHoverSlot(p.slot!)} onMouseLeave={() => setHoverSlot(null)}
            onClick={(e) => openAt(p.slot!, e.currentTarget, e.clientX, e.clientY)}
            onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); openAt(p.slot!, e.currentTarget) } }}>
            {p.text}
          </span>
        )
        : <span key={i}>{p.text}</span>)}
      {menu && <PhraseMenu slot={menu.slot} anchor={menu.rect} onClose={() => setMenu(null)} />}
    </>
  )
}

function PhraseMenu({ slot, anchor, onClose }: { slot: string; anchor: DOMRect; onClose: () => void }) {
  const { concept, controls, boot, a } = useStudio()
  const ref = useRef<HTMLDivElement>(null)
  const [pos, setPos] = useState({ top: anchor.bottom + 6, left: anchor.left })
  const [picking, setPicking] = useState(false)
  const pick = concept?.picks.find((p) => p.slot === slot)

  // Keep the menu on screen.
  useLayoutEffect(() => {
    const el = ref.current
    if (!el) return
    const w = el.offsetWidth, h = el.offsetHeight
    const left = Math.max(8, Math.min(anchor.left, window.innerWidth - w - 8))
    const below = anchor.bottom + 6
    const top = below + h > window.innerHeight - 8 ? Math.max(8, anchor.top - h - 6) : below
    setPos({ top, left })
  }, [anchor, picking])

  useEffect(() => {
    const down = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) onClose() }
    const key = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    const t = setTimeout(() => document.addEventListener('mousedown', down), 0)
    document.addEventListener('keydown', key)
    window.addEventListener('resize', onClose)
    return () => { clearTimeout(t); document.removeEventListener('mousedown', down); document.removeEventListener('keydown', key); window.removeEventListener('resize', onClose) }
  }, [onClose])

  if (!pick) return null
  const locked = !!controls.pins[slot]
  const category = boot.categories.find((c) => c.key === pick.category)
  const kind = pick.field === 'secondary' ? 'Supporting mechanic' : pick.field === 'mood' ? 'Tone' : category?.displayName ?? pick.field
  const alts = concept?.alternatives?.[slot] ?? []
  const act = (fn: () => void) => { onClose(); fn() }

  return (
    <div ref={ref} className="phrase-menu" style={{ top: pos.top, left: pos.left }} role="dialog" aria-label={`${kind}: ${pick.label}`}>
      <div className="phrase-menu-head">
        <span className="muted small">{kind}</span>
        <strong>{pick.label}</strong>
      </div>
      {alts.length > 0 && (
        <div className="phrase-alts">
          <span className="muted small">Swap for:</span>
          {alts.map((alt) => <button key={alt.itemId} className="alt-chip" onClick={() => act(() => a.pickItem(slot, alt.itemId))}>{alt.label}</button>)}
        </div>
      )}
      <div className="phrase-actions">
        <button className="mini-btn" onClick={() => act(() => a.rerollSlot(slot))}><RefreshCw size={13} /> Reroll</button>
        <button className={`mini-btn ${locked ? 'active' : ''}`} onClick={() => act(() => a.toggleLock(slot))}>{locked ? <><Lock size={13} /> Unlock</> : <><LockOpen size={13} /> Lock</>}</button>
        <button className="mini-btn" onClick={() => setPicking((p) => !p)} aria-expanded={picking}><ChevronDown size={13} /> Choose…</button>
        <button className="mini-btn bad" onClick={() => act(() => a.excludeItem(pick.itemId))} title="Never suggest this again"><Ban size={13} /> Banish</button>
      </div>
      {picking && (
        <div className="phrase-picker">
          <ItemPicker category={pick.category} current={pick.itemId} onPick={(id) => act(() => a.pickItem(slot, id))} onExclude={() => act(() => a.excludeItem(pick.itemId))} />
        </div>
      )}
    </div>
  )
}
