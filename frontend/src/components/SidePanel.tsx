import { useMemo, useState } from 'react'
import { Bookmark, ChevronDown, Clock, History, Layers, NotebookPen, Search, Star, Tags } from 'lucide-react'
import { api } from '../api'
import { useStudio } from '../store'
import type { Concept, VaultEntry } from '../types'

type Tab = 'favorites' | 'recent' | 'tags' | 'history'

export function SidePanel() {
  const { vault, a, refreshVault, linkedEntryId, setPage, history, historyIndex, goToHistory } = useStudio()
  const [tab, setTab] = useState<Tab>('recent')
  const [query, setQuery] = useState('')
  const [tag, setTag] = useState<string | null>(null)

  const allTags = useMemo(() => [...new Set(vault.flatMap((e) => e.tags))].sort(), [vault])
  const list = useMemo(() => {
    const q = query.trim().toLowerCase()
    return vault
      .filter((e) => tab !== 'favorites' || e.favorite)
      .filter((e) => tab !== 'tags' || !tag || e.tags.includes(tag))
      .filter((e) => !q || e.title.toLowerCase().includes(q) || e.concept.hook.toLowerCase().includes(q) || e.tags.some((t) => t.toLowerCase().includes(q)))
  }, [vault, tab, query, tag])

  const favCount = vault.filter((e) => e.favorite).length
  const star = async (e: VaultEntry) => { await api.vaultUpdate(e.id, { favorite: !e.favorite }); await refreshVault() }

  return (
    <aside className="right-col" aria-label="Idea Vault and notes">
      <section className="panel vault-panel">
        <div className="panel-head">
          <h2><Layers size={20} /> Idea Vault</h2>
          <button className="count-pill" onClick={() => setPage('vault')} title="Open the full Vault">{vault.length} idea{vault.length === 1 ? '' : 's'}</button>
        </div>
        <div className="tabs" role="tablist">
          <button role="tab" aria-selected={tab === 'favorites'} className={tab === 'favorites' ? 'active' : ''} onClick={() => setTab('favorites')}><Star size={15} /> Favorites <span className="badge">{favCount}</span></button>
          <button role="tab" aria-selected={tab === 'recent'} className={tab === 'recent' ? 'active' : ''} onClick={() => setTab('recent')}><Clock size={15} /> Recent</button>
          <button role="tab" aria-selected={tab === 'tags'} className={tab === 'tags' ? 'active' : ''} onClick={() => setTab('tags')}><Tags size={15} /> Tags</button>
          <button role="tab" aria-selected={tab === 'history'} className={tab === 'history' ? 'active' : ''} onClick={() => setTab('history')} title="Every idea you generated recently, saved or not"><History size={15} /> Just made</button>
        </div>
        {tab === 'tags' && allTags.length > 0 && (
          <div className="tag-cloud">
            {allTags.map((t) => <button key={t} className={`chip small ${tag === t ? 'active' : ''}`} onClick={() => setTag(tag === t ? null : t)}>{t}</button>)}
          </div>
        )}
        {tab === 'history' ? <HistoryList items={history} index={historyIndex} onOpen={goToHistory} savedKeys={new Set(vault.map((e) => e.concept.codename + '|' + e.concept.pitch))} /> : <>
        <div className="search"><Search size={15} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search ideas…" aria-label="Search ideas" /></div>
        <ul className="vault-list">
          {list.map((e) => (
            <li key={e.id} className={`vault-card ${linkedEntryId === e.id ? 'current' : ''}`}>
              <button className="vault-card-main" onClick={() => a.loadEntry(e)}>
                <span className="vault-title">{e.title}</span>
                <span className="vault-hook">{e.concept.hook}</span>
                <span className="vault-tags">{e.tags.slice(0, 3).map((t) => <span key={t} className="tag mini">{t}</span>)}</span>
              </button>
              <button className={`star ${e.favorite ? 'on' : ''}`} aria-label={e.favorite ? 'Unfavorite' : 'Favorite'} onClick={() => void star(e)}><Star size={17} /></button>
            </li>
          ))}
          {list.length === 0 && (
            <li className="empty small">
              {vault.length === 0 ? 'No ideas saved yet. Hit Save to Vault on something terrible.' : 'Nothing here. Try another filter.'}
            </li>
          )}
        </ul>
        </>}
      </section>
      <NotesPanel />
    </aside>
  )
}

function NotesPanel() {
  const { notes, setNotes, linkedEntryId } = useStudio()
  const [open, setOpen] = useState(() => { try { return localStorage.getItem('bgg.notesOpen') !== '0' } catch { return true } })
  const toggle = () => setOpen((v) => { try { localStorage.setItem('bgg.notesOpen', v ? '0' : '1') } catch { /* fine */ } return !v })
  return (
    <section className={`panel notes-panel ${open ? '' : 'collapsed'}`}>
      <button className="panel-head notes-head" onClick={toggle} aria-expanded={open}>
        <h2><NotebookPen size={19} /> Notes {!open && notes && <span className="badge">•</span>}</h2>
        <span className="muted small">{open ? (linkedEntryId ? 'saved with the idea' : 'saved when you save the idea') : 'show'} <ChevronDown size={14} className={open ? 'rot' : ''} /></span>
      </button>
      {open && <textarea value={notes} onChange={(e) => setNotes(e.target.value)} aria-label="Notes"
        placeholder={'• Strong atmosphere\n• Keep scope in check (Feature Creep is watching...)\n• Possible demo: 7-day challenge?'} />}
    </section>
  )
}

function HistoryList({ items, index, onOpen, savedKeys }: { items: Concept[]; index: number; onOpen: (i: number) => void; savedKeys: Set<string> }) {
  const ordered = items.map((c, i) => ({ c, i })).reverse()
  return (
    <ul className="vault-list">
      {ordered.map(({ c, i }) => (
        <li key={i + c.codename} className={`vault-card ${i === index ? 'current' : ''}`}>
          <button className="vault-card-main" onClick={() => onOpen(i)}>
            <span className="vault-title">{c.title}</span>
            <span className="vault-hook">{c.hook}</span>
            <span className="muted small">{c.codename}</span>
          </button>
          {savedKeys.has(c.codename + '|' + c.pitch) && <span className="saved-mark" title="In the Vault"><Bookmark size={15} /></span>}
        </li>
      ))}
      {items.length === 0 && <li className="empty small">Nothing yet. Every idea you generate lands here, saved or not.</li>}
    </ul>
  )
}
