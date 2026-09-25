# Math Dungeon — Menus

A standalone Unity project used **only** to build the Math Dungeon menu screens.
Nothing here ships on its own. The output is a package that gets imported into
the main **Techwiz** project.

> **If you only read one section, read [Handing off to Techwiz](#handing-off-to-techwiz).**

---

## Why this repo exists

Building menus directly in Techwiz means everyone fights over the same scene
files, and Unity scene merges corrupt silently when two people touch them. So the
menus are built here, in isolation, and delivered as a single self-contained
package that drops into Techwiz in one step.

Two rules keep that workable, and breaking either one is what causes pain later:

1. **Everything we author lives in `Assets/_MathDungeon/`.** Nothing project-specific
   goes anywhere else. That folder *is* the deliverable.
2. **The Unity version matches Techwiz exactly** — `6000.6.0f1`. Opening this in a
   different version silently upgrades the asset serialization, and then the
   import into Techwiz throws upgrade warnings or quietly breaks prefabs.

---

## Requirements

| | |
|---|---|
| Unity | **6000.6.0f1** — exact match required, not "6000.6 or newer" |
| Render pipeline | URP 17.6.0 |
| UI system | uGUI 2.6.0 (includes TextMeshPro) |

Check your version in Unity Hub before opening. If Hub offers to upgrade the
project, **say no** and install `6000.6.0f1` instead.

### Opening it

```bash
git clone https://github.com/mvntaha/menus.git
```

Then in Unity Hub: **Add → pick the cloned folder → open with 6000.6.0f1**.

First open takes a few minutes — Unity is building the `Library/` folder from
scratch. That folder is gitignored and regenerable; never commit it.

---

## What's in here

```
Menus/
├── Assets/
│   ├── _MathDungeon/          ← EVERYTHING WE AUTHOR. This is the deliverable.
│   │   ├── Art/UI/            ← 999 sprites, 5 Kenney packs
│   │   │   ├── Kenney_UIPack/           buttons, panels, sliders, checkboxes (6 colourways)
│   │   │   ├── Kenney_UIPack_RPG/       hearts, mana, inventory slots, bars
│   │   │   ├── Kenney_UIPack_Adventure/ chunkier fantasy-styled controls
│   │   │   ├── Kenney_FantasyBorders/   ornate frames, dividers, panel borders
│   │   │   └── Kenney_GameIcons/        210 icons, white + black, tint at runtime
│   │   ├── Audio/UI/          ← 6 click/tap/switch sounds
│   │   ├── Fonts/Kenney/      ← 12 TTFs
│   │   ├── Editor/            ← import rules (see below)
│   │   └── THIRD-PARTY.md     ← licences and sourcing decisions
│   │
│   ├── Scenes/                ┐
│   ├── Settings/              │ Unity's URP template. Not ours.
│   ├── TutorialInfo/          │ Do NOT copy these to Techwiz.
│   └── InputSystem_Actions…   ┘
│
├── Delivery/
│   └── MathDungeon.unitypackage   ← the built handoff package (2.8 MB)
│
├── Packages/manifest.json     ← this project's packages. NOT Techwiz's. Do not copy.
└── ProjectSettings/           ← this project's settings. Do not copy.
```

The `_` prefix on `_MathDungeon` is deliberate: it sorts to the top of Unity's
Project window, above the template folders.

---

## Handing off to Techwiz

There are two routes. **Use route A.** Route B is the fallback if the package
import misbehaves.

### Route A — import the `.unitypackage` (recommended)

This preserves asset GUIDs, folder structure and every import setting. Nothing
to configure on the other side.

```bash
# 1. In the Techwiz repo, start from a clean, up-to-date main
git checkout main
git pull
git checkout -b feature/math-dungeon-menus
```

2. Open the **Techwiz** project in Unity `6000.6.0f1`.
3. Grab `Delivery/MathDungeon.unitypackage` from this repo (clone it, or download
   the file from GitHub).
4. In Techwiz: **Assets → Import Package → Custom Package…**, pick that file.
5. The import dialog lists 1052 assets, all ticked. Leave them all ticked, hit
   **Import**. It lands at `Assets/_MathDungeon/` — the same path it has here.
6. Wait for the import to finish (the progress bar runs for a minute or two —
   it's compiling ~1000 sprites).
7. Check the Console. It should be clean.

```bash
# 8. Commit on the branch
git add Assets/_MathDungeon Assets/_MathDungeon.meta
git commit -m "Add Math Dungeon menu assets"
git push -u origin feature/math-dungeon-menus
```

Then open a PR against Techwiz's `main`.

### Route B — copy the folder by hand

Only if route A fails.

1. Close Unity on **both** projects first. Copying assets under a running Editor
   causes half-imported states that are annoying to clear.
2. Copy `Assets/_MathDungeon/` from this repo to `Assets/_MathDungeon/` in Techwiz.
   **Copy the `.meta` files too** — they carry the GUIDs and the import settings.
   If you copy only the `.png` files, Unity regenerates fresh metas with wrong
   settings and new GUIDs, and every reference breaks.
3. Also copy the sibling `Assets/_MathDungeon.meta`.
4. Reopen Techwiz. Unity imports the folder.

### What NOT to copy

Copying these will break Techwiz:

- **`Packages/manifest.json`** — this project has packages Techwiz doesn't need
  (notably `com.unity.pipeline`, a tooling package), and overwriting Techwiz's
  manifest would strip packages it depends on.
- **`ProjectSettings/`** — layers, tags, physics, input and quality settings are
  Techwiz's own. Overwriting them is a bad afternoon.
- **`Assets/Scenes/`, `Assets/Settings/`, `Assets/TutorialInfo/`** — Unity's URP
  template sample content. Not ours, not wanted.

### Will it look right in Techwiz?

Yes. These are **UI sprites drawn on a Canvas**, which uses the `UI/Default`
shader and is independent of the render pipeline. They render identically under
URP, HDRP or Built-in — so even if Techwiz's pipeline config differs from this
project's, the menus are unaffected. The usual "everything turned pink" URP
problem applies to 3D materials, not canvas art.

---

## Regenerating the delivery package

If you change anything under `Assets/_MathDungeon/` here, rebuild the package
before handing off again, or Techwiz gets stale art.

In Unity: right-click `_MathDungeon` in the Project window → **Export Package…**
→ make sure *Include dependencies* is ticked → save to `Delivery/MathDungeon.unitypackage`
→ commit it.

---

## Import rules — read this before wondering why a sprite looks wrong

Unity's URP 3D template imports PNGs as **Texture Type: Default, with mipmaps on**.
For canvas art that is wrong in two ways: sprites render blurry (mipmaps get
selected even at 1:1), and they cost memory they don't need.

Rather than fix ~1000 files by hand,
[`Assets/_MathDungeon/Editor/MathDungeonUIImportRules.cs`](Assets/_MathDungeon/Editor/MathDungeonUIImportRules.cs)
stamps the correct settings automatically on anything imported under
`Assets/_MathDungeon/Art/UI/`:

| Setting | Value | Why |
|---|---|---|
| Texture Type | Sprite (2D and UI) | it's canvas art |
| Sprite Mode | Single | one sprite per file, not an atlas |
| Mesh Type | **FullRect** | required for `Image` set to Sliced / Tiled / Filled — Tight breaks 9-slicing |
| Mipmaps | off | UI is never minified |
| Wrap Mode | Clamp | stops edge bleed on 9-sliced borders |
| Compression | Uncompressed | DXT blocking is visible on Kenney's soft borders |
| Max Size | 2048 | none of the source art exceeds this |

Two things to know about it:

- **It only fires on first import.** If you set 9-slice borders on a sprite in the
  Inspector, a reimport will not wipe them. That's deliberate.
- **It travels with the folder.** It lives inside `_MathDungeon/Editor/` and is
  path-scoped to `_MathDungeon/Art/UI/`, so it keeps working after the import
  into Techwiz and touches nothing else in that project.

If you ever need to force the rules to re-apply to existing assets, delete the
`.png.meta` files under `Art/UI/` and let Unity reimport — but only do that while
nothing references those sprites, because it generates new GUIDs and breaks
existing references.

### 9-slicing a button

The rules can't guess border widths, so that's still manual and per-sprite:

1. Select the sprite → **Sprite Editor** → drag the green border handles in.
2. On the `Image` component, set **Image Type: Sliced**.

Do this once per sprite you actually use. Don't pre-slice all 999.

---

## Licensing

All third-party art, fonts and audio are **Kenney (kenney.nl), CC0 1.0** — public
domain, free for commercial and educational use, credit appreciated but not
required. Each pack folder keeps its original `License.txt`, and
[`Assets/_MathDungeon/THIRD-PARTY.md`](Assets/_MathDungeon/THIRD-PARTY.md) records
the full provenance.

Nothing here has an attribution obligation, so the Techwiz build needs no credits
screen on account of these assets.

---

## Git notes

`.gitattributes` sets **UnityYAMLMerge** on `.unity`, `.prefab`, `.asset`, `.mat`,
`.anim`, `.controller` and `.meta`. Without it, a merge on a scene file produces
YAML that Unity loads but renders wrong — silently. If you clone this on a new
machine, point Git at Unity's merge tool once:

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

Adjust the path if your Unity install isn't under Hub's default location.

Also: `Library/`, `Temp/`, `Logs/`, `obj/` and `UserSettings/` are gitignored.
They're machine-specific and regenerable. If `git status` ever shows them, the
`.gitignore` has been damaged — fix that rather than committing them.

---

## Troubleshooting

**"Unity wants to upgrade the project."**
Wrong Editor version. Close without upgrading, install `6000.6.0f1` from Unity Hub.

**Sprites look blurry or soft in Techwiz.**
The import rules didn't run — likely the `Editor/` folder wasn't included in the
copy. Confirm `Assets/_MathDungeon/Editor/MathDungeonUIImportRules.cs` exists in
Techwiz, then delete the `.png.meta` files under `Art/UI/` and let Unity reimport.

**A 9-sliced button's corners stretch.**
Mesh Type is Tight instead of FullRect, or the sprite's borders were never set in
the Sprite Editor. Check the Sprite Editor first — that's the usual cause.

**TextMeshPro text renders as pink boxes.**
TMP Essentials aren't imported in that project: **Window → TextMeshPro → Import
TMP Essential Resources**. Note that this project doesn't ship TMP Essentials
either — they're Unity's, not ours, and each project imports its own.

**Console spam: "Account API did not become accessible within 30 seconds."**
Harmless. It's Unity's AI Assistant package failing to reach its service. It has
nothing to do with this project's assets and can be ignored.

**Merge conflict in a `.unity` or `.prefab` file.**
Don't hand-edit it. Set up UnityYAMLMerge (see Git notes) and re-run the merge.

---

## Status

| | |
|---|---|
| ✅ | Project scaffolded, Unity version matched to Techwiz |
| ✅ | 999 sprites, 12 fonts, 6 UI sounds imported and configured |
| ✅ | Automatic UI import rules in place |
| ✅ | `Delivery/MathDungeon.unitypackage` built and committed |
| ⬜ | **Backdrop concept not yet chosen** — see below |
| ⬜ | Main menu scene |
| ⬜ | Settings / level select screens |

### Open decision

Three backdrop concepts are on the table for the main menu:

- **A · Torchlit Stone** — tiled stone wall, two torch pools, heavy vignette.
  Cheapest to build, but generic; nothing about it says *maths*.
- **B · Descent Shaft** — looking down a shaft through four parallax arch layers.
  Reinforces the idea of descending floors. Needs sorted layers and a parallax script.
- **C · Etched Slate** — dark slate wall with arithmetic glyphs carved into it,
  lit by a slow emissive pulse. The only one that states the genre on sight.

**Recommendation: C.** For a school-facing project, a player understanding the
premise in the first second beats atmosphere. B's parallax isn't exclusive with
C and can be layered on later.

C still needs one call: do the glyphs read as **carved runes** (mysterious,
thematic) or **chalk working** (classroom, legible)?
