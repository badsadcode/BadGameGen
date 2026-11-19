/** The BadSad cat: unimpressed, sunglasses on, judging your scope. */
export function Logo({ size = 44, className }: { size?: number; className?: string }) {
  return (
    <svg className={className} width={size} height={size} viewBox="0 0 64 64" aria-hidden="true">
      <path d="M10 26 L8 6 L24 16 Q32 13 40 16 L56 6 L54 26 Q58 34 55 42 Q50 56 32 57 Q14 56 9 42 Q6 34 10 26 Z"
        fill="var(--c-text)" stroke="var(--c-bg)" strokeWidth="2.5" strokeLinejoin="round" />
      <path d="M12 10 L22 17 L13 22 Z M52 10 L42 17 L51 22 Z" fill="var(--c-accent)" opacity="0.85" />
      <path d="M11 30 Q20 26 30 30 L30 34 Q29 41 21 41 Q13 41 12 35 Z" fill="var(--c-bg)" />
      <path d="M53 30 Q44 26 34 30 L34 34 Q35 41 43 41 Q51 41 52 35 Z" fill="var(--c-bg)" />
      <path d="M29 31 L35 31" stroke="var(--c-bg)" strokeWidth="2.5" />
      <path d="M15 32 L19 31" stroke="var(--c-accentStrong)" strokeWidth="2" strokeLinecap="round" />
      <path d="M38 32 L42 31" stroke="var(--c-accentStrong)" strokeWidth="2" strokeLinecap="round" />
      <path d="M29 45 L32 47 L35 45" fill="none" stroke="var(--c-bg)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M26 50 Q32 48 38 50" fill="none" stroke="var(--c-bg)" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
