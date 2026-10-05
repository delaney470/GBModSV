# Gang Beasts Sandevistan — Local Couch Co-op Beta

Each local player can activate their own Sandevistan with **R3**. Every Beast has an independent three-second active window and six-second cooldown. Activations can overlap: the world remains at 20% speed while any Beast is active, while active Beasts receive scoped movement and physics compensation with motion trails.

## Compatibility and safety

- **Xbox PC Gang Beasts 1.28** only.
- **Local/offline** matches only. The mod rejects sessions with remote human players.
- This is a beta. Test it with the standard Gang Beasts maps and report issues with reproducible steps.

## Installation

Install this package through Thunderstore Mod Manager or r2modman. It declares and installs **MelonLoader 0.7.3** automatically.

If you install manually, put `GangBeastsSandevistan.dll` in the game’s `Mods` directory after installing MelonLoader 0.7.3. Remove older copies of this mod before updating.

## Controls

- **R3**: activate or cancel the Sandevistan for the Beast paired to that controller.
- **F7**: select the keyboard fallback target.
- **F8**: activate or cancel the keyboard fallback target.

L3 is not required.

## Beta limits

The mod passed structural, input, timer and safety checks. Wider couch co-op gameplay coverage is still needed, particularly for controllers, grabs, knock-outs, recovery, collisions, water, moving platforms and three or more local players.

The world clock slows known scenery and vehicles. Map scripts that use unscaled time may not follow the 20% effect. If a player keeps unexpected speed or slow motion does not clear, please report the map, player count, controllers, steps, expected result and actual result at the [GitHub issue tracker](https://github.com/delaney470/GBModSV/issues).

## Source and checksums

Source, standalone release ZIPs and SHA-256 checksums are available from the [GitHub releases page](https://github.com/delaney470/GBModSV/releases).
