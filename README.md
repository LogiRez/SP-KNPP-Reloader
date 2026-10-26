# SP-KNPP Reloader

A standalone tool for refreshing Kino `.knpp` car-part packs in CarX Drift Racing Online without returning to the garage. After reloading, it rebuilds your current car to reapply its selected custom parts.

## Requirements

- Windows x64 and CarX Drift Racing Online through Steam.
- Kino installed and loaded. This release requires the inspected Kino **3.39.1** build; other builds are rejected.

## Setup

1. Extract the release archive into its own folder.
2. Keep `SP-KNPP Reloader.exe`, `SP-KNPP.Mod.dll` and `Licenses/` together.
3. Run `SP-KNPP Reloader.exe` and select **Launch CarX & load tool**. It can also connect to an already running game.
4. Wait for Kino and your car to finish loading in single-player, training or multiplayer.
5. Press **F7** to open the panel.

Close CarX and the launcher before replacing files when updating.

## Reloading packs

1. Finish exporting or copying your `.knpp` files into:

   ```text
   CarX Drift Racing Online/kino/mods/KN_Base/car_parts/
   ```

2. Leave **Force reread existing packs (same filename)** enabled when replacing an existing pack.
3. Click **Reload car-part packs** and wait for the refresh and car rebuild to finish.

The tool reapplies the car’s selected custom parts. Newly added parts must be selected through Kino first; the tool does not choose a new body kit automatically.

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
- Build and automated checks pass. The standalone workflow, game modes and multiplayer results still need in-game verification.

## Troubleshooting

- **Missing DLL:** Keep `SP-KNPP.Mod.dll` beside the launcher.
- **Kino or car not ready:** Wait for both to finish loading in a driving session.
- **Unsupported Kino build:** Use the inspected 3.39.1 build or a matching tool update.
- **Missing or duplicate packs:** Check Kino’s errors and confirm the files finished copying.
- **Reload error:** Check the report below. Include it, your Kino version and game mode when reporting an issue.

## Credits

Created by rez. Unofficial tool for CarX Drift Racing Online and Kino.

Includes HarmonyX, Mono.Cecil and MonoMod components. 
