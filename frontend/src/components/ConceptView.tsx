import { useEffect, useRef, useState } from 'react'
import {
  AlertTriangle, Bookmark, BookmarkCheck, Check, ChevronLeft, ChevronRight, Copy, Download, Eye, EyeOff, FlaskConical,
  Lock, LockOpen, Pencil, RefreshCw, Share2, Undo2, Wand2, X, Zap,
} from 'lucide-react'
import { useStudio } from '../store'
import type { Concept, CreepProposal } from '../types'
import { CoverArt } from './CoverArt'
import { Popover, Segmented } from './ui'
import { NoorBubble } from './Noor'
import { Phrases } from './Phrases'

export function ConceptView() {
  const { concept, busy, outputMode, setOutputMode, a, isSaved, linkedEntryId, canBack, canForward, changes } = useStudio()
  const [exportOpen, setExportOpen] = useState(false)
  const [showArt, setShowArt] = useState(() => { try { return localStorage.getItem('bgg.showArt') !== '0' } catch { return true } })
  const toggleArt = () => setShowArt((v) => { try { localStorage.setItem('bgg.showArt', v ? '0' : '1') } catch { /* fine */ } return !v })
  if (!concept) return <main className="panel center-panel loading-panel"><div className="spinner" /> Brewing trouble…</main>

  const chaos = Math.round(concept.recipe.chaos * 100)
  return (
    <main className={`panel center-panel ${busy ? 'busy' : ''}`} aria-label="Generated concept">
      <div className="concept-top">
        <div className="codename" title="Codename: BG-seed-scope letter (+C when Feature Creep is on)">
          <span>CODENAME</span> <strong>{concept.codename}</strong>
        </div>
        <div className="history-nav">
          <button className="icon-btn sm" disabled={!canBack} title="Previous idea (Alt+←)" onClick={a.back}><ChevronLeft size={16} /></button>
          <button className="icon-btn sm" disabled={!canForward} title="Next idea (Alt+→)" onClick={a.forward}><ChevronRight size={16} /></button>
        </div>
        <div className={`chaos-stamp ${chaos >= 70 ? 'hot' : ''}`} aria-label={`Chaos ${chaos}%`}>
          <span>CHAOS</span><strong>{chaos}%</strong>
        </div>
      </div>

      <TitleLine concept={concept} key={concept.codename + concept.title} />
      <p className={`hook ${changes.pitch ? 'flash-text' : ''}`} key={'h' + concept.hook}><Phrases parts={concept.hookParts} fallback={concept.hook} /></p>

      <div className="concept-body">
        {showArt
          ? <div className="cover-wrap"><CoverArt concept={concept} /><button className="cover-toggle" onClick={toggleArt} title="Hide cover art (more room for text)"><EyeOff size={14} /></button></div>
          : <button className="link small cover-show" onClick={toggleArt}><Eye size={14} /> Show cover art</button>}

        <div className="chips-row">
          {concept.chips.map((c) => <span key={c.kind + c.label} className={`tag tag-${c.kind}`}>{c.label}</span>)}
          <span className="tag tag-scope">{concept.scopeName}</span>
        </div>

        <div className="mode-row">
          <Segmented ariaLabel="Output mode" value={outputMode} onChange={setOutputMode}
            options={[{ value: 'oneliner', label: 'One-liner' }, { value: 'pitch', label: 'Short pitch' }, { value: 'expanded', label: 'Expanded' }]} />
          <button className="link small" onClick={a.reword} title="Same ingredients, different sentences (W)"><Wand2 size={14} /> Reword</button>
        </div>

        {outputMode !== 'oneliner' && <p className={`pitch ${changes.pitch ? 'flash-text' : ''}`} key={'p' + concept.pitch}><Phrases parts={concept.pitchParts} fallback={concept.pitch} /></p>}
        {outputMode === 'expanded' && (
          <dl className="expanded">
            {concept.expanded.map((s) => (
              <div key={s.key} className="expanded-row">
                <dt>{s.heading}</dt>
                <dd>{s.body.split('\n').map((line, i) => <span key={i} className="line">{line}</span>)}</dd>
              </div>
            ))}
          </dl>
        )}

        {outputMode !== 'oneliner' && concept.badIdea && (
          <div className="bad-idea">
            <h4><FlaskConical size={17} /> Why it's a bad idea (and why it might work):</h4>
            <p>{concept.badIdea} {concept.mightWork}</p>
          </div>
        )}

        {concept.recipe.featureCreep && <CreepSection concept={concept} />}

        {concept.warnings.length > 0 && (
          <div className="warnings">{concept.warnings.map((w) => <p key={w}><AlertTriangle size={14} /> {w}</p>)}</div>
        )}
      </div>

      <div className="action-bar">
        <button className="btn primary big" onClick={a.generateTrouble} title="New seed, keeps locks (G or Space)"><Zap size={20} /> Generate Trouble</button>
        <button className="btn" onClick={a.rerollUnlocked} title="Same seed, reroll every unlocked field (R)"><RefreshCw size={17} /><span className="btn-label">Reroll</span></button>
        <button className="btn" onClick={() => void a.copyMarkdown()} title="Copy as Markdown (C)"><Copy size={17} /><span className="btn-label">Copy MD</span></button>
        <div className="export-wrap">
          <button className="btn icon-only" title="Export / share" onClick={() => setExportOpen((o) => !o)}><Download size={17} /></button>
          <Popover open={exportOpen} onClose={() => setExportOpen(false)} className="menu-pop">
            <button onClick={() => { setExportOpen(false); void a.exportAs('markdown') }}>Export Markdown (.md)</button>
            <button onClick={() => { setExportOpen(false); void a.exportAs('text') }}>Export plain text (.txt)</button>
            <button onClick={() => { setExportOpen(false); void a.exportAs('json') }}>Export JSON (.json)</button>
            <hr />
            <button onClick={() => { setExportOpen(false); void a.shareCode() }}><Share2 size={14} /> Copy recipe code</button>
          </Popover>
        </div>
        {isSaved ? (
          <button className="btn saved" disabled><BookmarkCheck size={17} /> Saved</button>
        ) : linkedEntryId ? (
          <>
            <button className="btn" onClick={() => void a.updateSaved()} title="Overwrite the saved idea with this version"><Bookmark size={17} /> Update<span className="btn-label-long">saved</span></button>
            <button className="btn" onClick={() => void a.save()} title="Save as a new idea (S)">New<span className="btn-label-long">copy</span></button>
          </>
        ) : (
          <button className="btn" onClick={() => void a.save()} title="Save to Vault (S)"><Bookmark size={17} /> Save<span className="btn-label-long">to Vault</span></button>
        )}
      </div>
    </main>
  )
}

