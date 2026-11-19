import { api } from '../api'
import { useStudio } from '../store'
import { Segmented, Toggle } from '../components/ui'
import { Logo } from '../components/Logo'

export function SettingsPage() {
  const { boot, setTheme, outputMode, setOutputMode, refreshBoot, noorMode, setNoorMode } = useStudio()
  const setReduceMotion = async (v: boolean) => { const settings = await api.updateSettings({ reduceMotion: v }); await refreshBoot({ ...boot, settings }) }
  return (
    <div className="page settings-page">
      <section className="panel">
        <h2>Colour scheme</h2>
        <p className="muted small">Themes are JSON files. Drop your own into <code>{boot.dataDir}/themes</code> and reload.</p>
        <div className="theme-grid">
          {boot.themes.map((t) => (
            <button key={t.id} className={`theme-card ${boot.settings.theme === t.id ? 'active' : ''}`} onClick={() => setTheme(t.id)} aria-pressed={boot.settings.theme === t.id}
              style={{ background: t.colors.surface, color: t.colors.text, borderColor: boot.settings.theme === t.id ? t.colors.accentStrong : t.colors.border }}>
              <div className="theme-preview" style={{ background: `radial-gradient(circle at 70% 20%, ${t.colors.bgGlow}, ${t.colors.bg})` }}>
                <span style={{ background: t.colors.accent }} />
                <span style={{ background: t.colors.accentStrong }} />
                <span style={{ background: t.colors.accent2 }} />
                <span style={{ background: t.colors.textMuted }} />
              </div>
              <strong>{t.name}</strong>
              <span style={{ color: t.colors.textMuted }} className="small">{t.description}</span>
            </button>
          ))}
        </div>
        {boot.themeErrors.length > 0 && <div className="pack-errors">Some themes could not be loaded:<ul>{boot.themeErrors.map((e) => <li key={e}>{e}</li>)}</ul></div>}
      </section>
      <section className="panel settings-list">
        <h2>Preferences</h2>
        <div className="setting-row"><div><strong>Default output</strong><p className="muted small">How much text a concept shows by default.</p></div>
          <Segmented ariaLabel="Default output" value={outputMode} onChange={setOutputMode}
            options={[{ value: 'oneliner', label: 'One-liner' }, { value: 'pitch', label: 'Short pitch' }, { value: 'expanded', label: 'Expanded' }]} /></div>
        <div className="setting-row noor-row"><div><strong>💜 Noor Mode</strong><p className="muted small">Feature Creep becomes permanent, every idea gets one extra proposal, a purple heart and Noor's commentary. Sage was not consulted. (Psst: type <kbd>n</kbd><kbd>o</kbd><kbd>o</kbd><kbd>r</kbd> on the Generator.)</p></div>
          <Toggle on={noorMode} onChange={() => setNoorMode(!noorMode)} label="Noor Mode" /></div>
        <div className="setting-row"><div><strong>Reduce motion</strong><p className="muted small">Turns off the title slam and other wobbles.</p></div>
          <Toggle on={boot.settings.reduceMotion} onChange={() => void setReduceMotion(!boot.settings.reduceMotion)} label="Reduce motion" /></div>
        <div className="setting-row"><div><strong>Data folder</strong><p className="muted small">Vault, settings, imported packs and themes live here.</p></div><code className="path">{boot.dataDir}</code></div>
        <div className="about">
          <Logo size={56} />
          <div>
            <strong>BadGameGen v{boot.appVersion}</strong> · generator v{boot.generatorVersion}
            <p className="muted small">Made at Bad Ideas Works by Tom, Sage and Noor. Runs fully offline. No LLM required, no ideas harmed.<br />
              <em>"Code shall remain understandable to whichever AI inherits this mess."</em></p>
          </div>
        </div>
      </section>
    </div>
  )
}
