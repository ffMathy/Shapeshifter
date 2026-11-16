#!/usr/bin/env bash
# Prints the version tag (such as v7.1.0) that a merge to master releases.
#
# It is the <Version> in Directory.Build.props when that is newer than every released v* tag, so raising it releases a
# new minor or major version; otherwise the newest released tag with its patch number increased.
# Needs the repository's tags (git fetch --tags).
set -euo pipefail
cd "$(dirname "$0")/.."

declared=$(sed -n 's:.*<Version>\([0-9]*\.[0-9]*\.[0-9]*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)
if [[ -z "$declared" ]]; then
  echo "No <Version>X.Y.Z</Version> in Directory.Build.props" >&2
  exit 1
fi

# Released versions only: vX.Y.Z without a prerelease suffix.
latest=$(git tag --list 'v*' | sed -n 's/^v\([0-9]*\.[0-9]*\.[0-9]*\)$/\1/p' | sort -V | tail -n 1)

if [[ -z "$latest" ]] || [[ "$(printf '%s\n%s\n' "$latest" "$declared" | sort -V | tail -n 1)" == "$declared" && "$declared" != "$latest" ]]; then
  echo "v$declared"
else
  IFS=. read -r major minor patch <<< "$latest"
  echo "v$major.$minor.$((patch + 1))"
fi
