# Path Finder

**Current release: 1.1.1 — for Kingdom Inc game version 1.0.14.**

[GitHub releases](https://github.com/KingdomIncModding/KingdomInc_PathFinder/releases) · [Nexus Mods](https://www.nexusmods.com/kingdominc/mods/8) · Version 1.1.1

See a node's immediate input and output paths at a glance. Hover a node to highlight its direct connections in bright neon colors, with one solid color per line and a soft glow.

Research **Path Finder** for **100 gold** under **Tech Tree → Mods** to enable highlights and pin buttons in that kingdom. The unlock is saved with your research progress.

## Features

- **Hover Path Tracing:** Hover any workshop node to immediately highlight its direct input and output pipes.
- **Solid Neon Colors:** Distinct bright neon colors per line with an outer glow, cycling across 8 unique color channels.
- **Persistent Pins:** Click the tiny pin at a node's top-left to keep its paths highlighted after moving away. Bronze pins gain a cyan rim when pinned; click again to unpin.
- **Multi-Pin & Hover Integration:** Pin multiple nodes simultaneously. Shared pipes are drawn once without visual clutter.
- **Lane Separation:** Where straight pipes overlap, colors separate into narrow parallel lanes so every line remains visible.
- **Dynamic Routing:** Highlights accurately follow pipe bends, reroutes, and mirrored ports.
- **Configurable Width:** Customize line thickness from 2 to 12 world units via config file.

## Requirements

- Kingdom Inc (Windows x64 Mono)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) (5.4.23.6 or newer)
- [Foundation](https://www.nexusmods.com/kingdominc/mods/18) (0.14.0 or newer)

## Installation

1. Install BepInEx 5 for Kingdom Inc.
2. Install Foundation (0.14.0 or newer).
3. Extract the release ZIP into your Kingdom Inc game directory.
4. Verify the file exists at `BepInEx/plugins/PathFinder/PathFinder.dll`.
5. Launch the game, open **Tech Tree → Mods**, and research **Path Finder**!

## Usage and Settings

- **Unlocking:** Open **Tech Tree → Mods** and research **Path Finder** (100 gold).
- **Pinning:** Click the small bronze pin icon at the top-left of any placed workshop node to lock its path highlights. Click again to unpin.
- **Settings:** Configuration options appear in `BepInEx/config/urakka.kingdominc.pathfinder.cfg`:
  - `Highlights / Enabled` (default `true`): Enable or disable hover and pinned highlights after research.
  - `Highlights / Width` (default `5`): Solid line width in world units (range 2.0 to 12.0).

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| WorldFlowElement.SetElement | Foundation hook (postfix) | Attaches path tracing pin component to workshop nodes when elements are instantiated or refreshed. |

## Removal

Close the game and delete `BepInEx/plugins/PathFinder/`. Pins and highlights add no placed nodes or custom items to your kingdom saves.

## Credits and Source

Created by **Urakka** (rockyamyx). [Source code on GitHub](https://github.com/KingdomIncModding/KingdomInc_PathFinder). Game artwork belongs to its respective owners.
