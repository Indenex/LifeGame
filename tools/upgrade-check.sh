#!/usr/bin/env bash
# Upgrade check: publishes the base branch's module to a local server, seeds it with a
# short load test, then publishes this checkout's module over it without --delete-data,
# the way Justin updates his world. Fails if the new module can't take over the old
# data, which means merging it would need a world wipe.
#
# Usage: tools/upgrade-check.sh <base-ref> [uri=http://127.0.0.1:3000]
# Needs a running local server and this checkout's module already built.
set -euo pipefail
base=$1
uri=${2:-http://127.0.0.1:3000}
db=upgrade-check
root=$(cd "$(dirname "$0")/.." && pwd)
cli=$(command -v spacetimedb-cli || command -v spacetime)
work=$(mktemp -d)
trap 'git -C "$root" worktree remove --force "$work/base" >/dev/null 2>&1 || true; rm -rf "$work"' EXIT

echo "--- Base world from $base ($(git -C "$root" rev-parse --short "$base"))"
git -C "$root" worktree add --detach "$work/base" "$base" >/dev/null
(cd "$work/base/server" && "$cli" build >/dev/null)
(cd "$work/base/server" && "$cli" publish "$db" --server "$uri" --delete-data=always --yes)
"$work/base/tools/gen-bindings.sh" >/dev/null
(cd "$work/base/tools/LoadTest" && dotnet run -c Release -- 3 3 brotli "$uri" "$db" | tail -2)

echo "--- Publishing this module over it without --delete-data"
if ! (cd "$root/server" && "$cli" publish "$db" --server "$uri" --delete-data=never --yes=break-clients,migrate); then
    echo "::error::This module can't be published over $base's world without --delete-data. Merging it needs a world wipe, which Justin approves through the PM."
    exit 1
fi

echo "--- Playing on the upgraded world"
(cd "$root/tools/LoadTest" && dotnet run -c Release -- 3 3 brotli "$uri" "$db" | tail -2)
echo "Upgrade check passed: $base's world carries over"
