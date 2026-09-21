# Pupverse

Native Unity mobile starter: an animated Raven menu and a two-card training battle, based on your existing PupVerse web game.

## Open and play

- **Unity:** double-click `Open in Unity.command`, or add this folder to Unity Hub. Use **Unity 6000.6.0f1**. Open `Assets/Pupverse/Scenes/MainMenu.unity`, then press Play.
- **VS Code:** double-click `Open in VS Code.command` or open `Pupverse.code-workspace`.
- **First battle:** Enter the arena → choose Power for a Raven victory or Speed for a Brooklyn victory → Play Again. Home returns to the menu.

You can edit everything manually without Codex.

## Folder map

```text
Pupverse/
├── Assets/Pupverse/
│   ├── Art/          Artwork and interface textures
│   ├── Data/         Editable card assets
│   ├── Scenes/       MainMenu and Battle
│   ├── Prefabs/      Reusable hero and card templates
│   ├── Scripts/
│   │   ├── Game/     Card data, battle rules and game flow
│   │   ├── UI/       Card display, menu and screen layout
│   │   └── Motion/   Idle, entrance, tilt and victory effects
│   ├── Editor/       Scene generator and build commands
│   └── Tests/        Unity rule tests
├── Packages/         Unity package dependencies
├── ProjectSettings/  Unity and iOS configuration
├── docs/             Development guide, project notes, validation
├── tools/            Test launcher
├── Pupverse.code-workspace
└── README.md
```

Unity creates cache, log and solution files automatically. They are hidden from the normal file view and excluded from Git.

## Common edits

| Change | Where |
| --- | --- |
| Statistics, rarity, fact or ability | Select a card in `Assets/Pupverse/Data` and use the Inspector |
| Menu or battle layout | Open the scene and edit its named UI objects in Unity |
| New card | Create → Pupverse → Card; assign it to a card view |
| Battle comparison rules | `Scripts/Game/BattleRules.cs` |
| Character idle, card tilt or parallax | `Scripts/Motion` |
| iOS Xcode export | Unity menu → Pupverse → Export iOS Xcode project |

Scenes are saved and editable. **Pupverse → Day One → Build starter scenes** recreates the template and replaces generated scenes/prefabs/data; duplicate customised files before using it.

## GitHub backup

Private repository: [McauleeMaddison/pupverse-unity](https://github.com/McauleeMaddison/pupverse-unity).

This folder is a Git repository. Use VS Code **Source Control → Commit → Sync Changes** after reviewing edits. Saving a file alone does not upload it. The remote is `origin` and the default branch is `main`.

Unity `.meta` files are tracked even though hidden in Explorer. Move assets in Unity's Project window so their references stay intact.

## Status

Implemented: layered menu, Raven hero and idle, real CardData assets, five statistics, rarity/fact, entrance and tilt, two-card battle, victory/loss/draw, replay and reduced motion. Unity and Mac player checks are recorded in `docs/VALIDATION.md`.

iOS player settings are configured. Choose your Apple Developer team in Xcode for physical-device signing. Touch/accelerometer behaviour and performance still require device testing. Accounts, online battles, packs and persistent economy are future migration work.

Read [Development](docs/DEVELOPMENT.md) for Git/iOS/VS Code, [Project notes](docs/PROJECT_NOTES.md) for source inspection and artwork, and [Validation](docs/VALIDATION.md) for tests and the Day One checklist.
