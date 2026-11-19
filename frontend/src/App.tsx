import { useEffect, useRef, useState } from 'react'
import { useStudio } from './store'
import { TopBar } from './components/TopBar'
import { StatusBar } from './components/StatusBar'
import { GeneratorPanel } from './components/GeneratorPanel'
import { ConceptView } from './components/ConceptView'
import { SidePanel } from './components/SidePanel'
import { HelpOverlay } from './components/HelpOverlay'
import { Toasts } from './components/ui'
import { HeartBurst } from './components/Noor'
import { VaultPage } from './pages/VaultPage'
import { PacksPage } from './pages/PacksPage'
import { StatsPage } from './pages/StatsPage'
import { SettingsPage } from './pages/SettingsPage'

export function App() {
  const { page, a, setOutputMode, noorMode, setNoorMode } = useStudio()
  const [help, setHelp] = useState(false)
  const typed = useRef('')

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement
      if (target.closest('input, textarea, select, [contenteditable=true]') || e.ctrlKey || e.metaKey) return
      if (e.altKey) {
        if (e.key === 'ArrowLeft') { e.preventDefault(); a.back() }
        if (e.key === 'ArrowRight') { e.preventDefault(); a.forward() }
        return
      }
      if (e.key === '?') { setHelp((h) => !h); return }
      if (page !== 'generator') return
      // Secret: typing n-o-o-r toggles Noor Mode (the final "r" doesn't reroll).
      typed.current = (typed.current + e.key.toLowerCase()).slice(-4)
      if (typed.current === 'noor') { typed.current = ''; e.preventDefault(); setNoorMode(!noorMode); return }
      // Space only when focus isn't on a button (where Space means "click").
      if (e.key === ' ' && target.closest('button')) return
      const map: Record<string, () => void> = {
        g: a.generateTrouble, ' ': a.generateTrouble, r: a.rerollUnlocked, w: a.reword, t: a.rerollTitle,
        f: a.toggleCreep, a: a.anotherThing, s: () => void a.save(), c: () => void a.copyMarkdown(),
        '1': () => setOutputMode('oneliner'), '2': () => setOutputMode('pitch'), '3': () => setOutputMode('expanded'),
      }
      const fn = map[e.key.toLowerCase()]
      if (fn) { e.preventDefault(); fn() }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [a, page, setOutputMode, noorMode, setNoorMode])

  return (
    <div className={`app ${noorMode ? 'noor-mode' : ''}`}>
      <TopBar onHelp={() => setHelp(true)} />
      <div className="workspace">
        {page === 'generator' && (
          <div className="generator-layout">
            <GeneratorPanel />
            <ConceptView />
            <SidePanel />
          </div>
        )}
        {page === 'vault' && <VaultPage />}
        {page === 'packs' && <PacksPage />}
        {page === 'stats' && <StatsPage />}
        {page === 'settings' && <SettingsPage />}
      </div>
      <StatusBar />
      <Toasts />
      <HeartBurst />
      {help && <HelpOverlay onClose={() => setHelp(false)} />}
    </div>
  )
}
