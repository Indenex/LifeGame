#!/usr/bin/env bash
# survival-24: a character, not a login, owns things. In the server module the caller is
# Me(ctx); raw ctx.Sender is the login and belongs only in Characters.cs (and the connect
# and disconnect reducers in Reducers.cs). Fails if it shows up anywhere else.
set -euo pipefail
cd "$(dirname "$0")/.."
bad=$(grep -n 'ctx\.Sender\b' server/spacetimedb/*.cs | grep -v '^server/spacetimedb/Characters.cs' | grep -v '^server/spacetimedb/Reducers.cs' || true)
if [ -n "$bad" ]; then
  echo "Raw ctx.Sender outside Characters.cs / Reducers.cs (use Me(ctx)):"; echo "$bad"; exit 1
fi
echo "ok: no raw ctx.Sender"
