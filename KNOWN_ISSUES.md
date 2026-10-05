# Known issues and beta limits

- Local/offline play only. Online and remote-human sessions are intentionally refused.
- This beta targets Xbox PC Gang Beasts 1.28 and MelonLoader 0.7.3 only.
- Independent controller routing is designed for controllers paired by the game. Hot-plugging, unsupported controller layouts and keyboard-only multi-player need more coverage.
- Couch co-op needs wider gameplay testing: all maps, grabs, knock-outs, recovery, collisions, water, moving platforms and more than two local players.
- The world clock slows known scenery and vehicles, but map scripts using unscaled time may not follow the 20% effect.
- The current action/physics compensation has guard rails but is still experimental. Report unexpected launches, lingering slow motion or a player retaining speed after expiry.

The mod never uses pose extrapolation or joint-drive overrides; those earlier experiments were removed after unstable results.
