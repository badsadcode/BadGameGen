# Ideas & Roadmap

## Shipped in v0.1.0, beyond the brief (Sage's creep 😏)

- **Colour schemes.** Six built-in themes as JSON, and users can add their own.
- **Recipe codes** (`BGG1-…`). Shareable codes that reproduce an idea exactly, including locks,
  rerolls and accepted creep. Paste one into the seed box.
- **Word seeds.** "crab office" is a perfectly valid seed.
- **Bad Idea of the Day** (☀ button). The same idea for everyone with the same packs.
- **Procedural cover art.** A seeded scene in theme colours that reacts to the concept: water
  gets waves, towns get skylines, horror gets an eclipse. There's no image AI involved.
- **Keep-on-reroll.** Rerolling one field changes exactly that field.
- **Race sampling.** Enabling a new pack doesn't scramble everyone's old seeds.
- **Reword.** Same ingredients, different sentences.
- **Banish.** Exclude an item forever from the picker.
- **Session history.** Back and forward through the ideas from this session.
- **Codename** (`BG-418920-MC`). Encodes the seed, the scope letter and a creep flag.
- **Creep kind filters.** The four chips on the Feature Creep card.
- **Keyboard-first.** `G` generate, `R` reroll, `F` creep, `S` save, `?` for the rest.
- **CLI** (`bgg`). For batch generation, plus pack validation in CI.
- **💜 Noor Mode** (v0.1.1). Permanent Feature Creep, one extra proposal per idea, purple-heart
  bursts, Noor's commentary and her theme. Secret activation: type `noor` on the Generator.
- **Comfort pass** (v0.1.2): laptop-friendly layout, "what changed" highlights, sliders that set
  up the *next* idea, a "Just made" history that survives restarts, collapsible notes and art,
  and labelled Keep/Nope buttons on creep proposals.
- **Click-to-edit phrases** (v0.1.3): every ingredient phrase in the pitch is clickable
  (swap, reroll, lock, choose, banish), with hover-linking to the ingredient rows.
- **Inline alternatives** (v0.1.3): two one-click suggestions under each main ingredient.
- **Editable library** (v0.1.2): packs live as JSON in a visible folder, reload live, and
  updates never overwrite your edits.
- **Single-file exes** (v0.1.1). `BadGameGen.exe` and `bgg.exe` carry everything inside them.

## v0.2.x: smarter combinations

- **Library editor in-app:** add or edit items with live form previews ("reads as: *set in a
  half-flooded Baltic fishing town*").
- **Per-category grammar checks:** warn when a protagonist starts with an article, a verb form
  isn't base form, and so on.
- **Weighted "themes" control:** slide Horror up and Cozy down without disabling packs.
- **Constraint explanations:** "Space station excluded *drowning* because of `space` ↔ `water`".
- **More templates per section:** about 10 hooks and 6 per pitch section, so wording varies more.
- **Second-order creep:** creep items that react to *other accepted creep* ("Your boat system
  now needs a harbour").

## v0.3.x: packs and vault quality

- Pack versions with migration notes. The vault already flags ideas whose packs changed.
- Generation history log, plus "restore the idea I had 20 minutes ago".
- Vault collections or boards, e.g. "Games of the Week shortlist".
- Bulk export of the vault to one Markdown file.

## Later / optional

- **AI elaboration** (opt-in, never required): turn a saved concept into a one-page design doc.
- **Games of the Week brief export:** a schedule-shaped template with day-by-day build steps
  from the scope note.
- **Mashup:** breed two vault ideas into a child concept that inherits locked fields from each.
  *Very* on brand.
- **Pitch roulette for streams:** a big-screen mode that spins ingredients like a slot machine.

## Out of scope (per the brief)

Accounts, cloud sync, multiplayer, compulsory LLM calls, giant node graphs, project-management
suites, automatic game creation, and generated artwork for every concept.
