# BadGameGen — Design Brief for Sage

**Tagline:** Terrible ideas. Surprisingly playable games.

## Goal
A standalone offline-first C# desktop application with a WebView2 front end (React + TypeScript proposed), dedicated to producing short, coherent, sometimes absurd game ideas using extensive structured libraries of words, phrases, genres, game mechanics, and design ingredients. A generator for Bad Ideas Works / Games of the Week, not a mandatory LLM wrapper.

**Design reference:** `BadGameGen_UI_Mockup.png`. It is a visual mood board and concept, not a rigid implementation specification.

## Product principles
- Generate a useful short concept immediately, with optional one-line and expanded views.
- Use **large editable, data-driven libraries**, not a tiny list hard-coded into UI components.
- Support deterministic seeds, weighted selection, compatibility/exclusion rules, and creative surprises.
- Keep operation offline, fast, keyboard-friendly, and transparent.
- Be genuinely useful, but keep the playful BadSad personality.

## Generation ingredients
Maintain independent content libraries for: genre; subgenre; setting/biome/location; time period; protagonist/player role; camera perspective; objectives; gameplay loop; core mechanic; secondary mechanics; progression; threats/antagonists; resources; mood/tone; visual style; unusual twists; scope; optional multiplayer model. Include reusable title fragments and text templates.

Each item should have a stable ID, display text, optional descriptive phrases, tags, weight, compatibility/incompatibility tags, and optional scope/genre affinities. Prefer JSON or another accessible structured format. Store library/version metadata and allow content additions without recompilation.

## Generator behaviour
1. Select suitable ingredients using weighted random picks with a reproducible seed.
2. Respect locked fields and user-selected exclusions.
3. Resolve conflicting items using compatibility rules; high chaos may intentionally allow strange combinations, but descriptions must remain understandable.
4. Construct a coherent pitch through output templates and appropriate grammatical phrasing rather than concatenating random field labels.
5. Output working title, one-line hook, short paragraph, and optional expanded concept.
6. Reroll an individual field or all unlocked fields. Distinguish full seed reruns from partial rerolls so results remain reproducible.
7. Make it easy to copy, export, and save.

## Main controls
- **Generate Trouble** primary button.
- Genre/setting/mechanics/tone/visual style/twist and other categories with select, lock, and reroll controls.
- **Chaos slider:** low = grounded, medium = unusual yet viable, high = audacious combinations.
- **Scope control:** tiny jam, small indie, medium, ambitious, absurd. The scope must constrain feature count and suggested systems.
- **Serious ↔ silly** tone control.
- Seed display / input / copy / replay.
- Content pack enable/disable, search, and selection.
- Output mode: one-liner, short pitch, expanded simple concept.

## Obligatory Feature Creep Mode 😈
Prominent optional toggle: **FEATURE CREEP MODE** (subtitle: *Just one more system...*).

When ON, each generation receives a few entertaining but concept-relevant additions, for example:
- An extra supporting system that unexpectedly fits the core loop.
- A secondary gameplay loop, with a short explanation of how it connects.
- An unnecessary-but-delightful feature or emergent interaction.
- Optional **"And another thing..."** reroll to append one additional feature at a time.
- A **scope warning** or humorous **creep meter** indicating how much complexity has been added.

Keep additional systems visually separate from the original core pitch, so the base idea stays accessible. Allow each added feature to be accepted, rejected, or rerolled individually. Ensure Feature Creep Mode is opt-in and doesn't silently overwrite a saved concept.

## Idea Vault
Store title, generated pitch, ingredient IDs and values, seed, library/template versions, creation/update timestamps, tags, notes, favorite flag, and any accepted feature-creep additions.

Support search, filtering, favorites, duplicate, rename, notes, delete with confirmation, and local persistence. Export concepts to Markdown, plain text, and optionally JSON. Saved ideas must remain readable even after libraries change.

## Content packs
Provide built-in packs and an extensible pack format: Core, Horror, Fantasy, Sci-Fi, Cozy, Baltic, Management Mayhem, Games of the Week, etc. Packs should be inspectable, switchable, and importable. Validate external pack data. Avoid collisions by using stable namespaces/IDs.

## UI direction — BadSad style
- Dark near-black/plum surfaces; purple, magenta, and restrained neon-pink accents.
- Bold playful typography for idea names; readable compact controls everywhere else.
- Subtle borders/glow, rounded cards, crisp layout, energetic but not visually exhausting.
- Three main areas: **left** generation controls; **center** generated concept/preview and actions; **right** Idea Vault, favorites, and notes.
- Top navigation: Generator, Vault, Packs, Statistics, Settings.
- Status line: active seed, active packs, generator mode, Feature Creep Mode status.
- UI mockup image shows an illustrative concept (including optional image thumbnail). **No image generation engine is required in v0.1.0.**

## Example concept
**Frost Signal** — A survival management game set in a flooded Baltic town. You play a stranded radio operator who keeps survivors connected while rising water threatens supplies and routes. Repair antennas, manage scarce fuel, coordinate rescues, and decide which incoming calls to trust. Twist: the coastline and accessible roads change day by day. Feature Creep Mode might propose a boat-upgrade system or rival radio network.

## Suggested implementation organization
- C#/.NET desktop shell and application services.
- WebView2 frontend; React + TypeScript if it matches the surrounding application infrastructure.
- A **headless generation library** for selection, weighting, constraints, templates, seeds, and data validation.
- Separate vault persistence layer and content pack loader.
- Typed API between frontend and host; error reporting and recovery for corrupt packs.
- Unit tests for determinism, field locks, constraints, grammar/template generation, vault persistence, and pack loading.

## Milestones
### v0.1.0 — Playable generator
Desktop shell; polished generator UI; meaningful starter libraries; short pitch generation; field locks/rerolls; deterministic seeds; chaos and scope; Feature Creep Mode basics; copy/export Markdown; local Idea Vault.

### v0.2.x — Smarter combinations
Broader libraries; weighted compatibility; grammar/templates; improved scope limiting; richer creep suggestions; basic user library editing.

### v0.3.x — Packs and vault quality
Pack manager, search and favorites, notes, tags, import/export, reproducibility against content revisions, generation history, statistics.

### Later, optional
AI elaboration for saved concepts; Games of the Week brief export; larger/custom shared packs; more complex concept variation. None of these may be required to use the core app.

## Out of scope for initial version
Accounts, cloud sync, multiplayer, compulsory LLM calls, giant node graphs, project-management suite, automatic game creation, generated artwork for every concept.

## Acceptance criteria for first release
- Repeated generations produce varied, intelligible game pitches.
- Entering the same seed with the same library configuration reproduces the concept.
- Locked categories stay unchanged when generating again.
- Chaos and scope visibly affect outcomes without breaking generation.
- Feature Creep Mode adds optional related ideas and can be disabled cleanly.
- Ideas save, reopen, copy, and export locally with their original contents intact.
- Application runs offline.

**Bad Ideas Works motto:** Code shall remain understandable to whichever AI inherits this mess.
