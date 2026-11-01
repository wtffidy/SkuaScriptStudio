param(
    [switch]$SkipTests,
    [switch]$Run
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Solution = Join-Path $Root "SkuaScriptStudio.sln"
$Project = Join-Path $Root "src/SkuaScriptStudio/SkuaScriptStudio.csproj"
$Output = Join-Path $Root "artifacts/local"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}

$SdkVersion = (& dotnet --version).Trim()
if ([version]($SdkVersion.Split('-')[0]) -lt [version]"10.0") {
    throw "Skua Script Studio requires the .NET 10 SDK. Found $SdkVersion."
}

$Architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
$RuntimeIdentifier = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()) {
    "X64" {
        if ($IsWindows) { "win-x64" }
        elseif ($IsMacOS) { "osx-x64" }
        else { "linux-x64" }
    }
    "Arm64" {
        if ($IsMacOS) { "osx-arm64" }
        elseif ($IsLinux) { "linux-arm64" }
        else { throw "Windows ARM64 publishing is not configured." }
    }
    default { throw "Unsupported architecture: $Architecture" }
}

Write-Host "Building Skua Script Studio for $RuntimeIdentifier..." -ForegroundColor Cyan

if (-not $SkipTests) {
    dotnet test $Solution --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
}

if (Test-Path $Output) {
    Remove-Item $Output -Recurse -Force
}

dotnet publish $Project --configuration Release --runtime $RuntimeIdentifier --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o $Output
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }

Write-Host "Build complete: $Output" -ForegroundColor Green

if ($Run) {
    $Executable = if ($IsWindows) { Join-Path $Output "SkuaScriptStudio.exe" } else { Join-Path $Output "SkuaScriptStudio" }
    if (-not (Test-Path $Executable)) { throw "Executable not found: $Executable" }
    & $Executable
}
