# The Hollow Beyond — UI theme

This folder contains the first visual pass for the MainMenu scene. The palette and
assets can be reused when the other scenes are designed later.

## Visual rules

- Base: near-black navy and indigo (`#080A18`, `#17142D`).
- Accent: muted violet borders (`#75558E`).
- Text: warm ivory (`#D8D1C0`), with muted lavender (`#AFA4B7`) for secondary text.
- Buttons: dark translucent fill with a thin violet outline and an ivory serif label.
- Main menu: title and controls on the left; the eclipse and traveler remain on the right.
- Cosmic horror tone without gore or blood.

The background is a separate texture with no baked-in text or buttons. Interactive
elements remain Unity UI so they can respond to input and change language.
`CosmicMenuAmbience` adds a few softly twinkling stars and a slow eclipse-halo pulse
at runtime. These effects do not intercept clicks and do not require sprite sheets.

## Apply to scenes

In the Unity Editor, use **Tools > Cosmic UI > Apply Theme to Main Menu**.
The tool updates only MainMenu. The New Run button calls `MainMenuManager.StartGame`,
which now enters MapScene so `RunManager` can initialize a run. Continue is disabled
until a real save/load flow exists.

English and Thai Main Menu labels use `CosmicLocalizedText`. The top-right `TH / EN`
button toggles the saved language preference. Other scenes and dynamic gameplay text
are outside this first pass.

## Asset provenance

- `Art/CosmicMenuBackground.png`: generated with the built-in ImageGen tool for this project.
- `Fonts/Cinzel.ttf`: Google Fonts, SIL Open Font License (see `Cinzel-OFL.txt`).
- `Fonts/NotoSerifThai.ttf`: Google Fonts, SIL Open Font License (see `NotoSerifThai-OFL.txt`).

Background prompt: "Original 16:9 dark cosmic fantasy landscape for a Unity main
menu. Deep indigo night, restrained violet haze, sparse stars, a black eclipse
with a subtle purple corona on the right, and a lone cloaked traveler on a rocky
ridge. Keep the left 35 percent dark and low-detail for Unity UI. Painterly 2D
game art. No gore, blood, text, logos, menus, buttons, borders, or watermarks."
