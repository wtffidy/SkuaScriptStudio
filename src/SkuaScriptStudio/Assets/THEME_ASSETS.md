# Tatotastic™ theme assets

The live client uses the original potato artwork crops committed under
`potato-artwork-assets/`. They are embedded as Avalonia resources, so the
images ship inside the application on Windows, Linux and macOS.

## Live artwork

- `potato-banner.png` — current wide illustrated title/header background used behind the Studio header controls.
- `potato-title-banner.jpg` — retained legacy header artwork/reference.
- `potato-hero.jpg` — primary crowned potato artwork used in the app identity and Home welcome panel.
- `potato-sidebar.jpg` — compact sidebar Potato Patch artwork.
- `potato-pattern-corner.jpg` — low-opacity decorative workspace background.
- `potato-happy-scripting.jpg` — Home page Potato Patch feature banner.

`../MainWindow.axaml` references these resources directly with Avalonia resource URIs.
`../SkuaScriptStudio.csproj` embeds the JPG files in the application package.

## Editable reference backups

The original vector implementations remain in this folder:

- `PotatoMascotArtwork.axaml` and code-behind.
- `PotatoPatchArtwork.axaml` and code-behind.
- `PotatoTitleBarArtwork.axaml` and code-behind.
- `PotatoBackgroundArtwork.axaml` and code-behind.
- `Reference/` contains editable SVG reference assets.

These are retained as editable fallbacks/reference material; the live window now
uses the original supplied artwork rather than the simplified vector mascot.
