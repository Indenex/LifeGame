#!/usr/bin/env bash
# Fails unless a Godot export folder carries the .NET assemblies. Godot still writes the .pck and
# the executable when the C# build fails, but without data_LifeGame_*/ (LifeGame.dll, GodotSharp.dll)
# the game dies at start with "Unable to find the .NET assemblies directory" (builds 1092 to 1096).
#
# Usage: tools/ci/check-export.sh <export-dir>...
set -uo pipefail
bad=0
for dir in "$@"; do
  found=0
  for data in "$dir"/data_LifeGame_*/; do
    [ -d "$data" ] || continue
    found=1
    for dll in LifeGame.dll GodotSharp.dll; do
      if [ ! -f "$data$dll" ]; then echo "::error::$data is missing $dll"; bad=1; fi
    done
  done
  if [ "$found" = 0 ]; then echo "::error::$dir has no data_LifeGame_*/ folder: the export has no .NET assemblies"; bad=1; fi
done
[ "$bad" = 0 ] && echo "Export folders carry the .NET assemblies."
exit "$bad"
