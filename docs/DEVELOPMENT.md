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
git add Assets Packages ProjectSettings docs README.md
git commit -m "Describe your change"
git push
```

Unity `.meta` files must be committed with their assets: they contain the identifiers used by scenes and prefabs. They are hidden in VS Code Explorer to reduce clutter, but Git still tracks them. Make future asset moves in Unity's Project window so Unity moves the matching `.meta` files automatically.

Unity regenerates `Library`, `Logs`, `Temp`, `UserSettings`, C# project files and solution files. They are excluded from Git and hidden from the normal Explorer/Finder view. Assets, Packages and ProjectSettings remain visible and versioned. Finder Command-Shift-Period reveals hidden files when needed.

## Working independently

Open the Desktop folder with either launcher. Read `README.md` for the map. Edit card values under `Assets/Pupverse/Data`; change layouts in the Unity scenes; edit C# under `Assets/Pupverse/Scripts`. No Codex session is required.
