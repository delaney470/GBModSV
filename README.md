# Gang Beasts Sandevistan

An experimental local couch co-op Sandevistan mod for the Xbox PC release of Gang Beasts 1.28.

Each local player can activate their own Sandevistan with **R3**. Their three-second active window and six-second cooldown start from their own button press. While one or more players are active, the rest of the world runs at 20% speed. Active Beasts are compensated to move and act in real time, with cyan and red motion trails.

## Beta status

`0.9.0-beta.1` is a local/offline beta, made for Gang Beasts 1.28 on Xbox PC with MelonLoader 0.7.3. Do not use it online. It actively refuses to run when it detects a remote human player.

The beta has been exercised locally and its timer, input and safety checks pass. It still needs broader couch co-op testing across maps, controllers, grabs, knock-outs, recovery, collisions and game updates. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Install

1. Close Gang Beasts.
2. Install MelonLoader 0.7.3 for the Xbox PC release of Gang Beasts 1.28.
3. Download `GangBeastsSandevistan-0.9.0-beta.1.zip` from Releases.
4. Extract `Mods/GangBeastsSandevistan.dll` into your Gang Beasts `Mods` folder.
5. Start a **local/offline** match.

Remove older copies of this mod before installing an update. The release ZIP deliberately contains only the production DLL; it does not include any automated-test helpers.

## Controls

- **R3** — activate or cancel the Sandevistan for the Beast paired to that controller.
- **F7** — cycle the keyboard fallback target.
- **F8** — activate or cancel the keyboard fallback target.

L3 is not required.

## Reporting a beta issue

Open a GitHub issue with the game build, map, local-player count, controllers used, exact steps, expected and actual result. Attach `MelonLoader/Latest.log` after removing any personal information it may contain.

## Development

```powershell
dotnet restore --locked-mode
dotnet build -c Release --no-restore
dotnet run --project tests/Checks.csproj -c Release -- .
./package.ps1
```

`tests/runtime` contains local diagnostic automation. It is excluded from the production DLL and must never be installed for ordinary play.
