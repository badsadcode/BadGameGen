import { BarChart3, Boxes, FolderArchive, Keyboard, Settings, Zap } from 'lucide-react'
import { useStudio, type Page } from '../store'
import { Logo } from './Logo'

const NAV: { page: Page; label: string; icon: typeof Zap }[] = [
  { page: 'generator', label: 'Generator', icon: Zap },
  { page: 'vault', label: 'Vault', icon: FolderArchive },
  { page: 'packs', label: 'Packs', icon: Boxes },
  { page: 'stats', label: 'Stats', icon: BarChart3 },
  { page: 'settings', label: 'Settings', icon: Settings },
]

export function TopBar({ onHelp }: { onHelp: () => void }) {
  const { page, setPage } = useStudio()
  return (
    <header className="topbar">
      <div className="brand">
        <Logo size={44} />
        <span className="brand-name">BadGameGen<span className="sparkle">✦</span></span>
      </div>
      <nav className="nav" aria-label="Main">
        {NAV.map(({ page: p, label, icon: Icon }) => (
          <button key={p} className={page === p ? 'active' : ''} aria-current={page === p ? 'page' : undefined} onClick={() => setPage(p)}>
            <Icon size={17} /> {label}
          </button>
        ))}
      </nav>
      <div className="tagline">Terrible ideas. <em>Surprisingly playable games.</em></div>
      <button className="icon-btn" title="Keyboard shortcuts (?)" onClick={onHelp}><Keyboard size={18} /></button>
    </header>
  )
}
