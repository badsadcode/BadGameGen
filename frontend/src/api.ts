import type {
  AppSettings, BootstrapInfo, Concept, ExportResult, ImportResult, OptionDto, PackContents,
  PackInfo, Recipe, StatsDto, VaultEntry,
} from './types'

/**
 * Transport to the C# host. Inside the desktop app requests go through WebView2 messages;
 * in a normal browser (dev server) they go over HTTP POST /api/rpc. Same JSON either way.
 */
interface WebViewBridge {
  postMessage(message: string): void
  addEventListener(type: 'message', listener: (e: { data: unknown }) => void): void
}
const webview: WebViewBridge | undefined = (window as unknown as { chrome?: { webview?: WebViewBridge } }).chrome?.webview

type Pending = { resolve: (v: unknown) => void; reject: (e: Error) => void }
const pending = new Map<string, Pending>()
let nextId = 1

if (webview) {
  webview.addEventListener('message', (e) => {
    const msg = JSON.parse(String(e.data)) as { id?: string; ok: boolean; result?: unknown; error?: string }
    const p = msg.id ? pending.get(String(msg.id)) : undefined
    if (!p) return
    pending.delete(String(msg.id))
    if (msg.ok) p.resolve(msg.result)
    else p.reject(new Error(msg.error ?? 'Unknown error'))
  })
}

async function call<T>(method: string, params?: unknown): Promise<T> {
  const id = String(nextId++)
  const body = JSON.stringify({ id, method, params })
  if (webview) {
    return new Promise<T>((resolve, reject) => {
      pending.set(id, { resolve: resolve as (v: unknown) => void, reject })
      webview.postMessage(body)
    })
  }
  const res = await fetch('/api/rpc', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body })
  if (!res.ok) throw new Error(`Host error ${res.status}`)
  const msg = (await res.json()) as { ok: boolean; result?: T; error?: string }
  if (!msg.ok) throw new Error(msg.error ?? 'Unknown error')
  return msg.result as T
}

export const api = {
  bootstrap: () => call<BootstrapInfo>('app.bootstrap'),
  reload: () => call<BootstrapInfo>('app.reload'),
  updateSettings: (patch: Partial<Pick<AppSettings, 'theme' | 'outputMode' | 'reduceMotion' | 'noorMode'>>) => call<AppSettings>('settings.update', patch),

  generate: (recipe: Recipe) => call<Concept>('concept.generate', { recipe }),
  options: (category: string, query?: string) => call<OptionDto[]>('category.options', { category, query }),
  encodeRecipe: (recipe: Recipe) => call<{ code: string }>('recipe.encode', { recipe }),
  decodeRecipe: (code: string) => call<{ recipe: Recipe; packs: string[] }>('recipe.decode', { code }),

  vaultList: () => call<VaultEntry[]>('vault.list'),
  vaultSave: (concept: Concept, notes?: string, tags?: string[]) => call<VaultEntry>('vault.save', { concept, notes, tags }),
  vaultUpdate: (id: string, patch: { title?: string; notes?: string; tags?: string[]; favorite?: boolean; concept?: Concept }) =>
    call<VaultEntry>('vault.update', { id, ...patch }),
  vaultDuplicate: (id: string) => call<VaultEntry>('vault.duplicate', { id }),
  vaultDelete: (id: string) => call<{ deleted: boolean }>('vault.delete', { id }),

  exportConcept: (concept: Concept, format: 'markdown' | 'text' | 'json') => call<ExportResult>('export.render', { concept, format }),
  exportEntry: (entryId: string, format: 'markdown' | 'text' | 'json') => call<ExportResult>('export.render', { entryId, format }),

  packs: () => call<PackInfo[]>('packs.list'),
  packContents: (id: string) => call<PackContents>('packs.contents', { id }),
  setPackEnabled: (id: string, enabled: boolean) => call<PackInfo[]>('packs.setEnabled', { id, enabled }),
  importPack: (fileName: string, content: string) => call<ImportResult>('packs.import', { fileName, content }),
  deletePack: (id: string) => call<{ deleted: boolean }>('packs.delete', { id }),

  stats: () => call<StatsDto>('stats.get'),
}
