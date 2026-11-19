import { Boxes, Dices, Layers } from 'lucide-react'
import { useStudio } from '../store'
import { Logo } from './Logo'

export function StatusBar() {
  const { concept, boot, outputMode } = useStudio()
  const active = boot.packs.filter((p) => p.enabled)
  const creep = concept?.recipe.featureCreep
  const modeLabel = { oneliner: 'One-liner', pitch: 'Short pitch', expanded: 'Expanded' }[outputMode]
  return (
    <footer className="statusbar">
      <span><Dices size={15} /> Seed: <strong>{concept?.recipe.seed ?? '…'}</strong></span>
      <span className="sep" />
      <span className="packs-status" title={active.map((p) => p.name).join(', ')}><Boxes size={15} /> Active packs: {active.map((p) => p.name).join(' · ') || 'none!'}</span>
      <span className="sep" />
      <span>{modeLabel} · {concept?.scopeName ?? ''}</span>
      <span className="grow" />
      <span className={`creep-status ${creep ? 'on' : ''}`}><Logo size={20} /> Feature Creep: {creep ? 'ON' : 'off'}</span>
      <span className="sep" />
      <span><Layers size={15} /> v{boot.appVersion}</span>
    </footer>
  )
}
