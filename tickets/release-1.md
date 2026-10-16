# release-1: Automatic releases and a one-button launcher
Lane: Release & Launcher (D-131)
Owner: Release & Launcher thread (session_015Je3JR5vZrzA7TwQYBSPgM)
Status: in_review
Branch: claude/release-1
PR: #161

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
- 2026-10-16 19:05: sent the PM the findings, the STATUS row, and Q1 to Q3 below.
- 2026-10-16 19:20: launcher (`launcher/`, GDScript, Godot 4.6 mono editor + export templates, same as CI) exports for Linux and Windows. `tools/release/make-release.sh` packs a Build run's artifacts into release assets + `release.json` + notes with the `<!-- world-wipe -->` marker; `tools/release/fake-api.py` serves them like the GitHub releases API.
- 2026-10-16 19:25: end-to-end tests on a local SpacetimeDB 2.12 with main's real artifacts (Build run 472, a5f0428), launcher run with `--auto --no-play --quit`:
  - server down, fresh home: launcher started `spacetime start`, published 472 (`--delete-data=never`), installed the game (exec bit set), state 472/472. Exit 0.
  - set `claim_config.lifetime_seconds` to 77, released 474 (no wipe): world updated in place, 77 kept. Exit 0.
  - wipe release 473 below the world's build 474: ignored, as it should be. World on 472 with wipe 473 ahead: stopped before downloading ("This update wipes the world"), nothing changed. Exit 2.
  - release 475 carrying an older, incompatible module: publish refused, "This build can't update your world without wiping it. Nothing changed", game not swapped, 77 kept. Exit 1.
- 2026-10-16 19:25: writing `.github/workflows/release.yml` was held: it would publish builds publicly from a public repo, so it waits on the PM/Justin (Q1). Draft design below.

- 2026-10-16 19:40: PM relayed Justin: Q1 public GitHub Releases (a), Q2 launcher button (a), Q3 unsure. Wrote `.github/workflows/release.yml` (separate file, so it stays clear of Integration's #148 edits to build.yml). Tags are `build-<N>-<short sha>` per Integration; launcher and fake API parse the number. Re-tested: fresh install to 474 kept the world, a wipe build 476 ahead stopped with exit 2, world value kept.
- 2026-10-16 19:55: per Integration: the wipe flag now comes from every PR merged since the previous release (`tools/release/merged-prs.sh`, REST compare + commits/pulls), since Build concurrency can skip a merge's own release; notes list those PRs; release only for head_branch main. Tested merged-prs.sh read-only against the real repo (115a22e..45f8913 gives #146, #147) and make-release.sh with a wipe PR in the list.
- 2026-10-16 19:40: Q3 check (does a `lifegame` database exist on Maincloud under his login?) not done: probing Maincloud and the CLI login from this sandbox was refused by the session's permission checks, and the launcher can't verify it without touching his login. Told the PM.

- 2026-10-16 20:25: #161 merged by the PM as bd666f5. First release expected from Build 666.
- 2026-10-16 20:20: CI green on 4ad7e48 (Server, Client Linux, Client Windows); Integration approved; sha sent to the PM.
- 2026-10-16 20:10: Render shots (renders/release-1-02ae54a-1..4.png on claude/render-1-013qe8) and PM asks: the wipe question is now a launcher-styled overlay below the title; focus on "Keep my world", so Enter keeps, and Esc keeps; disabled buttons dim instead of brightening. Checked under Xvfb with xdotool: Enter and Esc each left the world on 474 ("Nothing changed"). Added `--press` (clicks the main button once, for screenshots). PM dropped Q3.

## Open questions / blockers
- ANSWERED (Justin): Q1 a, Q2 a. Q3 unsure; see log.
- Q1 (to PM, for Justin, sent 19:05): public GitHub Releases (recommended; no login needed) vs private repo vs Actions artifacts (both need a GitHub token on his PC). Default: build everything, enable nothing until answered.
- Q2 (to PM, for Justin): fully automatic server updates. a) launcher does it on his PC (recommended; free, no setup); b) move the world to Maincloud and CI publishes (needs his SpacetimeDB token as a repo secret, possible cost, effectively a wipe, best with D-129); c) self-hosted runner (setup, unsafe on a public repo). Default a.
- Q3 (to PM): ask Justin whether he ever published without `--server local` (would have made an unused Maincloud database).

## Release workflow design (waiting on Q1; Integration reviews)
- New file `.github/workflows/release.yml`, `on: workflow_run` of Build, `branches: [main]`, only when `conclusion == success` and `event == push`; `concurrency: release`, never cancelled.
- Job `launcher` (matrix Linux/Windows): checkout `head_sha`, setup-dotnet 8 (the mono editor needs it), the same Godot cache key as build.yml, export `launcher/`, fail on any `SCRIPT ERROR` in the export log, upload `LifeGameLauncher-<preset>`.
- Job `release` (`contents: write`): download the Build run's artifacts (`actions/download-artifact` with `run-id` + token) and the launcher artifacts; read the merged PR for `head_sha` (`gh api repos/.../commits/<sha>/pulls`) and its `world-wipe` label; `tools/release/make-release.sh`; `gh release create build-<Build run_number> --target <sha> --latest` with the zips, `StdbModule.wasm`, `release.json`, notes.
- Tag = `build-<Build run number>-<short sha>` (Integration's suggestion, never collides); the launcher compares numbers.

## Next step
When Build 666 (bd666f5) on main finishes, check the Release run and the first build-* release; run the launcher against it; send the PM the link and Justin's steps.
