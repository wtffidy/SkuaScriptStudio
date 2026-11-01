# Theme assets

The app's potato-purple artwork is built as lightweight Avalonia vector controls so it scales cleanly on Windows and Linux without large bitmap downloads.

- `PotatoBackgroundArtwork.axaml`: subtle potato, sparkle, and purple-gradient decoration behind the workspace.
- `PotatoTitleBarArtwork.axaml`: matching potato and crown decoration layered into the custom application header.
- `Backgrounds/`: reserved for optional full-resolution raster backgrounds (1920×1080 or larger, WebP/PNG).
- `TitleBars/`: reserved for optional wide banner artwork (1920×180 PNG/WebP).

Keep artwork decorative rather than embedding UI text into images. The app should remain readable when decorative artwork is disabled or unavailable.

The theme palette lives in `ThemeResources.axaml`. UI controls should reference semantic brush keys rather than hard-coded colors so future palettes can be swapped centrally.
