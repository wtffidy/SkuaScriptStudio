#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION="$ROOT/SkuaScriptStudio.sln"
PROJECT="$ROOT/src/SkuaScriptStudio/SkuaScriptStudio.csproj"
OUTPUT="$ROOT/artifacts/local"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "The .NET 10 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0" >&2
    exit 1
fi

SDK_VERSION="$(dotnet --version)"
SDK_MAJOR="${SDK_VERSION%%.*}"
if [[ "$SDK_MAJOR" -lt 10 ]]; then
    echo "Skua Script Studio requires the .NET 10 SDK. Found $SDK_VERSION." >&2
    exit 1
fi

ARCH="$(uname -m)"
case "$(uname -s):$ARCH" in
    Linux:x86_64) RID="linux-x64" ;;
    Linux:aarch64|Linux:arm64) RID="linux-arm64" ;;
    Darwin:x86_64) RID="osx-x64" ;;
    Darwin:arm64|Darwin:aarch64) RID="osx-arm64" ;;
    *) echo "Unsupported platform: $(uname -s) $ARCH" >&2; exit 1 ;;
esac

SKIP_TESTS=0
RUN_APP=0
for ARG in "$@"; do
    case "$ARG" in
        --skip-tests) SKIP_TESTS=1 ;;
        --run) RUN_APP=1 ;;
        *) echo "Unknown argument: $ARG" >&2; exit 1 ;;
    esac
done

echo "Building Skua Script Studio for $RID..."
if [[ "$SKIP_TESTS" -eq 0 ]]; then
    dotnet test "$SOLUTION" --configuration Release
fi

rm -rf "$OUTPUT"
dotnet publish "$PROJECT" --configuration Release --runtime "$RID" --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o "$OUTPUT"

echo "Build complete: $OUTPUT"
if [[ "$RUN_APP" -eq 1 ]]; then
    "$OUTPUT/SkuaScriptStudio"
fi