function TitleLine({ concept }: { concept: Concept }) {
  const { a, noorMode } = useStudio()
  const [editing, setEditing] = useState(false)
  const [text, setText] = useState(concept.title)
  const input = useRef<HTMLInputElement>(null)
  useEffect(() => { if (editing) input.current?.select() }, [editing])
  const locked = !!concept.recipe.titleOverride
  const size = concept.title.length > 26 ? 'long' : concept.title.length > 16 ? 'mid' : ''

  if (editing) {
    return (
      <form className="title-edit" onSubmit={(e) => { e.preventDefault(); setEditing(false); a.renameTitle(text) }}>
        <input ref={input} value={text} onChange={(e) => setText(e.target.value)} onBlur={() => setEditing(false)} aria-label="Title" maxLength={80} />
      </form>
    )
  }
  return (
    <div className="title-line">
      <h1 className={`concept-title slam ${size}`} onDoubleClick={() => setEditing(true)} title="Double-click to rename">{concept.title}</h1>
      {noorMode && <span className="title-heart" title="Noor approves">💜</span>}
      <div className="title-tools">
        <button className="icon-btn sm" title="Rename" onClick={() => setEditing(true)}><Pencil size={14} /></button>
        <button className={`icon-btn sm ${locked ? 'active' : ''}`} title={locked ? 'Unlock title' : 'Lock title'} onClick={a.toggleTitleLock}>{locked ? <Lock size={14} /> : <LockOpen size={14} />}</button>
        <button className="icon-btn sm" title="New title (T)" onClick={a.rerollTitle}><RefreshCw size={14} /></button>
      </div>
    </div>
  )
}

function CreepSection({ concept }: { concept: Concept }) {
  const { a, noorMode, changes } = useStudio()
  const meter = concept.creepMeter
  const visible = concept.creep
  return (
    <section className="creep-section" aria-label="Feature Creep additions">
      <header>
        <h4>{noorMode ? '💜' : '😈'} Feature Creep <span className="muted small">{noorMode ? 'curated by Noor' : 'kept separate from the core pitch'}</span></h4>
        {meter && (
          <div className="creep-meter" title={`Creep meter: ${meter.score}%`}>
            <div className="meter-bar"><span style={{ width: `${Math.min(100, meter.score)}%` }} className={meter.score >= 80 ? 'danger' : meter.score >= 50 ? 'warn' : ''} /></div>
            <span className="meter-label">{meter.score >= 100 ? '∞' : `${meter.score}%`} · {meter.label}</span>
          </div>
        )}
      </header>
      {noorMode && <NoorBubble codename={concept.codename} />}
      {meter?.warning && <p className="scope-warning"><AlertTriangle size={14} /> {meter.warning}</p>}
      <ul className="creep-list">
        {visible.map((c) => <CreepItem key={c.slot + c.itemId} c={c} fresh={changes.creep.has(c.slot)} onAction={(act) => a.creep(c.slot, act)} />)}
        {visible.length === 0 && <li className="muted small">All proposals rejected. Your producer weeps with joy.</li>}
      </ul>
      <button className="btn ghost small" onClick={a.anotherThing}><Wand2 size={15} /> And another thing...</button>
    </section>
  )
}

function CreepItem({ c, fresh, onAction }: { c: CreepProposal; fresh: boolean; onAction: (a: 'accept' | 'reject' | 'reroll' | 'undo') => void }) {
  const accepted = c.status === 'accepted'
  return (
    <li className={`creep-item ${accepted ? 'accepted' : ''} ${fresh ? 'flash-box' : ''}`}>
      <div className="creep-item-head">
        <span className={`kind kind-${c.kind}`}>{c.kindLabel}</span>
        <strong>{c.name}</strong>
        <span className="complexity" title={`Complexity ${c.complexity}/10`}>{'●'.repeat(Math.ceil(c.complexity / 2))}<span className="dim">{'●'.repeat(5 - Math.ceil(c.complexity / 2))}</span></span>
        <div className="creep-actions">
          {accepted
            ? <button className="mini-btn" title="Undo accept" onClick={() => onAction('undo')}><Undo2 size={13} /> Kept</button>
            : <button className="mini-btn good" title="Accept this feature" onClick={() => onAction('accept')}><Check size={13} /> Keep</button>}
          <button className="icon-btn sm" title="Suggest something else" onClick={() => onAction('reroll')}><RefreshCw size={14} /></button>
          <button className="mini-btn bad" title="Reject this feature" onClick={() => onAction('reject')}><X size={13} /> Nope</button>
        </div>
      </div>
      <p>{c.pitch}{c.connects && <span className="connects"> {c.connects}</span>}</p>
    </li>
  )
}
