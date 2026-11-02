# Potato Studio theme assets

The Potato Studio identity is implemented as editable, scalable Avalonia vector controls. No generated screenshots or flattened UI mockups are used in the client.

## Live artwork

- `PotatoMascotArtwork.axaml` and its code-behind: reusable crowned potato character with face, cheeks, limbs, highlights and sparkles.
- `PotatoTitleBarArtwork.axaml`: layered mascot and decorative banner art.
- `PotatoBackgroundArtwork.axaml`: subdued mascot illustrations, glows and sparkles behind the workspace.
- `../MainWindow.axaml`: places the mascot in the app identity, home welcome panel and navigation footer.
- `../ThemeResources.axaml`: central plum, aubergine, toasted-gold, potato-cream and editor palette.

## Reference backups

- `Reference/potato-mascot-reference.svg` is the editable master vector reference.
- `Reference/README.md` explains how to keep the source reference in sync with the live artwork.
- `Backgrounds/` is reserved for optional full-resolution raster background references (1920×1080 or larger, WebP/PNG).
- `TitleBars/` is reserved for optional wide banner references (1920×180 PNG/WebP).

Keep artwork decorative rather than embedding UI text into images. The app should remain readable when decorative artwork is disabled or unavailable. Do not replace the live controls with a flattened screenshot: the client artwork must remain scalable and work across Windows, Linux and macOS.
