# Changelog

## 0.9.0-beta.1 — 2026-10-05

- Added independent Sandevistan sessions for each local Beast.
- Each player has a separate three-second active timer and six-second cooldown.
- R3 resolves through the game’s controller pairing, so it affects only that controller’s Beast.
- The world remains at 20% speed while one or more local players are active, then restores after the final active player ends.
- Active players retain their own motion trails and scoped locomotion/physics compensation.
- Added transactional physics restoration so a partial activation failure cannot rescale untouched bodies.
- Corrected scoped angular-limit and relative-force handling used by native movement and jumps.

## Earlier experimental builds

Earlier builds were internal experiments and are superseded by this beta.
