# Architecture

```
content/packs/*.json   content/themes/*.json        ← data, editable without recompiling
        │                       │
┌───────▼───────────────────────▼────────┐
│ BadGameGen.Core   (headless, no UI)     │  generation, packs, templates, vault, export
└───────────────────┬─────────────────────┘
┌───────────────────▼─────────────────────┐
│ BadGameGen.App    (application services) │  settings, themes, pack catalog, ApiDispatcher (JSON RPC)
└──────┬──────────────────┬──────────────┬─┘
       │                  │              │
 BadGameGen.Desktop  BadGameGen.DevServer  BadGameGen.Cli
 WinForms + WebView2  ASP.NET, POST /api/rpc  terminal tool
       │                  │
       └──── frontend/ (React + TypeScript + Vite) ────┘
             src/api.ts picks the transport automatically
```

## Core concepts

**Content item.** One ingredient (a genre, a twist...) with a label, grammatical *forms*, tags,
affinity/excludes tags, weight, weirdness, silliness and an optional scope range. Its full id is
namespaced: `core:setting.flooded-baltic-town`.

**Library.** The merged view of all enabled packs, always sorted by pack id, so the order you
enable packs in never matters.

**Recipe** (`Generation/Recipe.cs`). Everything needed to reproduce a concept:
- `seed`, `chaos`, `scope` and `silliness`.
- `pins`: locked or hand-picked items, slot → item id.
- `keep`: items held over during a single-field reroll, so only that field changes.
- `rerolls`: a nonce per slot, plus `title` and `wording`.
- `exclusions`.
- `titleOverride`.
- `featureCreep`, `creepKinds` and `creep` slots (each with a nonce and an
  accepted/rejected/proposed status).

**Slots.** The fields a concept has at a given scope (`Scopes.Plan`). Tiny jams get no supporting
mechanics, progression or multiplayer. Absurd scope gets four supporting mechanics.

## Randomness and reproducibility

- `SeedRng` is SplitMix64 with FNV-1a hashing. We never use `System.Random` or
  `string.GetHashCode()`, since both can change between runtimes.
- Every random choice uses **race sampling** (`SeedRng.Race`, Efraimidis–Spirakis). Each
  candidate gets its own hashed uniform value `u` from (seed, stream, nonce, item id) and the
  lowest `-ln(u)/weight` wins. Pick probability is exactly proportional to weight, but the
  winner only changes when its own odds change. In practice:
  - Changing one field barely disturbs the others.
  - Enabling a new pack doesn't reshuffle every existing seed.
- Each slot has its own stream (`genre`, `secondary#1`, `creep#2`, `tpl:hook`...). A reroll
  bumps that slot's nonce.
- `Generator.Version` must be bumped if an algorithm change alters output for existing recipes.

## The generator (`Generation/Generator.cs`)

1. Normalise the recipe (clamp values, fill default creep slots).
2. Place pins, then keeps.
3. Fill the remaining slots in a fixed order, weighting each candidate with `Score()`:
   - **Weirdness vs chaos:** grounded concepts favour low weirdness, audacious ones favour high.
   - **Silliness vs the serious↔silly slider.**
   - **Scope range.**
   - **Excludes:** forbidden below chaos 0.7, increasingly allowed above it.
   - **Affinity:** coherent pairs are boosted, and the boost fades as chaos rises.
4. Choose title parts by tag overlap with the concept, then render a title template.
5. Render the hook, pitch sections, bad idea / might work, expanded sections and scope note
   from templates.
   - A template is eligible only if every field it references exists.
   - A themed template (`tagsAny`) is eligible only if its theme appears on 2+ picks.
6. Feature Creep: pick creep items with affinity to the concept's tags, render their
   `pitch`/`connects` templates against the concept, and compute the creep meter.

## Templates (`Generation/TemplateEngine.cs`)

- `{field.form}`, `{secondary*.noun}` (a list) and `{title}`.
- `[a] word` becomes *a* or *an*, with exceptions handled: hour, unicorn, RPG, UFO, 8-bit.
- `Polish()` fixes spacing and punctuation and capitalises sentences. `TitleCase()` handles
  titles.
- Pack templates and creep forms are validated at load time against the known fields and forms.

## The editable library (`App/Library.cs`)

- All packs load from one folder: `<data>/packs`. Packs shipped with the app (embedded in the
  exe, or in `content/packs` in dev) are only the factory defaults that **seed** that folder.
- `.builtin-packs.json` records the hash of each built-in version we installed. On every start:
  - A built-in that's missing and was never installed gets copied in.
  - An unedited built-in (its hash matches the installed one) is updated if the app ships a newer
    version.
  - An edited built-in is left alone and flagged "update available".
  - A built-in the user deleted stays deleted until "Restore".
- Hashes ignore line endings, so re-saving a file in Notepad doesn't count as an edit.
- `library.check` is polled by the frontend every 2.5 s. It compares a cheap fingerprint (names,
  sizes, write times) and reloads when anything changed, so edits show up live.
- Hidden files (names starting with `.`) are never loaded as packs.

## Persistence

- **Vault** (`Vault/VaultStore.cs`) is one JSON file, written atomically (temp file + rename).
  A corrupt file is renamed to `vault.json.corrupt-<timestamp>`, never deleted.
- Entries store the **full rendered concept**, so they stay readable when packs change.
- Settings (`App/Settings.cs`) are stored the same way. Disposable: a corrupt settings file just
  resets to defaults.

## Host ↔ frontend API

`App/ApiDispatcher.cs` handles `{ id, method, params }` and returns `{ id, ok, result | error }`.

- **Desktop:** messages go through `chrome.webview.postMessage` and `PostWebMessageAsString`.
  The frontend is served from `https://app.badgamegen.local`, a virtual host, so it's a secure
  context and the clipboard works.
- **Dev server:** messages go through `POST /api/rpc`.
- `frontend/src/api.ts` chooses the transport automatically.
- `frontend/src/types.ts` mirrors the C# DTOs. Update both when adding fields.

## Frontend

- `store.tsx` holds all generator state and the actions. Every action is a small recipe
  transformation followed by a host call.
- `components/` holds the panels:
  - left: `GeneratorPanel`
  - centre: `ConceptView`
  - right: `SidePanel`
- `pages/` holds Vault, Packs, Stats and Settings.
- `styles.css` uses only `--c-*` theme tokens, so every theme works automatically.
- Fonts (Rubik and Permanent Marker) are bundled, so the app is fully offline.

## Adding things

- **New ingredients or packs:** see `CONTENT_GUIDE.md`, then run `bgg validate`.
- **A new category:** add it to `Categories.All`, wire a slot in `Scopes.Plan`, add a field in
  `TemplateEngine.FieldCategories`, and write templates that use it.
- **A new theme:** drop a JSON file in `content/themes/` with the 14 required colours. Optionally
  add `onAccent`, the text colour on primary buttons.
- **A new API method:** add it to `ApiDispatcher.Dispatch`, `api.ts` and `types.ts`.
