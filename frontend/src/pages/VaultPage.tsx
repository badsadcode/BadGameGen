import { useMemo, useState } from 'react'
import { AlertTriangle, Copy, Download, ExternalLink, Search, Star, Trash2 } from 'lucide-react'
import { api } from '../api'
import { useStudio } from '../store'
import type { VaultEntry } from '../types'
import { ConfirmDialog, Empty } from '../components/ui'
import { download, sameLibrary, timeAgo } from '../lib/util'

type Sort = 'updated' | 'created' | 'title'

export function VaultPage() {
  const { vault, refreshVault, a, toast, boot } = useStudio()
  const [query, setQuery] = useState('')
  const [onlyFav, setOnlyFav] = useState(false)
  const [sort, setSort] = useState<Sort>('updated')
  const [selectedId, setSelectedId] = useState<string | null>(vault[0]?.id ?? null)
  const [confirmDelete, setConfirmDelete] = useState<VaultEntry | null>(null)

  const list = useMemo(() => {
    const q = query.trim().toLowerCase()
    const filtered = vault.filter((e) => (!onlyFav || e.favorite) && (!q
      || [e.title, e.concept.hook, e.notes, ...e.tags, ...e.concept.picks.map((p) => p.label)].some((t) => t.toLowerCase().includes(q))))
    return [...filtered].sort((x, y) => sort === 'title' ? x.title.localeCompare(y.title)
      : sort === 'created' ? y.createdAt.localeCompare(x.createdAt) : y.updatedAt.localeCompare(x.updatedAt))
  }, [vault, query, onlyFav, sort])
  const selected = vault.find((e) => e.id === selectedId) ?? list[0] ?? null

  const update = async (id: string, patch: Parameters<typeof api.vaultUpdate>[1]) => { await api.vaultUpdate(id, patch); await refreshVault() }

  return (
    <div className="page vault-page">
      <section className="panel vault-list-panel">
        <div className="panel-head"><h2>Idea Vault</h2><span className="count-pill">{vault.length}</span></div>
        <div className="search"><Search size={15} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search titles, notes, tags, ingredients…" /></div>
        <div className="filter-row">
          <button className={`chip small ${onlyFav ? 'active' : ''}`} onClick={() => setOnlyFav((f) => !f)}><Star size={13} /> Favorites</button>
          <select value={sort} onChange={(e) => setSort(e.target.value as Sort)} aria-label="Sort">
            <option value="updated">Recently changed</option>
            <option value="created">Recently created</option>
            <option value="title">Title A–Z</option>
          </select>
        </div>
        <ul className="vault-list tall">
          {list.map((e) => (
            <li key={e.id} className={`vault-card ${selected?.id === e.id ? 'current' : ''}`}>
              <button className="vault-card-main" onClick={() => setSelectedId(e.id)}>
                <span className="vault-title">{e.title}</span>
                <span className="vault-hook">{e.concept.hook}</span>
                <span className="muted small">{timeAgo(e.updatedAt)}</span>
              </button>
              <button className={`star ${e.favorite ? 'on' : ''}`} aria-label="Favorite" onClick={() => void update(e.id, { favorite: !e.favorite })}><Star size={17} /></button>
            </li>
          ))}
          {list.length === 0 && <Empty>{vault.length ? 'No matches.' : 'The Vault is empty. Go generate some trouble.'}</Empty>}
        </ul>
      </section>

      <section className="panel vault-detail">
        {selected ? <EntryDetail key={selected.id} entry={selected} onUpdate={update}
          onOpen={() => a.loadEntry(selected)}
          onDuplicate={async () => { const d = await api.vaultDuplicate(selected.id); await refreshVault(); setSelectedId(d.id); toast('Duplicated.') }}
          onDelete={() => setConfirmDelete(selected)}
          libraryChanged={!sameLibrary(selected.concept, boot.librarySignature)} />
          : <Empty>Select an idea to see it here.</Empty>}
      </section>

      {confirmDelete && (
        <ConfirmDialog danger title="Delete this idea?" confirmLabel="Delete forever"
          body={`"${confirmDelete.title}" will be gone for good. Even bad ideas deserve a moment of silence.`}
          onCancel={() => setConfirmDelete(null)}
          onConfirm={async () => { await api.vaultDelete(confirmDelete.id); setConfirmDelete(null); setSelectedId(null); await refreshVault(); toast('Deleted.') }} />
      )}
    </div>
  )
}

