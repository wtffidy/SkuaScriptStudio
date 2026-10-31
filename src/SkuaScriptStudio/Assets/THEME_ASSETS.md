# Theme assets

This folder is reserved for the Skua Script Studio visual theme assets.

- `Backgrounds/`: large, low-contrast workspace backgrounds. Prefer 1920×1080 or larger, WebP/PNG, with enough dark negative space for readable controls.
- `TitleBars/`: wide title/header artwork. Prefer 1920×180 PNG or WebP; keep important details away from the window edges and leave room for the app title and toolbar.
- Keep artwork decorative rather than embedding UI text into the image. The app should remain readable when artwork is disabled or unavailable.

The theme palette lives in `ThemeResources.axaml`. UI controls should reference semantic brush keys rather than hard-coded hex colors so future potato-themed palettes can be swapped centrally.
