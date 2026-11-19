# BadGameGen Content Guide

Everything BadGameGen says comes from **content packs**: plain JSON files in `content/packs/`
(built-in) or the user's packs folder (imported). No recompiling needed to add ideas.

Validate and preview a pack at any time:

```bash
dotnet run --project src/BadGameGen.Cli -- validate content/packs
dotnet run --project src/BadGameGen.Cli -- generate --count 10 --chaos 0.5
```

## Pack file

```json
{
  "format": 1,
  "id": "horror",                     // lowercase, digits, dashes. Must be unique.
  "name": "Horror Bits",
  "version": "1.0.0",                  // bump when content changes (seeds are tied to versions)
  "author": "Bad Ideas Works",
  "description": "Things that go bump, scream, or file paperwork in the night.",
  "items": { "<category>": [ <item>, ... ] },
  "templates": { "<section>": [ <template>, ... ] }
}
```

Comments (`//`) and trailing commas are allowed.

## Items

```json
{
  "id": "flooded-baltic-town",           // unique within its category in this pack
  "label": "Flooded Baltic town",        // short display text for chips and menus
  "forms": { "noun": "flooded Baltic town", "in": "in a half-flooded Baltic fishing town" },
  "tags": ["baltic", "water", "cold", "town"],
  "affinity": ["survival", "radio", "boats"],  // likes appearing with items carrying these tags
  "excludes": ["space", "desert"],              // refuses items with these tags (unless chaos is high)
  "weight": 1.0,                                // relative pick chance (default 1)
  "weirdness": 0.2,                             // 0 grounded … 1 unhinged (chaos slider)
  "silliness": 0.2,                             // 0 serious … 1 clown shoes (tone slider)
  "scope": [1, 4]                               // optional: scope levels it suits (0 tiny jam … 4 absurd)
}
```

Full ids are namespaced automatically: `baltic:setting.flooded-baltic-town`. Two packs can
never collide.

### Forms per category

Templates glue forms into sentences, so **each form must fit the slot exactly**. Lowercase
unless it's a proper noun. No trailing punctuation. No articles unless stated.

| Category | Forms | Must read correctly in… |
|---|---|---|
| `genre` | `adj`, `noun` | "a **{adj}** deckbuilder" / "this is a **{noun}**" e.g. adj `survival`, noun `survival game` |
| `subgenre` | `noun` | "a survival **{noun}**" e.g. `management game`, `deckbuilder`, `metroidvania` |
| `setting` | `noun`, `in` | "a **{noun}**" / "set **{in}**" e.g. noun `flooded Baltic town`, in `in a flooded Baltic town` (`in` starts with its own preposition: in/on/inside/aboard…) |
| `era` | `in` | "set in a town **{in}**" e.g. `in the late 1980s`, `after the internet died`. May be `""` for present day. |
| `protagonist` | `noun` | "you play a **{noun}**" e.g. `stranded radio operator` (singular, no article) |
| `perspective` | `noun` | "seen from a **{noun}**" e.g. `top-down view`, `first-person view`, `fixed security-camera angle` |
| `objective` | `verb` | "you must **{verb}**" e.g. `keep the survivors connected` (base form) |
| `loop` | `verb` | "each day you **{verb}**" e.g. `scavenge by day and fortify by night` (base form) |
| `mechanic` | `noun`, `verb` | "built around **{noun}**" / "you **{verb}**" e.g. noun `radio triangulation`, verb `triangulate distress signals` |
| `progression` | `verb` | "over time you **{verb}**" e.g. `upgrade your antenna network` |
| `threat` | `noun`, `clause` | "a story about **{noun}**" / "meanwhile, **{clause}**" e.g. noun `rising water`, clause `the water creeps higher every day` (full present-tense clause) |
| `resource` | `noun` | "you never have enough **{noun}**" e.g. `fuel`, `spare batteries`, `goodwill` (plural or uncountable) |
| `mood` | `adj` | "the tone is **{adj}**" e.g. `moody`, `darkly comic`, `relentlessly wholesome` |
| `visual` | `noun` | "presented in **{noun}**" e.g. `chunky retro 3D`, `hand-inked comic panels` |
| `twist` | `clause` | "the twist: **{clause}**" e.g. `the coastline changes every day` (full clause) |
| `multiplayer` | `phrase` | sentence start: "**{phrase}**." e.g. `play solo or in two-player co-op` |
| `titleFirst` | *(label only)* | first word(s) of a title: `Frost`, `Velvet`, `Last` |
| `titleLast` | *(label only)* | last word(s) of a title: `Signal`, `Protocol`, `Tide` |
| `creep` | `pitch`, optional `connects` | Feature Creep proposal, see below |

