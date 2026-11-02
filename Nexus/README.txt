Path Finder 1.0.2 by Urakka

Hover any node to highlight pipes to its immediate parents and immediate children.
Each pipe has ONE solid bright neon color from a rainbow palette, with a glow.
Overlapping straight sections use parallel colored lanes so each line stays visible.
Only highlights move; native pipe positions and saved bends are unchanged.

Click the TINY pin at a node's top-left to keep its direct paths highlighted.
Pins have bronze bevels, dark enamel, a tilted gold tack and a cyan locked-state rim.
Click again to unpin. Cyan means pinned. Multiple pins and hover work together.
Pins are session-only; saves are unchanged. Indirect ancestors/descendants are excluded.

REQUIREMENTS
Kingdom Inc Unity Mono build and BepInEx 5.
Tested: Release v1.0.10 / Unity 6000.3.27f2 / BepInEx 5.4.23.6.
No other mod required. Manual installation supported; Vortex untested.

INSTALL / UPDATE
Close the game. Extract this ZIP into the game directory.
Expected file: BepInEx/plugins/PathFinder/PathFinder.dll
Start the game and hover or pin a connected node.
For updates, close the game and replace the DLL.

REMOVE
Close the game and remove BepInEx/plugins/PathFinder.
No save cleanup is needed; the mod adds no saved nodes or data.

SETTINGS
BepInEx/config/urakka.kingdominc.pathfinder.cfg, created on first normal launch.
Highlights / Enabled: true. Highlights / Width: 5 (2-12 world units).
Eight colors are reused when more than eight lines are visible.

CREDITS / SUPPORT
Author: Urakka. Pin art is procedural; no bundled game assets.
Diagnostic workflow adapted from the existing PartyNode/LedgerMod projects.
Source licensing not selected. Logs: BepInEx/LogOutput.log.
