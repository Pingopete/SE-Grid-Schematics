# SE Grid Schematics

A Space Engineers LCD App aiming to provide functional real-grid schematic information and block interaction.

## Current Status

The mod entry point lives in:

```text
Data/Scripts/GridSchematics/GridSchematicsSession.cs
```

Implementation is split by responsibility under `Data/Scripts/GridSchematics/`:

- `Core/`: shared types, settings, construct discovery, and scan basis selection
- `Panels/`: panel discovery and future panel routing
- `Scanning/`: raycast sampling and scan metrics
- `Rendering/`: LCD drawing and schematic outline generation

In game, set an LCD panel app to Grid Schematics


Optional panel `CustomData` settings:

```text
VIEW=TOP
RES=256
FILLMODE=THICKNESS
```

Supported values:

- `VIEW`: `TOP`, `FRONT`, `SIDE`
- `RES`: `64`, `96`, `128`, `192`, `256`, `384`, `512`
- `FILLMODE`: `SOLID`, `THICKNESS`, `DENSITY`

## Working On The Mod

Use `main` for stable, playable work. Create feature branches for focused changes:

```powershell
git switch -c feature/lcd-controls
git switch -c refactor/scanner-core
git switch -c fix/raycast-filtering
```

See [docs/DEVELOPER_NOTES.md](docs/DEVELOPER_NOTES.md), [docs/GIT_WORKFLOW.md](docs/GIT_WORKFLOW.md), and [docs/MOD_STRUCTURE.md](docs/MOD_STRUCTURE.md).
