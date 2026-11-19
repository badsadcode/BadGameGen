import { StrictMode, useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import '@fontsource-variable/rubik'
import '@fontsource/permanent-marker'
import './styles.css'
import { api } from './api'
import { App } from './App'
import { StudioProvider } from './store'
import type { BootstrapInfo } from './types'
import { applyTheme } from './lib/util'

function Root() {
  const [boot, setBoot] = useState<BootstrapInfo | null>(null)
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    api.bootstrap().then((b) => {
      applyTheme(b.themes.find((t) => t.id === b.settings.theme) ?? b.themes[0])
      setBoot(b)
    }).catch((e: Error) => setError(e.message))
  }, [])
  if (error) return <div className="fatal"><h2>BadGameGen couldn't reach its engine.</h2><p>{error}</p><p className="muted">In development, start the dev server: <code>dotnet run --project src/BadGameGen.DevServer</code></p></div>
  if (!boot) return <div className="fatal"><div className="spinner" /></div>
  return <StudioProvider initial={boot}><App /></StudioProvider>
}

createRoot(document.getElementById('root')!).render(<StrictMode><Root /></StrictMode>)
