# Build scripts for SchackBot UCI adapter

Location:
- Bash:   `SchackBot/scripts/build.sh`
- PowerShell: `SchackBot/scripts/build-win.ps1`

## Bash usage (macOS / Linux / WSL)
Run from repository root or `SchackBot/scripts` (script locates project automatically):

build a single runtime identifier (RID)
./SchackBot/scripts/build.sh win-x64

build and zip the output
./SchackBot/scripts/build.sh linux-x64 zip

build multiple targets (all: win, linux, osx x64 and arm64)
NOTE: building osx RIDs on Linux runners may fail — run 'all' locally on a Mac or use macOS runners.
./SchackBot/scripts/build.sh all zip


Outputs are placed under `SchackBot/publish/<rid>/...`. Example: `SchackBot/publish/win-x64/SchackBot.UciAdapter.exe`

## PowerShell usage (Windows)
Open PowerShell in `SchackBot/scripts` and run:

```powershell
.\build-win.ps1 -Runtime win-x64
.\build-win.ps1 -Runtime win-x64 -Zip
