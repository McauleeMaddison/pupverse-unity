# Day One validation — 21 September 2026

- Existing web tests: 6 passed, 0 failed.
- Unity Editor 6000.6.0f1 imported and compiled the project successfully.
- Both serialized scenes and card/hero prefabs generated successfully.
- Unity EditMode tests on the final Desktop project: 12 passed, 0 failed. All five Raven/Brooklyn comparisons match the web rules; draw, invalid inputs, content integrity and once-only round resolution covered. Both reorganised scenes were loaded and checked for missing scripts and broken controller/card references.
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

See VALIDATION.md for what was executed and what remains to be tested on hardware.

## Repository and navigation

- Desktop folder: `Pupverse`; VS Code workspace: `Pupverse.code-workspace`.
- Private remote: `https://github.com/McauleeMaddison/pupverse-unity`.
- Simplified source folders with all Unity asset GUIDs preserved.
- Git ignores caches, builds, local settings and signing/secret files; Unity metadata remains tracked.
