# Skua Script Studio User Guide

This guide covers the features available in the current foundation build of Skua Script Studio (0.1.0).

- [Getting started](#getting-started)
- [Working in the editor](#working-in-the-editor)
- [Using the Visual Builder](#using-the-visual-builder)
- [Managing the Script Library](#managing-the-script-library)
- [Checking diagnostics](#checking-diagnostics)
- [Using snippets](#using-snippets)
- [Unsaved changes and file safety](#unsaved-changes-and-file-safety)
- [Keyboard shortcuts](#keyboard-shortcuts)
- [Local data](#local-data)
- [Current limitations](#current-limitations)
- [Troubleshooting](#troubleshooting)

## Getting started

Skua Script Studio is a local desktop workspace for editing and organizing Skua C# scripts. It currently targets .NET 10 and supports Windows and Linux.

### Run from source

1. Install the .NET 10 SDK.
2. Clone or download the repository.
3. Open a terminal in the repository directory.
4. Restore, build, and run the application:

~~~bash
dotnet restore SkuaScriptStudio.sln
dotnet build SkuaScriptStudio.sln --configuration Release
dotnet run --project src/SkuaScriptStudio/SkuaScriptStudio.csproj
~~~

To run the automated tests:

~~~bash
dotnet test SkuaScriptStudio.sln --configuration Release
~~~

The application opens on the Home page. Use the navigation on the left to switch between Home, Editor, Visual Builder, Script Library, Diagnostics, and Snippets.

## Working in the editor

The Editor is where you work with C# source files.

### Open a script

1. Select **Editor** in the left navigation.
2. Select **Open file** (or **Open** in the top bar).
3. Choose a local **.cs** file.
4. The file is loaded into the editor and its name is shown above the source area.

You can also open a script from the Script Library; see [Managing the Script Library](#managing-the-script-library).

### Create a script

1. Open the Editor.
2. Start typing in the empty editor.
3. Select **Save file**.
4. Choose a destination and filename when prompted.

New documents are initially untitled. Saving them assigns a file path.

### Edit and save

- The editor accepts C# text, tabs, and multiple lines.
- Unsaved edits are marked with an asterisk next to the document name.
- Select **Save file** or press **Ctrl+S** to save.
- Saving writes a temporary file beside the destination and then replaces the destination, reducing the chance of leaving a partially written script behind.

The editor is a source editor; it does not run scripts.

## Using the Visual Builder

The Visual Builder assembles a complete script from a sequence of actions. You do not need to know where C# code belongs.

1. Open **Visual Builder**.
2. Choose an action from **What should your bot do?**
3. Fill in the fields shown for that action. Unneeded fields are hidden, and the short description explains what to enter.
4. Select **Add this step**.
5. Repeat to add as many actions as your script needs.
6. Select a step and use **Move up**, **Move down**, or **Remove** to arrange the sequence. Steps run from top to bottom.
7. Select **Build my complete script** when the sequence is ready.

The current action choices are:

- **Hunt for an item** — map, monster, item name, and quantity.
- **Accept a quest** or **Complete a quest** — numeric quest ID.
- **Equip a class** — Farm, Solo, or Support.
- **Travel to a map** — map name.
- **Check inventory** — item name and required quantity.
- **Log a message** — text to show in the bot log.
- **Wait** — pause duration in seconds.

The finished script includes the CoreBots setup scaffold and is opened in the Editor for review and saving. The builder keeps your ordered steps until you clear them or close the application.

**Important:** Building a script replaces the current editor document. If the current document has unsaved changes, the application asks whether to save, discard, or cancel first.

## Managing the Script Library

The Script Library stores copies of scripts inside the application's data directory. It is separate from the folders where your original scripts are kept.

### Import scripts

1. Open **Script Library**.
2. Select **Import scripts**.
3. Select one or more **.cs** files.
4. The application copies them into its managed library.

If a filename already exists in the library, the import service chooses a non-conflicting filename rather than overwriting the existing copy.

### Search and open

- Enter part of a filename in the search field to filter the list.
- Select a script and choose **Open selected** to load the library copy into the Editor.
- Changes to a library script are saved to that library copy when you save the document.

### Remove a library entry

1. Select the entry.
2. Choose **Remove from library**.

This removes the copy stored in the application's library. It does **not** delete the original file that was imported.

## Checking diagnostics

Diagnostics uses the Roslyn C# parser to identify syntax errors and warnings in the current editor text.

1. Open the script you want to check.
2. Select **Diagnostics**.
3. Choose **Analyze current script** (or press **Ctrl+F5**).
4. Review the reported severity, diagnostic ID, message, and source location.
5. Select a diagnostic to move the editor caret to its reported line and column.

Analysis runs asynchronously and can be cancelled when the document changes or a newer analysis starts. Editing the script invalidates the previous results; run analysis again to check the updated source.

Diagnostics currently checks C# syntax. It does not compile the script against Skua assemblies, validate Skua API names or signatures, or execute the script.

## Using snippets

The Snippets page provides built-in Skua code patterns.

1. Select **Snippets**.
2. Search by snippet name or description.
3. Select a snippet to view its description and code.
4. Choose **Insert into editor** to add it to the current document.

The current built-in collection includes:

- Equip class
- Hunt monster
- Register quest
- Wait for inventory
- Hunt monster quest

Insertion appends the snippet to the existing editor content, separated by blank lines. If the editor is empty, the snippet becomes the document content. The document is marked as having unsaved changes.

## Unsaved changes and file safety

Skua Script Studio protects unsaved editor work when you:

- Close the application.
- Open another file.
- Open a script from the library.
- Generate builder code that replaces the current document.

When needed, choose:

- **Save** to save the current document before continuing.
- **Discard** to continue without saving those edits.
- **Cancel** to stay in the current document or keep the application open.

If saving fails or newer edits are made while a save is in progress, the application will not treat the document as safely saved for a replacement or close operation.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| **Ctrl+O** | Open a C# script |
| **Ctrl+S** | Save the current script |
| **Ctrl+F5** | Analyze the current script |

## Local data

The Script Library is stored under the current user's application data directory:

- **Windows:** %APPDATA%/SkuaScriptStudio/Library
- **Linux:** ~/.config/SkuaScriptStudio/Library (using the platform application-data location)

The library contains imported copies. Removing a library entry does not remove the original source file.

## Current limitations

This is an early foundation build. The following are not currently provided:

- Running or debugging scripts from the application.
- Connecting to a live Skua client or game session.
- Full compiler diagnostics or semantic validation against Skua assemblies.
- IntelliSense, completion, symbol navigation, or refactoring.
- A full visual scripting system. The builder currently supports the HuntMonster action only.
- Editing or configuring the built-in snippets from the UI.
- Recent files, autosave, and crash recovery.

Features and workflows may change as the application develops.

## Troubleshooting

### The application does not start

Confirm that the .NET 10 runtime is installed when running a framework-dependent build. If running from source, install the .NET 10 SDK and run the restore/build commands in [Getting started](#getting-started).

### A script does not appear in the library

Confirm that the file is a local **.cs** file and that the import completed. Use the search field to check whether the list is filtered.

### Diagnostics reports no issues, but the script still does not work in Skua

Diagnostics currently checks C# syntax only. A script can be syntactically valid while containing invalid Skua API calls, incorrect game data, or runtime logic problems. Validate scripts in the appropriate Skua environment before relying on them.

### I removed a script by mistake

Removing an entry deletes the library copy. If the original file still exists in its source folder, import it again. The library does not currently provide a recycle bin or restore history.
