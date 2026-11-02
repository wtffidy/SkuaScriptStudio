using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkuaScriptStudio.Services;

public sealed class UpdateService
{
    private const string Repository = "wtffidy/SkuaScriptStudio";
    private const string ReleaseApiUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(ReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken);
        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            throw new InvalidOperationException("GitHub returned an invalid release response.");

        string latestVersionText = release.TagName.TrimStart('v', 'V');
        Version? latestVersion = Version.TryParse(latestVersionText, out Version? parsedVersion) ? parsedVersion : null;
        Version currentVersion = GetCurrentVersion();

        return new UpdateCheckResult(
            latestVersion is not null && latestVersion > currentVersion,
            currentVersion.ToString(),
            latestVersionText,
            release.HtmlUrl ?? $"https://github.com/{Repository}/releases/latest",
            release.Assets ?? []);
    }

    public async Task InstallUpdateAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        if (!update.IsUpdateAvailable)
            throw new InvalidOperationException("There is no newer release to install.");

        bool isWindows = OperatingSystem.IsWindows();
        bool isLinux = OperatingSystem.IsLinux();
        if (!isWindows && !isLinux)
            throw new PlatformNotSupportedException("Automatic updates are currently supported on Windows and Linux.");

        string assetName = isWindows ? "SkuaScriptStudio.exe" : "SkuaScriptStudio-linux-x64.tar.gz";
        GitHubReleaseAsset? asset = update.Assets.FirstOrDefault(item =>
            string.Equals(item.Name, assetName, StringComparison.OrdinalIgnoreCase));
        if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            throw new InvalidOperationException($"The release does not contain {assetName}.");

        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException("Unable to locate the running application.");

        string applicationDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("Unable to locate the application directory.");
        string updateDirectory = Path.Combine(Path.GetTempPath(), "SkuaScriptStudio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(updateDirectory);

        string archivePath = Path.Combine(updateDirectory, assetName);
        using (HttpResponseMessage response = await HttpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream destination = File.Create(archivePath);
            await source.CopyToAsync(destination, cancellationToken);
        }

        string scriptPath = Path.Combine(updateDirectory, isWindows ? "update.ps1" : "update.sh");
        string processPath = executablePath;
        int processId = Environment.ProcessId;

        if (isWindows)
            await File.WriteAllTextAsync(scriptPath, CreateWindowsUpdateScript(processId, applicationDirectory, archivePath, updateDirectory, processPath), cancellationToken);
        else
            await File.WriteAllTextAsync(scriptPath, CreateLinuxUpdateScript(processId, applicationDirectory, archivePath, updateDirectory, processPath), cancellationToken);

        ProcessStartInfo startInfo = isWindows
            ? new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"") { CreateNoWindow = true, UseShellExecute = false }
            : new ProcessStartInfo("/bin/bash", $"\"{scriptPath}\"") { CreateNoWindow = true, UseShellExecute = false };

        Process? updateProcess = Process.Start(startInfo);
        if (updateProcess is null)
            throw new InvalidOperationException("Unable to start the update helper.");
    }

    private static string CreateWindowsUpdateScript(int processId, string applicationDirectory, string archivePath, string updateDirectory, string executablePath)
    {
        return $$"""
            $ErrorActionPreference = 'Stop'
            $appProcessId = {{processId}}
            $newExePath = '{{EscapePowerShell(archivePath)}}'
            $exePath = '{{EscapePowerShell(executablePath)}}'
            $workPath = '{{EscapePowerShell(updateDirectory)}}'
            try {
                Wait-Process -Id $appProcessId -ErrorAction SilentlyContinue
                Copy-Item -LiteralPath $newExePath -Destination $exePath -Force
                Start-Process -FilePath $exePath
            }
            catch {
                Add-Content -Path (Join-Path $workPath 'update-error.log') -Value $_
            }
            """;
    }

    private static string CreateLinuxUpdateScript(int processId, string applicationDirectory, string archivePath, string updateDirectory, string executablePath)
    {
        return $$"""
            #!/usr/bin/env bash
            set -e
            while kill -0 {{processId}} 2>/dev/null; do sleep 1; done
            mkdir -p "{{EscapeShell(updateDirectory)}}/extracted"
            tar -xzf "{{EscapeShell(archivePath)}}" -C "{{EscapeShell(updateDirectory)}}/extracted"
            cp -a "{{EscapeShell(updateDirectory)}}/extracted/." "{{EscapeShell(applicationDirectory)}}/"
            chmod +x "{{EscapeShell(executablePath)}}"
            nohup "{{EscapeShell(executablePath)}}" >/dev/null 2>&1 &
            """;
    }

    private static string EscapePowerShell(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static string EscapeShell(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal);

    private static Version GetCurrentVersion()
    {
        Version? version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version ?? new Version(0, 0, 0);
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SkuaScriptStudio-Updater");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        [JsonPropertyName("assets")]
        public GitHubReleaseAsset[]? Assets { get; init; }
    }

    public sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; init; }
    }
}

public sealed record UpdateCheckResult(bool IsUpdateAvailable, string CurrentVersion, string LatestVersion, string ReleaseUrl, IReadOnlyList<UpdateService.GitHubReleaseAsset> Assets);
