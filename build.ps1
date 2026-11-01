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

$RuntimeInfo = [System.Runtime.InteropServices.RuntimeInformation]
$Architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
$IsWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$IsMacHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
$IsLinuxHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)

$RuntimeIdentifier = switch ($Architecture) {
    "X64" {
        if ($IsWindowsHost) { "win-x64" }
        elseif ($IsMacHost) { "osx-x64" }
        elseif ($IsLinuxHost) { "linux-x64" }
        else { throw "Unsupported operating system." }
    }
    "Arm64" {
        if ($IsMacHost) { "osx-arm64" }
        elseif ($IsLinuxHost) { "linux-arm64" }
        else { throw "ARM64 publishing is not configured for this operating system." }
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
    $Executable = if ($IsWindowsHost) { Join-Path $Output "SkuaScriptStudio.exe" } else { Join-Path $Output "SkuaScriptStudio" }
    if (-not (Test-Path $Executable)) { throw "Executable not found: $Executable" }
    & $Executable
}
