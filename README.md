# Pupverse

Editable Unity mobile game starter. Your project is on the Desktop, and its private backup is [pupverse-unity on GitHub](https://github.com/McauleeMaddison/pupverse-unity).

## Open and play

- **Unity:** double-click `Open in Unity.command`. Use Unity **6000.6.0f1**, open `Assets/Scenes/MainMenu.unity`, and press Play.
- **VS Code:** double-click `Open in VS Code.command` or open `Pupverse.code-workspace`.
- **Battle:** Enter the arena, then choose Power for a Raven victory or Speed for a Brooklyn victory. Play Again resets the round.

Everything can be edited manually without Codex.

## Folder map

```text
Pupverse/
├── Assets/
│   ├── Art/          Character art and interface textures
│   ├── Cards/        Editable card data
│   ├── Scenes/       MainMenu and Battle
│   ├── Prefabs/      Reusable hero and card
│   ├── Scripts/      All game, UI and motion C# files
│   └── Editor/       Build tools and Tests/
├── Packages/         Unity dependencies
├── ProjectSettings/  Unity configuration
├── Pupverse.code-workspace
├── Open in Unity.command
├── Open in VS Code.command
└── README.md
```

Unity also generates hidden cache, log and solution files. They are excluded from Git. Keep the required Packages and ProjectSettings folders; they make this a complete Unity project.

## Common changes

| Edit | Start here |
| --- | --- |
| Card name, stats, rarity, fact or ability | Select an asset in `Assets/Cards` and use the Inspector |
| Menu or battle layout | Open a scene in `Assets/Scenes` |
| Battle comparison rules | `Assets/Scripts/BattleRules.cs` |
| Buttons and battle flow | `Assets/Scripts/BattleController.cs` |
| Idle movement, tilt or entrance | `Assets/Scripts/IdleMotion.cs` and `CardMotion.cs` |
| iOS export | Unity menu → Pupverse → Export iOS Xcode project |
| Run the 12 Unity checks | Window → General → Test Runner → EditMode → Run All |

Each Unity component has its own C# file. Keep `.meta` files with their assets; they hold scene references. They are hidden from VS Code Explorer but tracked by Git. Use Unity's Project window to move assets during normal editing.

**Template rebuild:** Pupverse → Day One → Build starter scenes replaces generated scenes, prefabs and imported card data. Duplicate customised files before using it.

## Back up your edits

In VS Code: **Source Control → review changes → Stage → Commit → Sync Changes**. Saving alone does not upload files. The remote is `origin`; the branch is `main`.

This is a local training game. iOS settings are configured; signing and physical-device testing are still needed. Accounts, packs, online play and cloud saves are future work.

<details>
<summary>Development, iOS and VS Code</summary>

# iOS and VS Code

## Installed environment

- Unity Editor: 6000.6.0f1 (revision f7f8ed4d1e24).
- Its sibling `PlaybackEngines/iOSSupport` is installed.
- Xcode selected at `/Applications/Xcode.app/Contents/Developer`.
- VS Code at `/Applications/Visual Studio Code.app`.
- Unity package `com.unity.ide.visualstudio` 2.0.28; Microsoft Unity extension `visualstudiotoolsforunity.vstuc` plus existing C# tools.

## Committed player configuration

Product PupVerse; company Pupverse; version 0.1.0/build 1; initial bundle ID `com.pupverse.mobile`; iOS 15.0 minimum; iPhone and iPad; portrait; IL2CPP; device SDK; accelerometer 60 Hz. Native iOS uses ARM64. The scene list starts with MainMenu and then Battle. Classic input supports mouse, touches and accelerometer; no gyroscope permission is required by this code. Motion can be disabled from the menu.

Automatic signing is enabled, but **no Apple Developer team is selected**. Select your team in Xcode to run on a physical device. Confirm the final bundle identifier before TestFlight or release.

## Export

In Unity choose `Pupverse → Export iOS Xcode project`. Each export uses a new timestamped folder to preserve previous Xcode edits. It is a development export. To create a production archive later, disable development options in the build process and supply the final icon, signing and store configuration.

The Unity project can stay on the Mac target for fast Play-mode testing. The export method explicitly builds for iOS. If you manually use File → Build Profiles, switch to iOS and ensure the two scenes are included.

## VS Code

Open `Pupverse.code-workspace`. If IntelliSense has not populated yet, let Unity complete package import and use Unity Preferences → External Tools → Regenerate project files. Double-click a script in Unity. Use the VS Code Unity extension's Attach Unity Debugger command, or the included launch configuration.

The recommended package is the Visual Studio Editor package; the old `com.unity.ide.vscode` package is not used. See [Microsoft's Unity guide](https://code.visualstudio.com/docs/other/unity) and [Unity IDE support](https://docs.unity.com/en-us/engine/6000.7/manual/scripting/environment-and-tools/ide-support).

## Physical-device acceptance still needed

1. Run on a notched iPhone and iPad. Check portrait safe areas and readable labels.
2. Drag both cards, tilt the phone, release touch and background/resume the app. Ensure motion settles and buttons still respond.
3. Toggle motion off and confirm entrance, idle, tilt and victory motion are reduced.
4. Try all five statistics, repeated taps, replay, Home and reopening Battle.
5. Profile frame pacing, memory and battery on real hardware.

A local Mac smoke test or a successful Xcode export does not validate accelerometer behaviour, signing, device performance or App Store readiness. Current Unity [iOS build reference](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/iphone/ios-building-and-delivering/build-settings-ios).

## Save your changes to GitHub

Private repository: [McauleeMaddison/pupverse-unity](https://github.com/McauleeMaddison/pupverse-unity).
Local folder: `~/Desktop/Pupverse`. Default branch: `main`.

The local Git repository lives inside this project. VS Code's Source Control panel operates on that same repository.

1. Save your edited files in Unity and VS Code.
2. Open Source Control in VS Code and review the changes.
3. Stage the files you want to save, write a short commit message, and Commit.
4. Choose Sync Changes / Push to upload your commit to the private GitHub repository.

Saving a file alone does **not** upload it to GitHub. Commit and Push are the backup steps. Start each session with Pull if you have edited from another computer. Use a new branch for larger experiments.

Terminal equivalent:

```sh
git status
git add Assets Packages ProjectSettings README.md
git commit -m "Describe your change"
git push
```

Unity `.meta` files must be committed with their assets: they contain the identifiers used by scenes and prefabs. They are hidden in VS Code Explorer to reduce clutter, but Git still tracks them. Make future asset moves in Unity's Project window so Unity moves the matching `.meta` files automatically.

Unity regenerates `Library`, `Logs`, `Temp`, `UserSettings`, C# project files and solution files. They are excluded from Git and hidden from the normal Explorer/Finder view. Assets, Packages and ProjectSettings remain visible and versioned. Finder Command-Shift-Period reveals hidden files when needed.

## Working independently

Open the Desktop folder with either launcher. Read `README.md` for the map. Edit card values under `Assets/Cards`; change layouts in the Unity scenes; edit C# under `Assets/Scripts`. No Codex session is required.


</details>

<details>
<summary>Existing systems and artwork provenance</summary>

# Existing web project inspection

Inspected 21 September 2026 at `/Users/mcauleemaddison/Desktop/vs code projects/pupverse`.
Git HEAD: `c242d1efd6ee8f47a1564746db837b98106c8b8d`. The working tree was clean at inspection. No source files or secrets in the web project were modified or copied into Unity.

## Stack and entry points

The project is a JavaScript ES-module Vite app with Three.js presentation, Supabase services and a Capacitor iOS wrapper. `package.json` declares Vite 8.0.12, Three.js 0.186.0, Supabase JS 2.108.2 and Capacitor 8.4.3. `src/main.js` composes the app and binds the menu, tutorial, shop, vault, daily tasks and arena UI. `src/style.css` and `src/ui/visualUpgrade.css` own web styling.

| System | Evidence | Unity Day One decision |
| --- | --- | --- |
| Card catalogue | `src/data/cards.js`: 30 definitions across CryptoPups, CyberPups, AlienPups | Import Raven, Brooklyn and Jinx; other cards remain in the web project |
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
- `WebCards.json`: three definitions and structured ability boosts exported from the user's JavaScript catalogue and rule helper. The original 30-card snapshot remains in Git history if needed for migration.
- Facts: new concise paraphrases derived from source ability descriptions, separately editable.

Source projects remain separate. No `.env` values, service keys, browser saves or Apple credentials were imported.


</details>

<details>
<summary>Validation and Day One checklist</summary>

# Day One validation — 21 September 2026

- Existing web tests: 6 passed, 0 failed.
- Unity Editor 6000.6.0f1 imported and compiled the project successfully.
- Both serialized scenes and card/hero prefabs generated successfully.
- Unity EditMode tests after the final folder minimization: 12 passed, 0 failed. All five Raven/Brooklyn comparisons match the web rules; draw, invalid inputs, content integrity and once-only round resolution covered. Both reorganised scenes were loaded and checked for missing scripts and broken controller/card references.
- Native Mac development player built successfully.
- Automated player smoke test passed: menu, enter arena, card binding, victory, duplicate-click guard, replay, defeat and return home.
- MainMenu and Battle screenshots visually inspected at 540 × 960.
- Microsoft Unity VS Code extension 1.3.1 installed; Unity selected VS Code as external editor; solution generated.
- iOS configuration written and installed iOS Build Support verified. No signed iPhone build, Xcode export or physical-device test performed.

Known limits: local two-card training only; no accounts, cloud saves, online arena or economy port. Device tilt and performance require an actual iPhone/iPad check. The development smoke-test helper was updated to current Unity APIs during the cleanup. No C# compilation errors or player exceptions were found. Native Mac play-through screenshots were captured before the folder/namespace rename; scene integrity tests were run again after it.


# Pupverse Mobile — Day One

- [x] Existing web project inspected
- [x] Existing systems documented
- [x] New Unity project created
- [x] iOS player settings configured (device signing/testing still needed)
- [x] VS Code connected, Unity extension installed
- [x] Proper project structure
- [x] MainMenu scene
- [x] Layered background
- [x] Hero character
- [x] Character idle movement
- [x] CardData system
- [x] First real card: Raven, with Brooklyn as the rival
- [x] Five statistics
- [x] Rarity
- [x] Fact derived from existing card lore
- [x] Card entrance animation
- [x] Touch/tilt interaction implemented (physical-device verification pending)
- [x] Battle scene
- [x] Two-card comparison
- [x] Victory animation

Physical-device checks remain outstanding as noted above.

## Repository and navigation

- Desktop folder: `Pupverse`; VS Code workspace: `Pupverse.code-workspace`.
- Private remote: `https://github.com/McauleeMaddison/pupverse-unity`.
- Six direct Assets folders, flat Scripts, one README; all 36 asset-file GUIDs preserved. Historical screenshots/catalogue/test exports are available in Git history.
- Git ignores caches, builds, local settings and signing/secret files; Unity metadata remains tracked.


</details>

