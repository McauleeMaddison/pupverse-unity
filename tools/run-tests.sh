#!/bin/zsh
set -eu
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
EDITOR="${UNITY_EDITOR_PATH:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$PROJECT/TestResults"
"$EDITOR" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode -testResults "$PROJECT/TestResults/editmode.xml" -logFile "$PROJECT/TestResults/editmode.log"
