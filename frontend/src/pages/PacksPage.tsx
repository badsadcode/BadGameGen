import { useEffect, useRef, useState } from 'react'
import { AlertTriangle, Eye, FileUp, RotateCw, Search, Trash2 } from 'lucide-react'
import { api } from '../api'
import { useStudio } from '../store'
import type { PackContents, PackInfo } from '../types'
import { ConfirmDialog, Toggle } from '../components/ui'

export function PacksPage() {
  const { boot, refreshBoot, toast } = useStudio()
  const [packs, setPacks] = useState<PackInfo[]>(boot.packs)
  const [inspect, setInspect] = useState<PackContents | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<PackInfo | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)
  useEffect(() => setPacks(boot.packs), [boot.packs])

  const toggle = async (p: PackInfo) => {
    const next = await api.setPackEnabled(p.id, !p.enabled)
    setPacks(next)
    await refreshBoot()
    toast(`${p.name} ${p.enabled ? 'disabled' : 'enabled'}. Takes effect on the next generation.`)
  }
  const importFile = async (file: File) => {
    const r = await api.importPack(file.name, await file.text())
    if (r.ok) { toast(`Imported pack "${r.packId}".`, 'success'); await refreshBoot() }
    else toast(`Import failed: ${r.errors.slice(0, 3).join(' · ')}`, 'error')
  }
  const total = packs.filter((p) => p.enabled).reduce((n, p) => n + Object.values(p.counts).reduce((x, y) => x + y, 0), 0)

  return (
    <div className="page packs-page">
      <section className="panel">
        <div className="panel-head">
          <div><h2>Content Packs</h2><p className="muted small">{total} ingredients enabled. Packs are plain JSON; see docs/CONTENT_GUIDE.md to write your own.</p></div>
          <div className="row-gap">
            <button className="btn" onClick={async () => { await refreshBoot(await api.reload()); toast('Packs reloaded from disk.') }}><RotateCw size={16} /> Reload</button>
            <button className="btn primary" onClick={() => fileInput.current?.click()}><FileUp size={16} /> Import pack…</button>
            <input ref={fileInput} type="file" accept=".json,application/json" hidden onChange={(e) => { const f = e.target.files?.[0]; if (f) void importFile(f); e.target.value = '' }} />
          </div>
        </div>
        <div className="pack-grid">
          {packs.map((p) => (
            <article key={p.fileName} className={`pack-card ${p.enabled ? 'enabled' : ''} ${!p.usable ? 'broken' : ''}`}>
              <header>
                <div>
                  <h3>{p.name}</h3>
                  <span className="muted small">{p.id}@{p.version} · {p.isBuiltIn ? 'built-in' : 'imported'}{p.author ? ` · ${p.author}` : ''}</span>
                </div>
                {p.usable && <Toggle on={p.enabled} onChange={() => void toggle(p)} label={`Enable ${p.name}`} />}
              </header>
              <p>{p.description}</p>
              {p.usable ? (
                <div className="pack-counts">
                  {Object.entries(p.counts).map(([k, n]) => <span key={k} className="tag mini">{k} {n}</span>)}
                  {p.templateCount > 0 && <span className="tag mini">templates {p.templateCount}</span>}
                </div>
              ) : (
                <div className="pack-errors"><AlertTriangle size={14} /> This pack has problems and is not used:<ul>{p.errors.slice(0, 6).map((e) => <li key={e}>{e}</li>)}</ul></div>
              )}
              <footer>
                {p.usable && <button className="btn small" onClick={async () => setInspect(await api.packContents(p.id))}><Eye size={14} /> Inspect</button>}
                {!p.isBuiltIn && <button className="btn small danger-ghost" onClick={() => setConfirmDelete(p)}><Trash2 size={14} /> Remove</button>}
              </footer>
            </article>
          ))}
        </div>
      </section>
      {inspect && <PackInspector pack={inspect} onClose={() => setInspect(null)} />}
      {confirmDelete && (
        <ConfirmDialog danger title="Remove imported pack?" confirmLabel="Remove" body={`"${confirmDelete.name}" will be deleted from your packs folder. Saved ideas keep their text.`}
          onCancel={() => setConfirmDelete(null)}
          onConfirm={async () => { await api.deletePack(confirmDelete.id); setConfirmDelete(null); await refreshBoot(); toast('Pack removed.') }} />
      )}
    </div>
  )
}

function PackInspector({ pack, onClose }: { pack: PackContents; onClose: () => void }) {
  const categories = Object.keys(pack.items)
  const [cat, setCat] = useState(categories[0] ?? '')
  const [query, setQuery] = useState('')
  const q = query.toLowerCase()
  const items = (pack.items[cat] ?? []).filter((i) => !q || i.label.toLowerCase().includes(q) || i.tags.some((t) => t.includes(q)) || Object.values(i.forms).some((f) => f.toLowerCase().includes(q)))
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal inspector" role="dialog" aria-modal="true" aria-label={`Inspect ${pack.name}`} onMouseDown={(e) => e.stopPropagation()}>
        <div className="panel-head"><h3>{pack.name} <span className="muted small">{pack.id}@{pack.version}</span></h3><button className="btn small" onClick={onClose}>Close</button></div>
        <div className="inspector-body">
          <nav className="inspector-cats">
            {categories.map((c) => <button key={c} className={c === cat ? 'active' : ''} onClick={() => setCat(c)}>{c} <span className="muted">{pack.items[c].length}</span></button>)}
          </nav>
          <div className="inspector-items">
            <div className="search"><Search size={15} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Filter…" /></div>
            <table>
              <thead><tr><th>Label</th><th>Forms</th><th>Tags</th><th title="weirdness / silliness">W / S</th></tr></thead>
              <tbody>
                {items.map((i) => (
                  <tr key={i.id}>
                    <td><strong>{i.label}</strong><div className="muted small">{i.id}{i.kind ? ` · ${i.kind}` : ''}</div></td>
                    <td>{Object.entries(i.forms).map(([k, v]) => <div key={k}><span className="muted">{k}:</span> {v || <em className="muted">(empty)</em>}</div>)}</td>
                    <td>{i.tags.join(', ')}</td>
                    <td>{i.weirdness.toFixed(2)} / {i.silliness.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}
