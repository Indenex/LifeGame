# release-1: Automatic releases and a one-button launcher
Lane: Release & Launcher (D-131)
Owner: Release & Launcher thread (session_015Je3JR5vZrzA7TwQYBSPgM)
Status: in_progress
Branch: claude/release-1
PR: (not yet)

## Goal
Justin opens one launcher on Linux or Windows, presses one button, and both the game and his world's server are on the newest green `main` build, with no setup work and no wipe. Concretely:
1. Every green merge to `main` publishes a versioned GitHub Release (`build-<N>`) with the Linux client, the Windows client and the server module (`StdbModule.wasm`) attached, plus `release.json` saying whether that merge wipes the world (D-130 `world-wipe` label).
2. A small launcher (`launcher/`, exported for Linux and Windows) checks the newest release, downloads and swaps the client with a progress bar, makes sure the local SpacetimeDB server is running, publishes the new module over the existing world with `--delete-data=never` using the `spacetime` login already on his machine, and then Plays. It refuses any world-wipe release unless Justin confirms.

## Findings: how the server is hosted (step 1)
- **Justin's world is a local SpacetimeDB server on his own computer, not Maincloud.** Evidence:
  - D-007: "Playtests run on Justin's local server; no hosting spend."
  - `PLAYING.md` (his update guide): `spacetime start`, then `spacetime publish lifegame --server local --bin-path StdbModule.wasm --delete-data=never --yes=migrate,break-clients,skip-login`.
  - The client (`client/scripts/Main.cs` Connect) defaults to `http://127.0.0.1:3000`, database `lifegame`; no Maincloud URL anywhere in the repo. `--server=` / `LIFEGAME_SERVER` override it.
  - `tools/upgrade-check.sh` and `bugfix-3` model exactly that: publish over the old world without `--delete-data`.
  - `server/CLAUDE.md` mentions Maincloud only as the CLI's generic default; no ticket or decision publishes there.
- So Justin's memory ("I push the server file to SpacetimeDB's hosted service") is the `spacetime publish` step, but `--server local`: it goes to the server running on his own machine. If he ever ran plain `spacetime publish lifegame` without `--server local`, that would have created a separate, unused Maincloud database; worth asking the PM to check with him.
- Today the build is downloaded by hand from the Actions tab (artifacts `LifeGame-Server`, `LifeGame-Linux`, `LifeGame-Windows`); nothing is published as a release. The repo is public, so releases download with no GitHub login.
- D-130's `world-wipe` label is decided but not yet in `build.yml` (Integration's to add).
- Consequence for step 4: CI cannot reach a server on Justin's home machine, so a "fully automatic from CI" publish needs either Maincloud hosting or something running on his machine (the launcher). See open questions.

## Progress log
- 2026-10-16 19:00: started on `claude/release-1` from main 45f8913. Findings above.

## Open questions / blockers
- (to PM) Fully automatic server updates: options being drafted.

## Next step
Write `.github/workflows/release.yml` (workflow_run after Build on main) and send it to Integration for review.
