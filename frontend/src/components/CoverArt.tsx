import { useMemo } from 'react'
import type { Concept } from '../types'
import { mulberry } from '../lib/util'

/**
 * Procedural "box art" for a concept: a seeded scene in the current theme's colours.
 * Not an image generator, just shapes. Elements react to the concept's tags so
 * water concepts get waves, space concepts get stars, horror gets an ominous moon, etc.
 */
export function CoverArt({ concept }: { concept: Concept }) {
  const tags = useMemo(() => new Set(concept.chips.map((c) => c.label.toLowerCase()).concat(concept.picks.map((p) => p.label.toLowerCase()))), [concept])
  const art = useMemo(() => {
    const rnd = mulberry(concept.codename + concept.title)
    const has = (...words: string[]) => [...tags].some((t) => words.some((w) => t.includes(w)))
    const stars = Array.from({ length: has('space', 'star', 'orbit', 'moon', 'alien', 'night') ? 70 : 28 }, () => ({
      x: rnd() * 640, y: rnd() * 200, r: rnd() * 1.6 + 0.3, o: rnd() * 0.7 + 0.2,
    }))
    const skyline = Array.from({ length: 14 }, (_, i) => ({
      x: i * 48 + rnd() * 10, w: 30 + rnd() * 26, h: 40 + rnd() * 110, windows: Math.floor(rnd() * 6),
    }))
    const hills = Array.from({ length: 3 }, (_, i) => ({ amp: 18 + rnd() * 30, phase: rnd() * 6, base: 230 + i * 26 }))
    return {
      stars, skyline, hills,
      sunX: 120 + rnd() * 400, sunY: 70 + rnd() * 60, sunR: 34 + rnd() * 30,
      water: has('water', 'sea', 'flood', 'ocean', 'lake', 'harbour', 'harbor', 'baltic', 'ship', 'fishing', 'island', 'boat'),
      city: has('town', 'city', 'office', 'urban', 'street', 'apartment', 'mall', 'station', 'street'),
      eerie: has('horror', 'haunt', 'ghost', 'dark', 'grim', 'cursed', 'eerie', 'creepy'),
      tilt: (rnd() - 0.5) * 8,
    }
  }, [concept, tags])

  const wave = (y: number, amp: number, phase: number) => {
    let d = `M0 ${y}`
    for (let x = 0; x <= 640; x += 20) d += ` L${x} ${y + Math.sin(x / 60 + phase) * amp}`
    return d + ' L640 360 L0 360 Z'
  }

  return (
    <svg className="cover-art" viewBox="0 0 640 360" preserveAspectRatio="xMidYMid slice" role="img" aria-label={`Procedural cover art for ${concept.title}`}>
      <defs>
        <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="var(--c-bg)" />
          <stop offset="0.65" stopColor="var(--c-bgGlow)" />
          <stop offset="1" stopColor="var(--c-accent)" stopOpacity="0.55" />
        </linearGradient>
        <radialGradient id="sun">
          <stop offset="0" stopColor="var(--c-accentStrong)" />
          <stop offset="0.6" stopColor="var(--c-accent)" stopOpacity="0.8" />
          <stop offset="1" stopColor="var(--c-accent)" stopOpacity="0" />
        </radialGradient>
      </defs>
      <rect width="640" height="360" fill="url(#sky)" />
      {art.stars.map((s, i) => <circle key={i} cx={s.x} cy={s.y} r={s.r} fill="var(--c-text)" opacity={s.o} />)}
      <circle cx={art.sunX} cy={art.sunY} r={art.sunR * 2.2} fill="url(#sun)" opacity="0.6" />
      <circle cx={art.sunX} cy={art.sunY} r={art.sunR} fill={art.eerie ? 'var(--c-text)' : 'var(--c-accentStrong)'} opacity={art.eerie ? 0.85 : 0.95} />
      {art.eerie && <circle cx={art.sunX + art.sunR * 0.35} cy={art.sunY - art.sunR * 0.2} r={art.sunR * 0.9} fill="var(--c-bgGlow)" />}
      <g transform={`rotate(${art.tilt} 320 200)`}>
        {!art.city && art.hills.map((h, i) => (
          <path key={i} d={wave(h.base, h.amp, h.phase)} fill="var(--c-surface)" opacity={0.55 + i * 0.2} />
        ))}
        {art.city && art.skyline.map((b, i) => (
          <g key={i}>
            <rect x={b.x} y={270 - b.h} width={b.w} height={b.h + 90} fill="var(--c-surface)" stroke="var(--c-border)" />
            {Array.from({ length: b.windows }, (_, j) => (
              <rect key={j} x={b.x + 6 + (j % 2) * (b.w / 2 - 4)} y={280 - b.h + Math.floor(j / 2) * 18} width="7" height="9" fill="var(--c-warning)" opacity="0.8" />
            ))}
          </g>
        ))}
      </g>
      {art.water && (
        <g>
          <path d={wave(285, 6, 0)} fill="var(--c-accent2)" opacity="0.35" />
          <path d={wave(300, 5, 2)} fill="var(--c-bg)" opacity="0.75" />
          {[0, 1, 2, 3, 4].map((i) => (
            <rect key={i} x={art.sunX - 30 + i * 6} y={300 + i * 10} width={60 - i * 12} height="2" fill="var(--c-accentStrong)" opacity={0.5 - i * 0.08} />
          ))}
        </g>
      )}
      <rect width="640" height="360" fill="none" stroke="var(--c-border)" />
    </svg>
  )
}
