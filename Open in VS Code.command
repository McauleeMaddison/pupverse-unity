#!/bin/zsh
set -eu
PROJECT="$(cd "$(dirname "$0")" && pwd)"
EDITOR="/Applications/Visual Studio Code.app"
open -a "$EDITOR" "$PROJECT/Pupverse.code-workspace"
