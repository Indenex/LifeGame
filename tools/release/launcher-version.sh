#!/usr/bin/env bash
# Prints the launcher's version: a short hash of everything tracked under launcher/.
# CI stamps it into the launcher (launcher/version.txt) and into release.json, and the
# launcher updates itself only when the two differ, so an unchanged launcher is never
# downloaded again.
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
git -C "$root" ls-files -s launcher | grep -v 'launcher/version.txt' | sha256sum | cut -c1-12
