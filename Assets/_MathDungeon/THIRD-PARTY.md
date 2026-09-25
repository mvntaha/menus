# Third-party assets

Everything under `Assets/_MathDungeon/` that is not authored in this repo is
**Kenney (www.kenney.nl), released under CC0 1.0 Universal** — public domain,
free for personal, educational and commercial use. Crediting Kenney is
appreciated but not required.

Each pack folder keeps its original `License.txt` alongside the art.

| Folder | Pack | Source |
|---|---|---|
| `Art/UI/Kenney_UIPack` | UI Pack 2.0 | https://kenney.nl/assets/ui-pack |
| `Art/UI/Kenney_UIPack_RPG` | UI Pack — RPG Expansion | https://kenney.nl/assets/ui-pack-rpg-expansion |
| `Art/UI/Kenney_UIPack_Adventure` | UI Pack — Adventure | https://kenney.nl/assets/ui-pack-adventure |
| `Art/UI/Kenney_FantasyBorders` | Fantasy UI Borders | https://kenney.nl/assets/fantasy-ui-borders |
| `Art/UI/Kenney_GameIcons` | Game Icons | https://kenney.nl/assets/game-icons |
| `Fonts/Kenney` | Kenney Fonts | https://kenney.nl/assets/kenney-fonts |
| `Audio/UI` | UI Pack 2.0 (Sounds) | https://kenney.nl/assets/ui-pack |

## What was kept, and why

- **2x ("Double") sprite variants only.** Kenney ships 1x and 2x under identical
  filenames, so the two sets cannot live in one folder. 2x was chosen because
  these are 9-sliced UI sprites — Unity downscales them cleanly at 1080p and
  below, whereas upscaling 1x goes soft.
- **`Vector/` and `Spritesheet/` folders dropped.** Unity does not import SVG
  without an extra package, and the loose PNGs are easier to slice than an atlas.
  Both are a re-download away if needed.
- **Game Icons: White and Black only**, at 2x. Tint at runtime rather than
  importing more colour variants.

## Handoff note

This folder is self-contained and prefixed with `_` so it sorts to the top of the
Project window. Copying `Assets/_MathDungeon/` into the Techwiz project brings the
art, fonts, audio and licences across in one move — no other folder in this repo
holds project-specific content.
