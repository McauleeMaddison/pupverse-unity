#!/bin/zsh
set -eu
PROJECT="$(cd "$(dirname "$0")" && pwd)"
EDITOR="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app"
open -a "$EDITOR" --args -projectPath "$PROJECT"
