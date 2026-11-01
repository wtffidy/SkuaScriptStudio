param(
    [switch]$Test,
    [switch]$Run,
    [switch]$NoPull
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Solution = Join-Path $Root "SkuaScriptStudio.sln"
$Project = Join-Path $Root "src/SkuaScriptStudio/SkuaScriptStudio.csproj"
$Output = Join-Path $Root "artifacts/local"
$Jobs = [Math]::Min(16, [Math]::Max(4, [Environment]::ProcessorCount))
$TotalTimer = [System.Diagnostics.Stopwatch]::StartNew()
function Format-Duration([TimeSpan]$Duration) { return ("{0:N3}s" -f $Duration.TotalSeconds) }

function Add-Timing([string]$Name, [System.Diagnostics.Stopwatch]$Timer) {
    $Timer.Stop()
    $Timings.Add([pscustomobject]@{ Name = $Name; Duration = $Timer.Elapsed })
    Write-Host ("  {0,-12} {1}" -f $Name, (Format-Duration $Timer.Elapsed)) -ForegroundColor DarkGray
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"
$env:MSBUILDDISABLENODEREUSE = "0"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}

$SdkVersion = (& dotnet --version).Trim()
if ([version]($SdkVersion.Split('-')[0]) -lt [version]"10.0") {
    throw "Skua Script Studio requires the .NET 10 SDK. Found $SdkVersion."
}

if (-not $NoPull -and (Test-Path (Join-Path $Root ".git"))) {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "Git is required to update the repository." }
    $Changes = & git -C $Root status --porcelain
    if ($LASTEXITCODE -ne 0) { throw "Unable to check Git working tree." }
    if ($Changes) { throw "Local changes detected. Commit or stash them before building so the update cannot overwrite your work." }
    Write-Host "Pulling latest main..." -ForegroundColor Cyan
    $StageTimer = [System.Diagnostics.Stopwatch]::StartNew()
    & git -C $Root pull --ff-only origin main
    if ($LASTEXITCODE -ne 0) { throw "Git pull failed. Resolve the repository state before building." }
    Add-Timing "Git pull" $StageTimer
}

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

Write-Host "Building $RuntimeIdentifier using $Jobs MSBuild workers..." -ForegroundColor Cyan
Write-Host "Incremental build enabled; NuGet and intermediate outputs are reused." -ForegroundColor DarkGray

if ($Test) {
    $StageTimer = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet test $Solution --configuration Release --no-restore --verbosity quiet -m:$Jobs
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
    Add-Timing "Tests" $StageTimer
}

dotnet restore $Project --runtime $RuntimeIdentifier --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }

$StageTimer = [System.Diagnostics.Stopwatch]::StartNew()\ndotnet publish $Project --configuration Release --runtime $RuntimeIdentifier --self-contained true --no-restore --verbosity quiet -m:$Jobs -p:BuildInParallel=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=false -o $Output
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }
Add-Timing "Publish" $StageTimer
$TotalTimer.Stop()
Write-Host ("  TOTAL        {0}" -f (Format-Duration $TotalTimer.Elapsed)) -ForegroundColor Green
Write-Host "Build complete: $Output" -ForegroundColor Green

if ($Run) {
    $Executable = if ($IsWindowsHost) { Join-Path $Output "SkuaScriptStudio.exe" } else { Join-Path $Output "SkuaScriptStudio" }
    if (-not (Test-Path $Executable)) { throw "Executable not found: $Executable" }
    & $Executable
}