`[a]` in templates picks *a/an* automatically, so never put an article in `noun` forms of
`genre`, `subgenre`, `protagonist`, `perspective` or `setting.noun`.

### Title parts

`titleFirst` / `titleLast` items are chosen by **tag overlap with the concept**, so tag them
with the moods/themes they evoke (`cold`, `radio`, `horror`, `cozy`…). That's how "Frost" meets
"Signal" in a Baltic radio game.

### Feature Creep items (`creep`)

```json
{
  "id": "rival-network",
  "label": "Rival radio network",
  "kind": "system",                    // system | loop | content | whyNot
  "complexity": 4,                     // 1..10, feeds the creep meter
  "forms": {
    "pitch": "A rival {protagonist.noun} broadcasts from across town, poaching your listeners.",
    "connects": "It turns {resource.noun} into a bidding war."
  },
  "tags": ["social", "radio"], "affinity": ["radio", "communication"]
}
```

`pitch` and `connects` are **templates** (see below), so they can reference the concept. A creep
item is only offered when every field it references exists in the concept.

Kinds: `system` = extra supporting system, `loop` = secondary gameplay loop,
`content` = more stuff, `whyNot` = unnecessary but delightful.

## Templates

```json
{ "id": "classic", "text": "[a] {genre.adj} {subgenre.noun} {setting.in}.", "weight": 1, "scopes": [0,1], "tagsAny": ["horror"] }
```

- `{field.form}`: form of the picked item. `{field}` alone means its label.
- `{secondary*.noun}`: every supporting mechanic, joined "a, b and c".
- `{title}`: the concept title (not allowed in `title` templates).
- `{first}` / `{last}`: title parts (only in `title` templates).
- `[a] word`: becomes "a word" / "an word" correctly (handles "an hour", "a unicorn", "an RPG").
- Sentences are capitalised automatically, double spaces and empty forms are cleaned up.
- A template is only used if every field it references exists (e.g. tiny jams have no `multiplayer`).
- `scopes` restricts to scope levels; `tagsAny` restricts to concepts with one of those tags
  (and doubles its chance when eligible). The tag must appear on at least two picked items, so one stray item doesn't theme the whole pitch.

Fields: `genre subgenre setting era protagonist perspective objective loop mechanic secondary
progression threat resource mood visual twist multiplayer` (+ `title`, `first`, `last`).

Sections: `title hook pitchOpen pitchLoop pitchSystems pitchThreat pitchTwist pitchStyle badIdea
mightWork scopeNote`. The short pitch is pitchOpen → pitchLoop → pitchSystems → pitchThreat →
pitchTwist → pitchStyle.

## Shared tag vocabulary

Tags are free text, but packs only "talk to each other" when they share tags. Prefer these:

**Themes:** `horror cozy fantasy scifi baltic nautical water cold heat desert forest urban rural
town space underground sky medieval historical modern future postapoc office bureaucracy nature
animals food music radio tech internet magic religion crime war sport school family romance death
time weather mystery cosmic robots plants retail travel trains cars mail art science health`

**Play:** `survival management building combat stealth puzzle social trade exploration racing
rhythm narrative crafting physics strategy action deduction farming cooking timing chaos`

**Tone:** `grim wholesome absurd mundane comedy melancholy tense epic weird`

## Writing tips

- Specific beats generic. "Stranded radio operator" > "survivor".
- Write for the **sentence**, not the label. Read your forms aloud in the table's example slot.
- Keep it short. Forms are building blocks, not paragraphs.
- Mix grounded (low weirdness) and unhinged (high weirdness) so the chaos slider has range.
- Be funny, never cruel. No real people, brands, or existing game names.