function EntryDetail({ entry, onUpdate, onOpen, onDuplicate, onDelete, libraryChanged }: {
  entry: VaultEntry
  onUpdate: (id: string, patch: Parameters<typeof api.vaultUpdate>[1]) => Promise<void>
  onOpen: () => void; onDuplicate: () => void; onDelete: () => void; libraryChanged: boolean
}) {
  const [title, setTitle] = useState(entry.title)
  const [notes, setNotes] = useState(entry.notes)
  const [tagText, setTagText] = useState('')
  const c = entry.concept
  const exportAs = async (format: 'markdown' | 'text' | 'json') => { const r = await api.exportEntry(entry.id, format); download(r.fileName, r.mime, r.content) }

  return (
    <div className="entry-detail">
      <input className="entry-title concept-title" value={title} onChange={(e) => setTitle(e.target.value)}
        onBlur={() => title.trim() && title !== entry.title && void onUpdate(entry.id, { title })} aria-label="Title" />
      <p className="hook">{c.hook}</p>
      <div className="chips-row">{c.chips.map((ch) => <span key={ch.kind + ch.label} className={`tag tag-${ch.kind}`}>{ch.label}</span>)}</div>
      {libraryChanged && <p className="scope-warning"><AlertTriangle size={14} /> Made with a different set of packs than you have enabled now. The saved text is intact; replaying the seed may give a different result.</p>}
      <p className="pitch">{c.pitch}</p>
      {c.creep.filter((x) => x.status === 'accepted').length > 0 && (
        <div className="accepted-creep"><strong>Accepted feature creep:</strong> {c.creep.filter((x) => x.status === 'accepted').map((x) => x.name).join(', ')}</div>
      )}
      <div className="entry-actions">
        <button className="btn primary" onClick={onOpen}><ExternalLink size={16} /> Open in Generator</button>
        <button className="btn" onClick={onDuplicate}><Copy size={16} /> Duplicate</button>
        <button className="btn" onClick={() => void exportAs('markdown')}><Download size={16} /> .md</button>
        <button className="btn" onClick={() => void exportAs('text')}>.txt</button>
        <button className="btn" onClick={() => void exportAs('json')}>.json</button>
        <button className="btn danger-ghost" onClick={onDelete}><Trash2 size={16} /> Delete</button>
      </div>
      <h4>Tags</h4>
      <div className="tag-editor">
        {entry.tags.map((t) => (
          <span key={t} className="tag removable">{t}<button aria-label={`Remove ${t}`} onClick={() => void onUpdate(entry.id, { tags: entry.tags.filter((x) => x !== t) })}>×</button></span>
        ))}
        <input value={tagText} placeholder="+ add tag" onChange={(e) => setTagText(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter' && tagText.trim()) { void onUpdate(entry.id, { tags: [...entry.tags, tagText.trim()] }); setTagText('') } }} />
      </div>
      <h4>Notes</h4>
      <textarea className="notes-big" value={notes} onChange={(e) => setNotes(e.target.value)} onBlur={() => notes !== entry.notes && void onUpdate(entry.id, { notes })} placeholder="Why this one? What would the demo be?" />
      <p className="muted small">Created {new Date(entry.createdAt).toLocaleString()} · {c.codename} · seed {c.recipe.seed} · packs {c.library.map((p) => `${p.id}@${p.version}`).join(', ')}</p>
    </div>
  )
}
