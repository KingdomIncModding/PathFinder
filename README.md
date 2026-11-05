# Path Finder 1.1.0

[Downloads](https://www.nexusmods.com/kingdominc/mods/8) · [Source and issues](https://github.com/urakkaamyx/KingdomInc_PathFinder)

See a node's immediate input and output paths at a glance. Hover a node to highlight its direct connections in bright neon colors, with **one solid color per line** and a soft glow.

Research **Path Finder** for **100 gold** under **Tech Tree → Mods** to enable highlights and pin buttons in that kingdom. The unlock is saved with your research progress.

## Highlights and pins

Highlights follow pipe bends and mirrored ports. Where straight pipes overlap, their colors separate into narrow parallel lanes so every line remains visible. The original pipes and saved bends stay in place.

Click the **tiny pin at a node's top-left** to keep its paths highlighted after moving away. Bronze pins gain a cyan rim when pinned; click again to unpin. Multiple pins and hover work together, and shared pipes are drawn once.

Pins last for the loaded kingdom session. Hidden paths disappear when switching tabs and return when visible again. Popups block hover highlights behind them. Dragging suppresses hover selection while pinned paths stay visible.

Eight neon colors repeat when needed. Colors and pins are not saved.

## Requirements

- Kingdom Inc, Windows Mono build.
- BepInEx 5 for Windows x64.
- Foundation 0.5.0 or later, installed separately.

## Install or update

Close the game and extract the player ZIP into the folder containing `KingdomInc.exe`. The plugin belongs at `BepInEx/plugins/PathFinder/PathFinder.dll`.

Launch the game, open **Tech Tree → Mods**, and research **Path Finder**. Hover a connected node or click its pin. To update, close the game and replace the DLL.

## Settings

Settings appear in `BepInEx/config/urakka.kingdominc.pathfinder.cfg` after launching:

| Setting | Default | Meaning |
| --- | --- | --- |
| Highlights / Enabled | true | Enable hover and pinned highlights after research |
| Highlights / Width | 5 | Solid line width in world units, from 2 to 12 |

## Remove

Close the game and remove `BepInEx/plugins/PathFinder`. Pins and highlights add no placed nodes or items to your kingdom.

## Screenshots

![Direct paths highlighted](Assets/path-finder-hover.png)

![Pin states: idle, hover and pinned](Assets/path-finder-pin-styles.png)

## Credits and support

Author: Urakka. Pin artwork is drawn procedurally; no game assets are bundled. Source license not selected.

For bug reports, include `BepInEx/LogOutput.log`, your game and Path Finder versions, and the affected node types.
