#!/usr/bin/env bash
# Rebuilds unity/Assets/LifeGame/Plugins/LifeGame.Shared.dll from shared/.
set -euo pipefail
cd "$(dirname "$0")"
export PATH="$HOME/.dotnet:$PATH"
dotnet build -c Release -o out >/dev/null
mkdir -p ../Assets/LifeGame/Plugins
cp out/LifeGame.Shared.dll ../Assets/LifeGame/Plugins/
echo "built ../Assets/LifeGame/Plugins/LifeGame.Shared.dll"
