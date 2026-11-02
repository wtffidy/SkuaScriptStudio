# Potato Studio theme assets

The Potato Studio identity is built from editable Avalonia vector controls. The client does not use flattened screenshots for its theme.

## Live artwork

- `PotatoMascotArtwork.axaml` and code-behind: reusable crowned potato mascot with facial expressions, blush, limbs, freckles, highlights and sparkles. Uses a Viewbox so it scales cleanly in different panels.
- `PotatoPatchArtwork.axaml` and code-behind: a small family of potatoes growing in a storybook patch, used on the Home workspace.
- `PotatoTitleBarArtwork.axaml`: layered decorative banner and mascot illustrations.
- `PotatoBackgroundArtwork.axaml`: subdued mascot illustrations, ambient glows, garden tones and sparkles behind the workspace.
- `../MainWindow.axaml`: uses the art in the title identity, Home welcome panel, Potato Patch feature panel, sidebar and workspace.
- `../ThemeResources.axaml`: central plum, aubergine, toasted-gold, potato-cream and editor palette.

## Reference backups

- `Reference/potato-mascot-reference.svg`: editable master mascot vector.
- `Reference/potato-patch-reference.svg`: editable potato family and patch vector.
- `Reference/README.md`: source-reference maintenance notes.
- `Backgrounds/`: reserved for optional full-resolution raster background references (1920×1080 or larger, WebP/PNG).
- `TitleBars/`: reserved for optional wide banner references (1920×180 PNG/WebP).

Keep artwork decorative rather than embedding UI text into images. The app must remain readable when decorative artwork is disabled or unavailable. Keep vector source editable and scalable across Windows, Linux and macOS. New artwork should have both a live Avalonia implementation and an editable reference backup.
