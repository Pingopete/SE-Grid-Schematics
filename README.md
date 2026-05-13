# Raycast Grid Discovery

Space Engineers mod prototype for scanning a construct with orthographic raycasts and drawing a schematic-style LCD preview.

## Current Status

The active mod code lives in:

```text
Data/Scripts/SchemaRayTest/SchemaRayTest.cs
```

In game, add an LCD or text panel whose name contains:

```text
[SCHEMA_TEST]
```

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

See [docs/GIT_WORKFLOW.md](docs/GIT_WORKFLOW.md) and [docs/MOD_STRUCTURE.md](docs/MOD_STRUCTURE.md).
