import { useEffect, useRef, useState, type ReactNode } from 'react'
import { useStudio } from '../store'

export function Segmented<T extends string | number>({ value, options, onChange, ariaLabel }: {
  value: T; options: { value: T; label: string; title?: string }[]; onChange: (v: T) => void; ariaLabel: string
}) {
  return (
    <div className="segmented" role="radiogroup" aria-label={ariaLabel}>
      {options.map((o) => (
        <button key={String(o.value)} role="radio" aria-checked={o.value === value} title={o.title}
          className={o.value === value ? 'active' : ''} onClick={() => onChange(o.value)}>{o.label}</button>
      ))}
    </div>
  )
}

/** Slider that only commits on release, so dragging doesn't spam the generator. */
export function Slider({ value, onCommit, ariaLabel, leftLabel, rightLabel, showValue }: {
  value: number; onCommit: (v: number) => void; ariaLabel: string; leftLabel?: string; rightLabel?: string; showValue?: boolean
}) {
  const [local, setLocal] = useState(value)
  useEffect(() => setLocal(value), [value])
  const commit = () => { if (Math.abs(local - value) > 0.001) onCommit(local) }
  return (
    <div className="slider-wrap">
      <div className="slider-row">
        <input type="range" min={0} max={1} step={0.01} value={local} aria-label={ariaLabel}
          style={{ ['--fill' as string]: `${local * 100}%` }}
          onChange={(e) => setLocal(Number(e.target.value))} onPointerUp={commit} onKeyUp={commit} onBlur={commit} />
        {showValue && <span className="slider-value">{Math.round(local * 100)}%</span>}
      </div>
      {(leftLabel || rightLabel) && <div className="slider-labels"><span>{leftLabel}</span><span>{rightLabel}</span></div>}
    </div>
  )
}

export function Toggle({ on, onChange, label }: { on: boolean; onChange: () => void; label: string }) {
  return <button className={`toggle ${on ? 'on' : ''}`} role="switch" aria-checked={on} aria-label={label} onClick={onChange}><span /></button>
}

export function Toasts() {
  const { toasts } = useStudio()
  return (
    <div className="toasts" aria-live="polite">
      {toasts.map((t) => <div key={t.id} className={`toast ${t.kind}`}>{t.text}</div>)}
    </div>
  )
}

/** Closes when clicking outside or pressing Escape. */
export function Popover({ open, onClose, children, className }: { open: boolean; onClose: () => void; children: ReactNode; className?: string }) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const down = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) onClose() }
    const key = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    setTimeout(() => document.addEventListener('mousedown', down), 0)
    document.addEventListener('keydown', key)
    return () => { document.removeEventListener('mousedown', down); document.removeEventListener('keydown', key) }
  }, [open, onClose])
  if (!open) return null
  return <div ref={ref} className={`popover ${className ?? ''}`}>{children}</div>
}

export function ConfirmDialog({ title, body, confirmLabel, onConfirm, onCancel, danger }: {
  title: string; body: string; confirmLabel: string; onConfirm: () => void; onCancel: () => void; danger?: boolean
}) {
  useEffect(() => {
    const key = (e: KeyboardEvent) => { if (e.key === 'Escape') onCancel() }
    document.addEventListener('keydown', key)
    return () => document.removeEventListener('keydown', key)
  }, [onCancel])
  return (
    <div className="modal-backdrop" onMouseDown={onCancel}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(e) => e.stopPropagation()}>
        <h3>{title}</h3>
        <p>{body}</p>
        <div className="modal-actions">
          <button className="btn ghost" onClick={onCancel} autoFocus>Cancel</button>
          <button className={`btn ${danger ? 'danger' : 'primary'}`} onClick={onConfirm}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  )
}

export function Empty({ children }: { children: ReactNode }) {
  return <div className="empty">{children}</div>
}
