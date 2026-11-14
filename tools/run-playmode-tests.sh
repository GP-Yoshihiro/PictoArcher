#!/usr/bin/env bash
# エディタを閉じてから実行する。結果は Logs/playmode-results.xml
set -euo pipefail
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" \
  -runTests -testPlatform PlayMode \
  -testResults "$PROJECT/Logs/playmode-results.xml" \
  -logFile "$PROJECT/Logs/playmode.log"
