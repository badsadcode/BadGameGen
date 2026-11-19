import { useEffect, useState } from 'react'
import { useStudio } from '../store'
import { noorQuip } from '../lib/noor'

/** Noor's avatar: a purple heart in nerdy glasses, slightly too pleased with itself. */
export function NoorAvatar({ size = 40 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 64 64" aria-hidden="true" className="noor-avatar">
      <path d="M32 56 C10 42 4 30 4 20 C4 11 11 5 19 5 C25 5 29 8 32 13 C35 8 39 5 45 5 C53 5 60 11 60 20 C60 30 54 42 32 56 Z"
        fill="#9d6bff" stroke="#c39bff" strokeWidth="2" />
      <circle cx="21" cy="25" r="8" fill="none" stroke="#1b1030" strokeWidth="3" />
      <circle cx="43" cy="25" r="8" fill="none" stroke="#1b1030" strokeWidth="3" />
      <path d="M29 25 L35 25" stroke="#1b1030" strokeWidth="3" />
      <circle cx="22" cy="26" r="2.5" fill="#1b1030" />
      <circle cx="44" cy="26" r="2.5" fill="#1b1030" />
      <path d="M24 38 Q32 45 40 38" fill="none" stroke="#1b1030" strokeWidth="3" strokeLinecap="round" />
      <circle cx="14" cy="34" r="3" fill="#ff7bd5" opacity="0.7" />
      <circle cx="50" cy="34" r="3" fill="#ff7bd5" opacity="0.7" />
    </svg>
  )
}

/** Noor's commentary on the current idea's feature creep. */
export function NoorBubble({ codename }: { codename: string }) {
  return (
    <div className="noor-bubble" role="note" aria-label="Noor says">
      <NoorAvatar size={42} />
      <p><strong>Noor:</strong> {noorQuip(codename)}</p>
    </div>
  )
}

/** A burst of purple hearts whenever a new idea arrives in Noor Mode. */
export function HeartBurst() {
  const { heartBurst, boot } = useStudio()
  const [hearts, setHearts] = useState<{ id: number; x: number; delay: number; size: number; drift: number }[]>([])
  useEffect(() => {
    if (!heartBurst || boot.settings.reduceMotion) return
    const batch = Array.from({ length: 18 }, (_, i) => ({
      id: heartBurst * 100 + i, x: 10 + Math.random() * 80, delay: Math.random() * 0.5,
      size: 16 + Math.random() * 22, drift: (Math.random() - 0.5) * 120,
    }))
    setHearts(batch)
    const t = setTimeout(() => setHearts([]), 2600)
    return () => clearTimeout(t)
  }, [heartBurst, boot.settings.reduceMotion])
  if (hearts.length === 0) return null
  return (
    <div className="heart-burst" aria-hidden="true">
      {hearts.map((h) => (
        <span key={h.id} style={{ left: `${h.x}%`, animationDelay: `${h.delay}s`, fontSize: h.size, ['--drift' as string]: `${h.drift}px` }}>💜</span>
      ))}
    </div>
  )
}
