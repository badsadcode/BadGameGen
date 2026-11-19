// TypeScript mirrors of the C# DTOs (src/BadGameGen.Core + src/BadGameGen.App).
// Keep in sync with ApiDispatcher.cs when adding fields.

export type CreepStatus = 'proposed' | 'accepted' | 'rejected'
export type CreepKind = 'system' | 'loop' | 'content' | 'whyNot'
export type OutputMode = 'oneliner' | 'pitch' | 'expanded'

export interface CreepSlot { nonce: number; status: CreepStatus }

export interface Recipe {
  seed: string
  chaos: number
  scope: number
  silliness: number
  pins: Record<string, string>
  keep: Record<string, string>
  rerolls: Record<string, number>
  exclusions: string[]
  titleOverride?: string | null
  featureCreep: boolean
  creepKinds: CreepKind[]
  creep: CreepSlot[]
}

export interface ConceptPick {
  slot: string; field: string; category: string; itemId: string; label: string; packId: string; pinned: boolean
}
export interface ConceptChip { label: string; kind: string }
export interface ConceptSection { key: string; heading: string; body: string }
export interface CreepProposal {
  slot: number; itemId: string; kind: CreepKind; kindLabel: string; name: string; pitch: string
  connects?: string | null; complexity: number; status: CreepStatus
}
export interface CreepMeter { score: number; label: string; warning?: string | null }
export interface PackRef { id: string; version: string }

export interface Concept {
  generatorVersion: string
  codename: string
  title: string
  hook: string
  pitch: string
  expanded: ConceptSection[]
  badIdea: string
  mightWork: string
  picks: ConceptPick[]
  chips: ConceptChip[]
  creep: CreepProposal[]
  creepMeter?: CreepMeter | null
  scopeName: string
  recipe: Recipe
  library: PackRef[]
  warnings: string[]
}

export interface VaultEntry {
  id: string; title: string; concept: Concept; tags: string[]; notes: string; favorite: boolean
  createdAt: string; updatedAt: string
}

export interface CategoryDto { key: string; displayName: string; icon: string; description: string }
export interface OptionDto { id: string; label: string; packId: string; tags: string[]; weirdness: number; silliness: number }

export interface PackInfo {
  id: string; name: string; version: string; author: string; description: string; fileName: string
  isBuiltIn: boolean; enabled: boolean; usable: boolean; errors: string[]; warnings: string[]
  counts: Record<string, number>; templateCount: number
}

export interface ThemeDef { id: string; name: string; description: string; dark: boolean; colors: Record<string, string>; isBuiltIn: boolean }

export interface AppSettings {
  theme: string; disabledPacks: string[]; outputMode: OutputMode; lastRecipe?: Recipe | null
  totalGenerated: number; reduceMotion: boolean; noorMode: boolean; themeBeforeNoor?: string | null
}

export interface BootstrapInfo {
  appVersion: string; generatorVersion: string; categories: CategoryDto[]; scopeNames: string[]
  packs: PackInfo[]; librarySignature: PackRef[]; themes: ThemeDef[]; themeErrors: string[]
  settings: AppSettings; dataDir: string; vaultNotice?: string | null
}

export interface StatsDto {
  totalGenerated: number; vaultCount: number; favoriteCount: number; libraryItems: number
  itemsPerCategory: Record<string, number>; topGenres: { key: string; value: number }[]
  topSettings: { key: string; value: number }[]; averageChaos: number; creepAccepted: number
}

export interface ExportResult { fileName: string; mime: string; content: string }
export interface ImportResult { ok: boolean; packId?: string | null; errors: string[]; warnings: string[] }

export interface PackContents {
  id: string; name: string; version: string; author: string; description: string
  items: Record<string, { id: string; label: string; forms: Record<string, string>; tags: string[]; weirdness: number; silliness: number; kind?: string }[]>
  templates: Record<string, { id: string; text: string }[]>
}
