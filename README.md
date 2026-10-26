## SP-KNPP Reloader

A standalone tool for reloading Kino `.knpp` car-part packs in CarX Drift Racing Online without returning to the garage. It requests a pack reread, confirms the registry refreshed, then rebuilds your current car to reapply its selected custom parts.

RCLI is not required.

## Requirements

- Windows x64.
- CarX Drift Racing Online through Steam.
- Kino installed and loaded. This release requires the supported **3.39.1** build; other builds are rejected.

## Setup

1. Extract the release archive into its own folder.
2. Keep `SP-KNPP Reloader.exe`, `SP-KNPP.Mod.dll` and `Licenses/` together.
3. Run `SP-KNPP Reloader.exe` and select **Launch CarX & load tool**. It can also connect to an already running game.
4. Wait for Kino and your car to finish loading in single-player, training or multiplayer.
5. Press **F7** to open the panel.

Close CarX and the launcher before replacing files when updating.

## Reloading packs

1. Finish exporting or copying your updated `.knpp` files into:

   ```text
   CarX Drift Racing Online/kino/mods/KN_Base/car_parts/
   ```

2. Overwrite the existing pack, keeping its pack and part IDs when updating an equipped part.
3. Click **Reload car-part packs**.
4. Wait for the pack refresh and car rebuild to finish.

Reload requests a disk reread even when no file changes are detected. There is no separate full-reread option.

New parts must be selected through Kino first. The tool does not automatically choose a new body kit.

### Missing packs

Reloading is blocked if a previously loaded pack file is missing, to help prevent losing equipped-part selections.

Restore missing files before retrying. If an earlier reload already cleared your selections, re-equip the parts or load a saved Kino preset once.

## Controls

| Control | Action |
| --- | --- |
| F7 | Show or hide the panel |
| F9 | Unload when no reload is running |
| Hide | Hide the panel while keeping the tool loaded |
| Unload tool | Request unloading from the desktop launcher |

Wait for reloading to finish before switching cars, unloading or exiting. F9 also detaches RCLI if both tools are loaded.

## Notes

- Reloads are local. Other players need the updated pack files installed too.
- Do not run an RCLI pack reload at the same time.
- Repeated reloads can retain old assets in memory. Restart CarX after a long editing session.
- Reports include timings for pack refresh, car rebuild and the total operation.
- Build and targeted automated checks pass. Updated model loading, game-mode compatibility and multiplayer appearance still need in-game verification.

## Troubleshooting

- **Missing DLL:** Keep `SP-KNPP.Mod.dll` beside the launcher.
- **Kino or car not ready:** Wait for both to finish loading in a driving session.
- **Unsupported Kino build:** Use the supported 3.39.1 build or a matching tool update.
- **Missing packs:** Restore previously loaded files before retrying.
- **Missing or duplicate pack entries:** Check Kino’s errors and confirm the files finished copying.
- **Parts do not return after restoring a file:** Re-equip them or load a saved Kino preset if their selections were previously cleared.
- **Reload error:** Include the report below, your Kino version and game mode when reporting an issue.

Report location:

```text
CarX Drift Racing Online/UserData/SP-KNPP Reloader/sp-knpp-reload.txt
```

## Source files

| File | Purpose |
| --- | --- |
| AttachmentLobby.cs | Reads multiplayer room information. |
| BundledHarmony.cs | Loads bundled patching dependencies. |
| CarRebuild.cs | Allows the current car model to be recreated during reload. |
| Entry.cs | Loads and unloads the tool inside CarX. |
| Injector.cs | Connects the launcher to CarX and loads the mod DLL. |
| LauncherUI.cs | Provides the desktop launcher interface. |
| PackReload.cs | Reloads packs, checks missing files, rebuilds the car and records timings. |
| RcliSettings.cs | Handles local settings, logs and launcher status files. |
| Tool.cs | Provides the in-game panel, hotkeys and input handling. |
| build.ps1 | Builds the launcher and mod DLL using supplied references and dependencies. |

## Credits

Created by rez. Unofficial tool for CarX Drift Racing Online and Kino.

Includes HarmonyX, Mono.Cecil and MonoMod components. Their license notices are included in `Licenses/`.
