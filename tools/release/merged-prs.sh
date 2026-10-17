#!/usr/bin/env bash
# Lists the PRs merged since the previous release, as a JSON array of {number, title, wipe, note, skip}.
# note (release-9) is what players read: the PR body's "Player note: <one sentence>" line, else the
# title without its ticket prefix ("crafting-1: add fire" -> "Add fire"). skip is true for PRs
# labelled or titled scribe / docs / ticket-only / ci-only, or whose Player note says "none", which the
# patch notes leave out.
# Build's concurrency (#107) lets a newer merge replace a queued Build, so one release can
# cover several PRs; a world-wipe label (D-130) on any of them marks the release.
#
# Usage: tools/release/merged-prs.sh <owner/repo> <head-sha>   (needs gh with a token)
set -euo pipefail
repo=$1 head=$2
prev=$(gh api "repos/$repo/releases?per_page=20" --jq '[.[] | select(.draft | not) | .tag_name | select(startswith("build-"))][0] // empty')
if [ -n "$prev" ]; then
  base=$(gh api "repos/$repo/commits/$prev" --jq .sha)
  commits=$(gh api "repos/$repo/compare/$base...$head" --jq '.commits[].sha')
else
  commits=$head
fi
for c in $commits; do
  gh api "repos/$repo/commits/$c/pulls" --jq '.[] | select(.merged_at != null)' | jq -c '
    ([.labels[].name | ascii_downcase] | map(select(. == "scribe" or . == "docs" or . == "ticket-only" or . == "ci-only")) | length > 0) as $labelled
    | (.title | test("^(scribe|docs?|ticket|ci)\\b[:( -]"; "i")) as $titled
    | ([(.body // "") | capture("(?i)(^|\\n)[ \\t]*player note:[ \\t]*(?<n>[^\\r\\n]+)")? | .n | gsub("^\\s+|\\s+$"; "")] | first // "" | if startswith("<") then "" else . end) as $said
    | (.title | sub("^[A-Za-z0-9_-]+: *"; "") | (.[0:1] | ascii_upcase) + .[1:]) as $stripped
    | {number, title, wipe: ([.labels[].name] | index("world-wipe") != null),
       note: (if $said != "" then $said else $stripped end),
       skip: ($labelled or $titled or ($said | ascii_downcase | test("^none[.!]?$")))}'
done | jq -s 'unique_by(.number)'
