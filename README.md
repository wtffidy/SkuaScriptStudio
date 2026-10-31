# Skua Script Studio

A cross-platform desktop workspace for creating and managing Skua C# scripts.

## Features

- Open and save local C# scripts.
- Import scripts into an application-managed local library.
- Search, open, and remove library copies without deleting the original imported files.
- Generate editable `Core.HuntMonster` code with the visual builder.
- Windows and Linux CI build/test workflow targeting .NET 10.

## Local data

The script library is stored under the current user's application data directory:

- Windows: `%APPDATA%/SkuaScriptStudio/Library`
- Linux: `~/.config/SkuaScriptStudio/Library` (based on the platform application-data path)

Imported scripts are copied into this folder. Editing a script opened from the library saves changes to the library copy.

## Build

```bash
dotnet restore SkuaScriptStudio.sln
dotnet build SkuaScriptStudio.sln --configuration Release
dotnet test SkuaScriptStudio.sln --configuration Release
```

.NET 10 SDK required.
