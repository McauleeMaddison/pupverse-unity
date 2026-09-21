# Existing web project inspection

Inspected 21 September 2026 at `/Users/mcauleemaddison/Desktop/vs code projects/pupverse`.
Git HEAD: `c242d1efd6ee8f47a1564746db837b98106c8b8d`. The working tree was clean at inspection. No source files or secrets in the web project were modified or copied into Unity.

## Stack and entry points

The project is a JavaScript ES-module Vite app with Three.js presentation, Supabase services and a Capacitor iOS wrapper. `package.json` declares Vite 8.0.12, Three.js 0.186.0, Supabase JS 2.108.2 and Capacitor 8.4.3. `src/main.js` composes the app and binds the menu, tutorial, shop, vault, daily tasks and arena UI. `src/style.css` and `src/ui/visualUpgrade.css` own web styling.

| System | Evidence | Unity Day One decision |
| --- | --- | --- |
| Card catalogue | `src/data/cards.js`: 30 definitions across CryptoPups, CyberPups, AlienPups | Import Raven, Brooklyn, Jinx; retain a full reference snapshot |
| Statistics | `src/app/constants.js`: Power, Speed, Intelligence, Defence, Luck | Same five C# enum values and fields |
| Combat | `src/game/battleRules.js`: base + selected-stat ability bonus on both cards, highest total wins, equality draws | Port as pure BattleRules and guarded BattleRound |
| Abilities | Parser accepts `+N Stat` and `Stat by +N`, plus structured boosts | Export structured boosts; no prose parsing during Unity play |
| Rarity | Common, Uncommon, Rare, Epic, Legendary, Mythic, Mystic visual treatments in `src/ui/worlds.js` | Explicit enum, visible rarity badges |
| Pack shop | `src/data/packs.js`: three-card packs costing 5/8/10 coins | Document only; no Unity economy yet |
| Local progression | `src/game/state.js`: `pupverse-save-v3`, coins, XP, vault, wins, decks, daily tasks and rewards | No import of browser localStorage; training score is session-only |
| Account services | `src/services/arenaBackend.js`, `src/lib/supabase.js` | Future adapter; no connection or credentials in this starter |
| Online arena | Match queues, friend rooms, realtime subscriptions, round resolution, presence/forfeit, report/block | Future server-authoritative integration |
| Protected progression | Supabase Edge Functions `progression`, `arena`, `arena-cron`, SQL migrations | Keep the existing trust boundary when porting; do not award live coins or rank locally |
| Background/worlds | `animatedBackground.js`, `worlds.js`, `arenaWorld.js`: stars, Bloom Nebula/Crypto Coast/Cyber Grid, quality modes | Native layered Bloom Nebula-inspired UI stage |
| Card image handling | `src/ui/cardImages.js`, `public/cards` | Keep original references; display portrait windows without baked stat panels |
| Analytics | `src/utils/analytics.js`, local product event hooks | Deferred |
| Existing iOS wrapper | `capacitor.config.json`, `ios/`, `docs/IOS_APP.md` | Separate Unity native project; existing Capacitor app remains intact |

## Important data/art mismatch

The Raven image prints Legendary, older numbers, 2024 and Night Prowl, while `cards.js` defines Mythic, 2025 and Nova Howl. Brooklyn's image prints +18 Speed, while code gives +15 Speed. The Unity prototype follows the live JavaScript data, and uses a separate portrait/hero presentation so incompatible printed values are not shown as authoritative.

Raven: Power 91, Speed 82, Intelligence 90, Defence 84, Luck 77. Nova Howl: +18 Power, +14 Intelligence.
Brooklyn: Power 85, Speed 87, Intelligence 78, Defence 80, Luck 79. Coast Rush: +10 Power, +15 Speed.

The source has no dedicated fact field. Unity facts are short paraphrases of each existing ability's lore, and are editable in CardData. They are not invented biological facts.

## Scope and migration order

Day One is a native presentation and deterministic local-combat foundation. Next, port collection and deck interfaces, add an authenticated Supabase service adapter, then implement online match synchronization and economy through the existing protected endpoints. Web DOM, CSS, JavaScript and Three.js code cannot simply be copied into Unity C#.

`docs/APP_DIRECTION.md` in the web source recommends keeping that app as a web game. The user's current request explicitly asks for a separate Unity build; this new project follows that request without changing the web project.

## Baseline verification

Ran the existing web Node tests: 6 passed, 0 failed. Covered ability parsing, both-card bonuses/ties, finite catalogue totals, rarity settings, graphics-setting recovery and once-only local rewards/pack saves.


# Asset provenance

- `raven-front.png`, `brooklyn-front.png`, `jinx-front.png`: copied from the user's existing PupVerse `public/cards` directory, unchanged. Kept for reference and runtime portrait UV windows; source artwork includes numbers which may differ from code.
- `RavenHero.png`: new transparent sprite generated with the built-in imagegen tool on 21 September 2026, using the existing Raven portrait as identity reference. The standalone sprite is a new interpretation, not a rigged or frame-by-frame character. Idle movement is transform-based.
- `Art/Generated`: simple rounded rectangle, disc, radial glow and ring textures made by C# code in DayOneBuilder. These support the layered native interface and are regenerable.
- Typography: Unity's built-in LegacyRuntime font. No downloaded font licence dependency.
- `WebCards.json`: three definitions and structured ability boosts exported from the user's JavaScript catalogue and rule helper. `docs/WebCatalogueSnapshot.json` preserves all 30 definitions for migration reference.
- Facts: new concise paraphrases derived from source ability descriptions, separately editable.

Source projects remain separate. No `.env` values, service keys, browser saves or Apple credentials were imported.
