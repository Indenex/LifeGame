#!/usr/bin/env bash
# Merge-readiness survey (Integration lane). For each open PR branch given, reports whether it
# merges cleanly into origin/main, then which pairs conflict once the first of the pair has
# merged (modelled as a two-parent merge, so a branch that already contains the other is not
# a false positive). Read-only: nothing is checked out or pushed.
#
# Usage: tools/integration/survey.sh <branch>...   (branch names as on origin, no "origin/")
# Get the open PR branches from GitHub (PR list, head.ref) and pass them all.
set -uo pipefail
cd "$(dirname "$0")/../.."
git fetch -q origin main "$@"
echo "main is $(git rev-parse --short origin/main)"
for b in "$@"; do
  if out=$(git merge-tree --write-tree --name-only origin/main "origin/$b" 2>&1); then echo "$b: clean"
  else echo "$b: CONFLICT with main: $(echo "$out" | sed -n '2,/^$/p' | tr '\n' ' ')"; fi
done
echo "== pairs that conflict after the first merges"
for a in "$@"; do for c in "$@"; do
  [[ "$a" < "$c" ]] || continue
  ta=$(git merge-tree --write-tree origin/main "origin/$a" 2>/dev/null) || continue
  git merge-tree --write-tree origin/main "origin/$c" >/dev/null 2>&1 || continue
  ma=$(git commit-tree "$(echo "$ta" | head -1)" -p origin/main -p "origin/$a" -m survey)
  out=$(git merge-tree --write-tree --name-only "$ma" "origin/$c" 2>&1) || echo "$a x $c: $(echo "$out" | sed -n '2,/^$/p' | tr '\n' ' ')"
done; done
