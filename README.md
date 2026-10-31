# Skua Script Studio

A cross-platform desktop workspace for creating, editing, and organizing Skua C# scripts. Built with .NET 10 and Avalonia.

## Documentation

- **[User Guide](docs/USER_GUIDE.md)** — installation from source, editor workflow, Visual Builder, Script Library, diagnostics, snippets, shortcuts, and troubleshooting.

## Features

- Open and save local C# scripts.
- Import scripts into an application-managed local library.
- Search, open, and remove library copies without deleting the original imported files.
- Generate editable `Core.HuntMonster` code with the Visual Builder.
- Browse built-in Skua snippets and insert reusable code into the editor.
- Analyze C# syntax with cancellable background Roslyn analysis and jump from a diagnostic to its source location.
- Keep filesystem enumeration, file reads/writes, and script analysis off the UI thread.
- Save through a temporary file and atomic replacement; newer edits remain marked as unsaved.
- Warn before closing or replacing a document with unsaved changes, with Save, Discard, and Cancel choices.
- Use `Ctrl+O` to open, `Ctrl+S` to save, and `Ctrl+F5` to analyze the current script.
- Track unsaved editor changes and prevent stale asynchronous loads from replacing the active document.
- Windows and Linux CI build/test workflow targeting .NET 10.

## Getting started

### Requirements

- .NET 10 SDK
- Windows or Linux

### Run from source

~~~bash
dotnet restore SkuaScriptStudio.sln
dotnet build SkuaScriptStudio.sln --configuration Release
dotnet run --project src/SkuaScriptStudio/SkuaScriptStudio.csproj
~~~

### Run tests

~~~bash
dotnet test SkuaScriptStudio.sln --configuration Release
~~~

## Releases and updates

Releases are created manually from GitHub Actions:

1. Open **[Release Skua Script Studio](https://github.com/wtffidy/SkuaScriptStudio/actions/workflows/release.yml)**.
2. Select **Run workflow**.
3. Enter the version without a `v` prefix (for example, `1.0.1`). Use an incrementing version for each release (for example, 1.0.1, then 1.0.2).
4. Optionally mark the release as a prerelease and provide release notes.
5. Run the workflow. It builds Windows x64 and Linux x64 packages, then publishes both ZIP/TAR.GZ files to a versioned GitHub Release.

The desktop app's **Check for updates** button checks the latest non-prerelease GitHub Release. On Windows and Linux, it can download and apply the matching package, then restart the application. Close the app during installation; the update helper waits for the current process to exit before replacing application files. Keep the application in a folder your user account can write to. Android releases are not part of this workflow yet.

## Local data

The script library is stored under the current user's application data directory:

- **Windows:** `%APPDATA%/SkuaScriptStudio/Library`
- **Linux:** `~/.config/SkuaScriptStudio/Library` (based on the platform application-data path)

Imported scripts are copied into this folder. Editing a script opened from the library saves changes to the library copy. Removing a library entry does not delete the original imported file.

## Project status

Skua Script Studio is an early foundation build. The current Visual Builder generates a HuntMonster action, and Diagnostics reports C# syntax errors and warnings. It does not currently run scripts, connect to a Skua client, or perform semantic compilation against Skua assemblies.

See the [User Guide](docs/USER_GUIDE.md) for feature instructions and current limitations.
