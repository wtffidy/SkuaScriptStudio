#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION="$ROOT/SkuaScriptStudio.sln"
PROJECT="$ROOT/src/SkuaScriptStudio/SkuaScriptStudio.csproj"
OUTPUT="$ROOT/build/local"
now_ms() { perl -MTime::HiRes=time -e 'printf "%.0f", time() * 1000'; }
format_ms() { local ms="$1"; printf "%d.%03ds" "$((ms / 1000))" "$((ms % 1000))"; }
TOTAL_START="$(now_ms)"
JOBS="$(getconf _NPROCESSORS_ONLN 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 8)"
(( JOBS > 16 )) && JOBS=16
(( JOBS < 4 )) && JOBS=4

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=0

command -v dotnet >/dev/null 2>&1 || { echo ".NET 10 SDK is required." >&2; exit 1; }
SDK_VERSION="$(dotnet --version)"
SDK_MAJOR="${SDK_VERSION%%.*}"
(( SDK_MAJOR >= 10 )) || { echo ".NET 10 SDK is required. Found $SDK_VERSION." >&2; exit 1; }

if [[ "$(uname -s)" == "Linux" ]]; then
    case "$(uname -m)" in
        x86_64) RID="linux-x64" ;;
        aarch64|arm64) RID="linux-arm64" ;;
        *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
    esac
else
    case "$(uname -m)" in
        x86_64) RID="osx-x64" ;;
        arm64|aarch64) RID="osx-arm64" ;;
        *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
    esac
fi

TEST=0
RUN_APP=0
NO_PULL=0
for ARG in "$@"; do
    case "$ARG" in
        --test) TEST=1 ;;
        --run) RUN_APP=1 ;;
        --no-pull) NO_PULL=1 ;;
        *) echo "Unknown argument: $ARG" >&2; exit 1 ;;
    esac
done

if [[ "$NO_PULL" -eq 0 && -d "$ROOT/.git" ]]; then
    command -v git >/dev/null 2>&1 || { echo "Git is required to update the repository." >&2; exit 1; }
    [[ -z "$(git -C "$ROOT" status --porcelain)" ]] || { echo "Local changes detected. Commit or stash them before building."; exit 1; }
    echo "Pulling latest main..."
    STAGE_START="$(now_ms)"
    git -C "$ROOT" pull --ff-only origin main
    echo "  Git pull     $(format_ms "$(( $(now_ms) - STAGE_START ))")"
fi

echo "Building $RID using $JOBS MSBuild workers (incremental)..."
if [[ "$TEST" -eq 1 ]]; then
    STAGE_START="$(now_ms)"
    dotnet test "$SOLUTION" --configuration Release --no-restore --verbosity quiet -m:"$JOBS"
    echo "  Tests        $(format_ms "$(( $(now_ms) - STAGE_START ))")"
fi

STAGE_START="$(now_ms)"
dotnet restore "$PROJECT" --runtime "$RID" --verbosity quiet
echo "  Restore      $(format_ms "$(( $(now_ms) - STAGE_START ))")"

STAGE_START="$(now_ms)"
dotnet publish "$PROJECT" --configuration Release --runtime "$RID" --self-contained true --no-restore --verbosity quiet -m:"$JOBS" -p:BuildInParallel=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=false -o "$OUTPUT"
echo "  Publish      $(format_ms "$(( $(now_ms) - STAGE_START ))")"

echo "  TOTAL        $(format_ms "$(( $(now_ms) - TOTAL_START ))")"
echo "Build complete: $OUTPUT"
if [[ "$RUN_APP" -eq 1 ]]; then
    if [[ "$RID" == win-* ]]; then "$OUTPUT/SkuaScriptStudio.exe"; else "$OUTPUT/SkuaScriptStudio"; fi
fi
