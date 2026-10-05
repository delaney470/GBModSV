# Development context

`0.9.0-beta.1` targets the Xbox PC Gang Beasts 1.28 build under MelonLoader 0.7.3.

The production mod supports local couch co-op only. Each local Actor receives an independent `AbilityState`, `PlayerClock` and `MotionEcho`. Controller ownership is resolved from the game’s actual paired input devices; do not substitute controller order or player IDs as ownership evidence. The shared `WorldDilation` remains active while at least one player session is active and is restored only after the last session ends.

Keep the safety boundaries intact: no pose extrapolation, no joint-drive edits, no online support, and no test-helper DLL in public releases. The production project excludes `tests/**/*.cs`. Runtime diagnostics live under `tests/runtime` and should only be installed for attended local testing.

Before publishing an update, update the version in both the project file and `MelonInfo`, run the structural/timer checks, run representative local gameplay tests, build the release ZIP through `package.ps1`, and record the new validation result.

Thunderstore packaging lives in `thunderstore/` and is built by `package-thunderstore.ps1`. Its public package version must use three numeric components, so this beta is `0.9.0` there while the GitHub release remains `0.9.0-beta.1`. The manifest depends on `LavaGang-MelonLoader-0.7.3`; validate the archive contents before uploading.
