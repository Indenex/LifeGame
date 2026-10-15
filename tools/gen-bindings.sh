#!/usr/bin/env bash
# Generates the client bindings in client/module_bindings from the server module.
# They are not committed: run this after cloning and whenever the server module
# changes, before building the client or the tools.
set -euo pipefail
cd "$(dirname "$0")/../server"
cli=$(command -v spacetimedb-cli || command -v spacetime)
rm -rf ../client/module_bindings
"$cli" generate --lang csharp --out-dir ../client/module_bindings --module-path ./spacetimedb
