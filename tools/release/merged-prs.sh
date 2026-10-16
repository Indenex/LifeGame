#!/usr/bin/env bash
# Lists the PRs merged since the previous release, as a JSON array of {number, title, wipe}.
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
  gh api "repos/$repo/commits/$c/pulls" \
    --jq '.[] | select(.merged_at != null) | {number, title, wipe: ([.labels[].name] | index("world-wipe") != null)}'
done | jq -s 'unique_by(.number)'
