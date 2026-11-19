import { useEffect, useState } from 'react'
import { api } from '../api'
import type { StatsDto } from '../types'
import { useStudio } from '../store'

export function StatsPage() {
  const { boot } = useStudio()
  const [stats, setStats] = useState<StatsDto | null>(null)
  useEffect(() => { void api.stats().then(setStats) }, [])
  if (!stats) return <div className="page"><div className="spinner" /></div>
  const maxCat = Math.max(1, ...Object.values(stats.itemsPerCategory))
  const label = (key: string) => boot.categories.find((c) => c.key === key)?.displayName ?? key
  return (
    <div className="page stats-page">
      <div className="stat-tiles">
        <Tile value={stats.totalGenerated} label="ideas generated" note="each one a small crime against production schedules" />
        <Tile value={stats.vaultCount} label="ideas in the Vault" note={`${stats.favoriteCount} favorites`} />
        <Tile value={stats.libraryItems} label="ingredients enabled" note={`${boot.packs.filter((p) => p.enabled).length} packs`} />
        <Tile value={stats.creepAccepted} label="feature creep accepted" note="you were warned" />
        <Tile value={`${Math.round(stats.averageChaos * 100)}%`} label="average chaos (saved)" note={stats.averageChaos > 0.6 ? 'concerning' : 'suspiciously reasonable'} />
      </div>
      <div className="stats-grid">
        <section className="panel">
          <h3>Library by category</h3>
          <ul className="bars">
            {Object.entries(stats.itemsPerCategory).map(([k, n]) => (
              <li key={k}><span className="bar-label">{label(k)}</span><span className="bar"><span style={{ width: `${(n / maxCat) * 100}%` }} /></span><span className="bar-value">{n}</span></li>
            ))}
          </ul>
        </section>
        <section className="panel">
          <h3>Most-saved genres</h3>
          <TopList items={stats.topGenres} empty="Save some ideas to see your genre habits." />
          <h3>Most-saved settings</h3>
          <TopList items={stats.topSettings} empty="No settings yet. Everywhere is possible." />
        </section>
      </div>
    </div>
  )
}

function Tile({ value, label, note }: { value: number | string; label: string; note: string }) {
  return <div className="panel stat-tile"><strong>{value}</strong><span>{label}</span><em className="muted small">{note}</em></div>
}

function TopList({ items, empty }: { items: { key: string; value: number }[]; empty: string }) {
  if (items.length === 0) return <p className="muted small">{empty}</p>
  const max = Math.max(...items.map((i) => i.value))
  return (
    <ul className="bars">
      {items.map((i) => <li key={i.key}><span className="bar-label">{i.key}</span><span className="bar"><span style={{ width: `${(i.value / max) * 100}%` }} /></span><span className="bar-value">{i.value}</span></li>)}
    </ul>
  )
}
